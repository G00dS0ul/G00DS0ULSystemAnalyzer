using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class ScanDiffService : IScanDiffService
{
	private readonly DiskScannerEngine _engine;
	private readonly IScanSnapshotStore _store;
	private readonly ISettingService _settings;
	private readonly IMemoryCache _cache;
	private readonly ILogger<ScanDiffService> _logger;

	public ScanDiffService(
		DiskScannerEngine engine,
		IScanSnapshotStore store,
		ISettingService settings,
		IMemoryCache cache,
		ILogger<ScanDiffService> logger)
	{
		_engine = engine;
		_store = store;
		_settings = settings;
		_cache = cache;
		_logger = logger;
	}

	private static string CacheKey(string root) => $"scandiff:{ScanSnapshotStore.NormalizeDriveKey(root)}";

	public ScanDiff ComputeAndPromote(string root)
	{
		var depth = _settings.Current.Scan.Depth;
		var now = DateTimeOffset.UtcNow;

		var current = BuildSnapshotFromCache(root);
		var baseline = _store.Load(root, depth);

		ScanDiff diff = baseline == null
			? BuildFirstScanDiff(root, now)
			: BuildDiff(root, baseline, current.Entries, baseline.CapturedAt, now);

		// Cache the computed diff for the GET endpoint to read (no recomputation on read).
		_cache.Set(CacheKey(root), diff, TimeSpan.FromHours(1));

		// Promote current to baseline AFTER diffing, so the next scan compares against this one.
		try
		{
			_store.Save(root, depth, current);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to promote scan baseline for {Root}", root);
		}

		return diff;
	}

	public ScanDiff? GetCachedDiff(string root, long minDeltaBytes = 0)
	{
		if (!_cache.TryGetValue(CacheKey(root), out ScanDiff? hit) || hit == null)
			return null;

		if (minDeltaBytes <= 0)
			return hit;

		// Filter small movers out of the lists but keep the summary counts intact.
		var grown = hit.Grown.Where(e => Math.Abs(e.DeltaBytes) >= minDeltaBytes).ToList();
		var shrunk = hit.Shrunk.Where(e => Math.Abs(e.DeltaBytes) >= minDeltaBytes).ToList();

		return hit with { Grown = grown, Shrunk = shrunk };
	}

	public void ClearBaseline(string root)
	{
		_store.Delete(root, _settings.Current.Scan.Depth);
		_cache.Remove(CacheKey(root));
	}

	private ScanSnapshot BuildSnapshotFromCache(string root)
	{
		var rootFwd = ToForwardSlash(Path.GetFullPath(root));
		var rootNoSlash = rootFwd.TrimEnd('/');
		var prefix = rootNoSlash + "/";

		var entries = new Dictionary<string, ScanSnapshotEntry>(StringComparer.OrdinalIgnoreCase);

		foreach (var kvp in _engine.DirectorySizeCache)
		{
			var keyFwd = ToForwardSlash(kvp.Key).TrimEnd('/');

			// Include the root itself and everything beneath it.
			var underRoot = keyFwd.Equals(rootNoSlash, StringComparison.OrdinalIgnoreCase)
				|| keyFwd.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
			if (!underRoot) continue;

			entries[keyFwd] = new ScanSnapshotEntry(
				SizeBytes: kvp.Value.Size,
				IsDirectory: true,
				LastModified: new DateTimeOffset(DateTime.SpecifyKind(kvp.Value.LastUpdated, DateTimeKind.Utc)));
		}

		return new ScanSnapshot(DateTimeOffset.UtcNow, entries);
	}

	private static ScanDiff BuildFirstScanDiff(string root, DateTimeOffset now) =>
		new(
			Root: ToForwardSlash(Path.GetFullPath(root)).TrimEnd('/'),
			HasBaseline: false,
			BaselineScannedAt: null,
			CurrentScannedAt: now,
			Added: Array.Empty<ScanDiffEntry>(),
			Removed: Array.Empty<ScanDiffEntry>(),
			Grown: Array.Empty<ScanDiffEntry>(),
			Shrunk: Array.Empty<ScanDiffEntry>(),
			Summary: new ScanDiffSummary(0, 0, 0, 0, 0, 0, 0, 0, 0));

	private static ScanDiff BuildDiff(
		string root,
		ScanSnapshot baseline,
		Dictionary<string, ScanSnapshotEntry> current,
		DateTimeOffset baselineAt,
		DateTimeOffset now)
	{
		var prev = baseline.Entries;

		// The root directory always changes when anything beneath it does; its delta is the
		// summary's NetDeltaBytes, so listing it as a mover would be redundant. Exclude it.
		var rootKey = ToForwardSlash(Path.GetFullPath(root)).TrimEnd('/');

		var added = new List<ScanDiffEntry>();
		var removed = new List<ScanDiffEntry>();
		var grown = new List<ScanDiffEntry>();
		var shrunk = new List<ScanDiffEntry>();

		// Sets of paths that are wholly new / wholly gone, used to collapse descendants.
		var addedPaths = current.Keys
			.Where(k => !prev.ContainsKey(k) && !k.Equals(rootKey, StringComparison.OrdinalIgnoreCase))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var removedPaths = prev.Keys
			.Where(k => !current.ContainsKey(k) && !k.Equals(rootKey, StringComparison.OrdinalIgnoreCase))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// ADDED — collapse to the top-most new directory; count collapsed descendants.
		foreach (var path in addedPaths)
		{
			if (HasAncestorIn(path, addedPaths)) continue; // descendant of another added dir
			var childCount = current.Keys.Count(k => IsDescendantOf(k, path));
			var e = current[path];
			added.Add(new ScanDiffEntry(path, e.IsDirectory, DiffKind.Added,
				CurrentBytes: e.SizeBytes, PreviousBytes: 0, DeltaBytes: e.SizeBytes,
				ChildCount: childCount > 0 ? childCount : null, LastModified: e.LastModified));
		}

		// REMOVED — collapse to the top-most vanished directory; count collapsed descendants.
		foreach (var path in removedPaths)
		{
			if (HasAncestorIn(path, removedPaths)) continue;
			var childCount = prev.Keys.Count(k => IsDescendantOf(k, path));
			var e = prev[path];
			removed.Add(new ScanDiffEntry(path, e.IsDirectory, DiffKind.Removed,
				CurrentBytes: 0, PreviousBytes: e.SizeBytes, DeltaBytes: -e.SizeBytes,
				ChildCount: childCount > 0 ? childCount : null, LastModified: e.LastModified));
		}

		// GROWN / SHRUNK — present in both; classify by signed recursive delta.
		foreach (var kvp in current)
		{
			if (kvp.Key.Equals(rootKey, StringComparison.OrdinalIgnoreCase)) continue; // root delta = NetDeltaBytes
			if (!prev.TryGetValue(kvp.Key, out var prevEntry)) continue;
			var delta = kvp.Value.SizeBytes - prevEntry.SizeBytes;
			if (delta == 0) continue; // never emit unchanged

			var entry = new ScanDiffEntry(kvp.Key, kvp.Value.IsDirectory,
				delta > 0 ? DiffKind.Grown : DiffKind.Shrunk,
				CurrentBytes: kvp.Value.SizeBytes, PreviousBytes: prevEntry.SizeBytes,
				DeltaBytes: delta, ChildCount: null, LastModified: kvp.Value.LastModified);

			if (delta > 0) grown.Add(entry); else shrunk.Add(entry);
		}

		// Biggest movers first.
		added.Sort((a, b) => b.CurrentBytes.CompareTo(a.CurrentBytes));
		removed.Sort((a, b) => b.PreviousBytes.CompareTo(a.PreviousBytes));
		grown.Sort((a, b) => Math.Abs(b.DeltaBytes).CompareTo(Math.Abs(a.DeltaBytes)));
		shrunk.Sort((a, b) => Math.Abs(b.DeltaBytes).CompareTo(Math.Abs(a.DeltaBytes)));

		// Directory sizes are recursive, so summing nested deltas would double-count. The
		// authoritative net change of the tree is the root's own recursive-size delta.
		long netDelta;
		if (current.TryGetValue(rootKey, out var curRoot) && prev.TryGetValue(rootKey, out var prevRoot))
			netDelta = curRoot.SizeBytes - prevRoot.SizeBytes;
		else if (current.TryGetValue(rootKey, out var addedRoot))
			netDelta = addedRoot.SizeBytes;          // whole root is new
		else if (prev.TryGetValue(rootKey, out var goneRoot))
			netDelta = -goneRoot.SizeBytes;          // whole root vanished
		else
			netDelta = 0;

		var summary = new ScanDiffSummary(
			AddedCount: added.Count, AddedBytes: added.Sum(e => e.CurrentBytes),
			RemovedCount: removed.Count, RemovedBytes: removed.Sum(e => e.PreviousBytes),
			GrownCount: grown.Count, GrownDeltaBytes: grown.Sum(e => e.DeltaBytes),
			ShrunkCount: shrunk.Count, ShrunkDeltaBytes: shrunk.Sum(e => e.DeltaBytes),
			NetDeltaBytes: netDelta);

		return new ScanDiff(
			Root: ToForwardSlash(Path.GetFullPath(root)).TrimEnd('/'),
			HasBaseline: true,
			BaselineScannedAt: baselineAt,
			CurrentScannedAt: now,
			Added: added, Removed: removed, Grown: grown, Shrunk: shrunk,
			Summary: summary);
	}

	private static bool IsDescendantOf(string path, string ancestor) =>
		path.Length > ancestor.Length
		&& path.StartsWith(ancestor + "/", StringComparison.OrdinalIgnoreCase);

	private static bool HasAncestorIn(string path, HashSet<string> set)
	{
		var idx = path.LastIndexOf('/');
		while (idx > 0)
		{
			var parent = path[..idx];
			if (set.Contains(parent)) return true;
			idx = parent.LastIndexOf('/');
		}
		return false;
	}

	private static string ToForwardSlash(string p) => p.Replace('\\', '/');
}

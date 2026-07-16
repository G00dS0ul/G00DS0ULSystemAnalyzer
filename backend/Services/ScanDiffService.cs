using System.Collections.Concurrent;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class ScanDiffService : IScanDiffService
{
	private readonly DiskScannerEngine _engine;
	private readonly IScanSnapshotStore _snapshotStore;
	private readonly ILogger<ScanDiffService> _logger;

	private readonly ConcurrentDictionary<string, ScanDiff> _diffCache = new(StringComparer.OrdinalIgnoreCase);

	public ScanDiffService(
		DiskScannerEngine engine,
		IScanSnapshotStore snapshotStore,
		ILogger<ScanDiffService> logger)
	{
		_engine = engine;
		_snapshotStore = snapshotStore;
		_logger = logger;
	}

	public ScanDiff ComputeDiff(string root, int depth, DateTimeOffset currentScannedAt)
	{
		var normalizedRoot = NormalizeRoot(root);
		_logger.LogInformation("Computing diff for {Root} at depth {Depth}", normalizedRoot, depth);

		// 1. Build current snapshot from the engine's DirectorySizeCache
		var currentSnapshot = BuildCurrentSnapshot(normalizedRoot);

		// 2. Load the baseline snapshot
		var baseline = _snapshotStore.LoadBaseline(normalizedRoot, depth);
		var baselineTimestamp = _snapshotStore.GetBaselineTimestamp(normalizedRoot, depth);

		ScanDiff diff;

		if (baseline == null)
		{
			// First scan — no baseline to compare against
			_logger.LogInformation("No baseline found for {Root} depth {Depth} — first scan, saving baseline", normalizedRoot, depth);

			var emptySummary = new ScanDiffSummary(0, 0, 0, 0, 0, 0, 0, 0, 0);
			diff = new ScanDiff(
				Root: normalizedRoot,
				HasBaseline: false,
				BaselineScannedAt: null,
				CurrentScannedAt: currentScannedAt,
				Added: Array.Empty<ScanDiffEntry>(),
				Removed: Array.Empty<ScanDiffEntry>(),
				Grown: Array.Empty<ScanDiffEntry>(),
				Shrunk: Array.Empty<ScanDiffEntry>(),
				Summary: emptySummary);
		}
		else
		{
			// 3. Classify every path
			var addedEntries = new List<ScanDiffEntry>();
			var removedEntries = new List<ScanDiffEntry>();
			var grownEntries = new List<ScanDiffEntry>();
			var shrunkEntries = new List<ScanDiffEntry>();

			// Paths in current but not in baseline → Added
			foreach (var kvp in currentSnapshot)
			{
				if (!baseline.ContainsKey(kvp.Key))
				{
					addedEntries.Add(new ScanDiffEntry(
						Path: kvp.Key,
						IsDirectory: kvp.Value.IsDirectory,
						Kind: DiffKind.Added,
						CurrentBytes: kvp.Value.SizeBytes,
						PreviousBytes: 0,
						DeltaBytes: kvp.Value.SizeBytes,
						ChildCount: null,
						LastModified: kvp.Value.LastModified));
				}
			}

			// Paths in baseline but not in current → Removed
			foreach (var kvp in baseline)
			{
				if (!currentSnapshot.ContainsKey(kvp.Key))
				{
					removedEntries.Add(new ScanDiffEntry(
						Path: kvp.Key,
						IsDirectory: kvp.Value.IsDirectory,
						Kind: DiffKind.Removed,
						CurrentBytes: 0,
						PreviousBytes: kvp.Value.SizeBytes,
						DeltaBytes: -kvp.Value.SizeBytes,
						ChildCount: null,
						LastModified: kvp.Value.LastModified));
				}
			}

			// Paths in both → Grown / Shrunk / Unchanged
			foreach (var kvp in currentSnapshot)
			{
				if (baseline.TryGetValue(kvp.Key, out var baselineEntry))
				{
					var delta = kvp.Value.SizeBytes - baselineEntry.SizeBytes;
					if (delta > 0)
					{
						grownEntries.Add(new ScanDiffEntry(
							Path: kvp.Key,
							IsDirectory: kvp.Value.IsDirectory,
							Kind: DiffKind.Grown,
							CurrentBytes: kvp.Value.SizeBytes,
							PreviousBytes: baselineEntry.SizeBytes,
							DeltaBytes: delta,
							ChildCount: null,
							LastModified: kvp.Value.LastModified));
					}
					else if (delta < 0)
					{
						shrunkEntries.Add(new ScanDiffEntry(
							Path: kvp.Key,
							IsDirectory: kvp.Value.IsDirectory,
							Kind: DiffKind.Shrunk,
							CurrentBytes: kvp.Value.SizeBytes,
							PreviousBytes: baselineEntry.SizeBytes,
							DeltaBytes: delta,
							ChildCount: null,
							LastModified: kvp.Value.LastModified));
					}
					// delta == 0 → Unchanged, never emitted
				}
			}

			// 4. Directory collapse for Added/Removed
			var collapsedAdded = CollapseDirectories(addedEntries, currentSnapshot);
			var collapsedRemoved = CollapseDirectories(removedEntries, baseline);

			// 5. Sort by magnitude descending
			collapsedAdded.Sort((a, b) => b.CurrentBytes.CompareTo(a.CurrentBytes));
			collapsedRemoved.Sort((a, b) => b.PreviousBytes.CompareTo(a.PreviousBytes));
			grownEntries.Sort((a, b) => b.DeltaBytes.CompareTo(a.DeltaBytes));
			shrunkEntries.Sort((a, b) => Math.Abs(a.DeltaBytes).CompareTo(Math.Abs(b.DeltaBytes)) * -1);

			// 6. Build summary from un-collapsed data (counts all entries, not just top-level)
			var addedBytes = addedEntries.Sum(e => e.DeltaBytes);
			var removedBytes = removedEntries.Sum(e => e.DeltaBytes);
			var grownDeltaBytes = grownEntries.Sum(e => e.DeltaBytes);
			var shrunkDeltaBytes = shrunkEntries.Sum(e => e.DeltaBytes);

			var summary = new ScanDiffSummary(
				AddedCount: addedEntries.Count,
				AddedBytes: addedBytes,
				RemovedCount: removedEntries.Count,
				RemovedBytes: removedBytes,
				GrownCount: grownEntries.Count,
				GrownDeltaBytes: grownDeltaBytes,
				ShrunkCount: shrunkEntries.Count,
				ShrunkDeltaBytes: shrunkDeltaBytes,
				NetDeltaBytes: addedBytes + removedBytes + grownDeltaBytes + shrunkDeltaBytes);

			diff = new ScanDiff(
				Root: normalizedRoot,
				HasBaseline: true,
				BaselineScannedAt: baselineTimestamp,
				CurrentScannedAt: currentScannedAt,
				Added: collapsedAdded,
				Removed: collapsedRemoved,
				Grown: grownEntries,
				Shrunk: shrunkEntries,
				Summary: summary);
		}

		// 7. Promote current snapshot to baseline
		_snapshotStore.SaveBaseline(normalizedRoot, depth, currentSnapshot);

		// 8. Cache the diff
		_diffCache[normalizedRoot] = diff;

		_logger.LogInformation(
			"Diff computed for {Root}: Added={Added}, Removed={Removed}, Grown={Grown}, Shrunk={Shrunk}, Net={Net}",
			normalizedRoot, diff.Summary.AddedCount, diff.Summary.RemovedCount,
			diff.Summary.GrownCount, diff.Summary.ShrunkCount, diff.Summary.NetDeltaBytes);

		return diff;
	}

	public ScanDiff? GetCachedDiff(string root)
	{
		var normalizedRoot = NormalizeRoot(root);
		return _diffCache.TryGetValue(normalizedRoot, out var diff) ? diff : null;
	}

	public bool DeleteBaseline(string root, int depth)
	{
		var normalizedRoot = NormalizeRoot(root);
		_diffCache.TryRemove(normalizedRoot, out _);
		return _snapshotStore.DeleteBaseline(normalizedRoot, depth);
	}

	/// <summary>
	/// Builds a flat snapshot from the engine's DirectorySizeCache for a given root.
	/// Each entry is a directory with its recursive byte total.
	/// </summary>
	private Dictionary<string, ScanSnapshotEntry> BuildCurrentSnapshot(string normalizedRoot)
	{
		var snapshot = new Dictionary<string, ScanSnapshotEntry>(StringComparer.OrdinalIgnoreCase);

		var rootNoSlash = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		foreach (var kvp in _engine.DirectorySizeCache)
		{
			var normalizedKey = kvp.Key.Replace("\\", "/");

			// Include entries that match the root exactly or are under the root
			if (normalizedKey.Equals(rootNoSlash, StringComparison.OrdinalIgnoreCase) ||
				normalizedKey.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
			{
				snapshot[normalizedKey] = new ScanSnapshotEntry(
					SizeBytes: kvp.Value.Size,
					IsDirectory: true,
					LastModified: new DateTimeOffset(kvp.Value.LastUpdated, TimeSpan.Zero));
			}
		}

		return snapshot;
	}

	/// <summary>
	/// Collapses Added/Removed entries: if a directory's ancestor is also in the list,
	/// the directory is suppressed and its ancestor gets a ChildCount.
	/// </summary>
	private static List<ScanDiffEntry> CollapseDirectories(
		List<ScanDiffEntry> entries,
		Dictionary<string, ScanSnapshotEntry> snapshotForCounting)
	{
		if (entries.Count == 0) return new List<ScanDiffEntry>();

		// Build a set of all paths in the entry list for fast lookup
		var pathSet = new HashSet<string>(entries.Select(e => e.Path), StringComparer.OrdinalIgnoreCase);

		var topLevel = new List<ScanDiffEntry>();
		var suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var entry in entries)
		{
			// Check if any ancestor of this entry is also in the entry list
			if (HasAncestorInSet(entry.Path, pathSet))
			{
				suppressed.Add(entry.Path);
			}
			else
			{
				topLevel.Add(entry);
			}
		}

		// For each top-level entry that is a directory, count descendants in the snapshot
		var result = new List<ScanDiffEntry>();
		foreach (var entry in topLevel)
		{
			if (entry.IsDirectory)
			{
				var prefix = entry.Path.EndsWith("/") ? entry.Path : entry.Path + "/";
				var childCount = snapshotForCounting.Keys
					.Count(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

				result.Add(entry with { ChildCount = childCount > 0 ? childCount : null });
			}
			else
			{
				result.Add(entry);
			}
		}

		return result;
	}

	/// <summary>
	/// Checks whether any ancestor directory of the given path exists in the path set.
	/// </summary>
	private static bool HasAncestorInSet(string path, HashSet<string> pathSet)
	{
		var normalized = path.Replace("\\", "/");
		var lastSlash = normalized.LastIndexOf('/');

		while (lastSlash > 0)
		{
			var parent = normalized[..lastSlash];
			if (pathSet.Contains(parent))
				return true;

			lastSlash = parent.LastIndexOf('/');
		}

		return false;
	}

	/// <summary>
	/// Normalizes a root path to forward slashes with trailing slash.
	/// E.g. "C:\" → "C:/", "/home/user" → "/home/user/"
	/// </summary>
	internal static string NormalizeRoot(string root)
	{
		var normalized = root.Replace("\\", "/");
		if (!normalized.EndsWith('/'))
			normalized += '/';
		return normalized;
	}
}

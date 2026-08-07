using System.IO;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

/// <summary>
/// Fine-grained, node-keyed directory cache service with lazy recursive size roll-ups,
/// targeted FileSystemWatcher invalidation, absolute TTL, and stampede protection.
/// </summary>
public interface IScanCacheService
{
	CachedDirNode? GetNode(string path);
	bool TryGetNode(string path, out CachedDirNode? node);
	void SetNode(CachedDirNode node, string? scanRoot = null);
	Task<CachedDirNode?> GetOrAddNodeAsync(
		string path,
		Func<string, CancellationToken, Task<CachedDirNode>> factory,
		string? scanRoot = null,
		CancellationToken ct = default);
	long GetOrRecomputeRecursiveBytes(string path);
	void MarkAncestorsStale(string path);
	bool HasScanRoot(string root);
	ScanRootMeta? GetScanRoot(string root, int? depth = null);
	void SetScanRoot(ScanRootMeta rootMeta);
	IEnumerable<ScanRootMeta> GetAllScanRoots();
	IEnumerable<CachedDirNode> GetNodesUnderRoot(string root);
	void InvalidatePath(string path, bool isDirectory);
	void InvalidatePrefix(string directoryPath);
	void InvalidatePaths(IEnumerable<string> paths);
	void InvalidateSubtree(string rootPath);
	void HandleWatcherEvent(string fullPath, WatcherChangeTypes changeType);
	void HandleWatcherOverflow(string watchedRoot);
	void EvictRoot(string root);
	void Clear();
	CacheStatsDto GetStats();
	event EventHandler<string>? OnSubtreeInvalidated;
}

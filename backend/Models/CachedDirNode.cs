using System.Text.Json.Serialization;

namespace GSSystemAnalyzer.Models;

/// <summary>
/// Represents a single leaf file held inline inside its parent directory node.
/// Never keyed individually in IMemoryCache to avoid entry explosion.
/// </summary>
public record CachedFileEntry(
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("extension")] string Extension,
	[property: JsonPropertyName("length")] long Length,
	[property: JsonPropertyName("lastModifiedUtc")] DateTime LastModifiedUtc
);

/// <summary>
/// Fine-grained cache entry for a single directory node.
/// </summary>
public record CachedDirNode(
	[property: JsonPropertyName("path")] string Path,
	[property: JsonPropertyName("childDirectoryPaths")] IReadOnlyList<string> ChildDirectoryPaths,
	[property: JsonPropertyName("files")] IReadOnlyList<CachedFileEntry> Files,
	[property: JsonPropertyName("ownBytes")] long OwnBytes,
	[property: JsonPropertyName("recursiveBytes")] long RecursiveBytes,
	[property: JsonPropertyName("cachedAt")] DateTimeOffset CachedAt,
	[property: JsonPropertyName("recursiveBytesStale")] bool RecursiveBytesStale
);

/// <summary>
/// Top-level scan metadata serving as the single source of truth for the 409 gate.
/// </summary>
public record ScanRootMeta(
	[property: JsonPropertyName("driveRoot")] string DriveRoot,
	[property: JsonPropertyName("depth")] int Depth,
	[property: JsonPropertyName("scannedAt")] DateTimeOffset ScannedAt,
	[property: JsonPropertyName("totalBytes")] long TotalBytes,
	[property: JsonPropertyName("totalFiles")] long TotalFiles,
	[property: JsonPropertyName("rootNodeKey")] string RootNodeKey
);

/// <summary>
/// Cache diagnostics and hit/miss metrics reported by GET /api/cache/stats.
/// </summary>
public record CacheStatsDto(
	[property: JsonPropertyName("entryCount")] int EntryCount,
	[property: JsonPropertyName("nodeCount")] int NodeCount,
	[property: JsonPropertyName("rootCount")] int RootCount,
	[property: JsonPropertyName("approximateBytes")] long ApproximateBytes,
	[property: JsonPropertyName("hitCount")] long HitCount,
	[property: JsonPropertyName("missCount")] long MissCount,
	[property: JsonPropertyName("hitMissRatio")] double HitMissRatio,
	[property: JsonPropertyName("oldestCachedAt")] DateTimeOffset? OldestCachedAt
);

using System;
using System.Collections.Generic;

namespace GSSystemAnalyzer.Models;

public enum DiffKind
{
	Added,
	Removed,
	Grown,
	Shrunk
}


public record ScanSnapshotEntry(long SizeBytes, bool IsDirectory, DateTimeOffset LastModified);
public record ScanSnapshot(DateTimeOffset CapturedAt, Dictionary<string, ScanSnapshotEntry> Entries);

public record ScanDiffEntry(
	string Path,
	bool IsDirectory,
	DiffKind Kind,
	long CurrentBytes,       
	long PreviousBytes,       
	long DeltaBytes,          
	int? ChildCount,          
	DateTimeOffset? LastModified);

public record ScanDiffSummary(
	int AddedCount, long AddedBytes,
	int RemovedCount, long RemovedBytes,
	int GrownCount, long GrownDeltaBytes,
	int ShrunkCount, long ShrunkDeltaBytes,
	long NetDeltaBytes);


public record ScanDiff(
	string Root,
	bool HasBaseline,
	DateTimeOffset? BaselineScannedAt,
	DateTimeOffset CurrentScannedAt,
	IReadOnlyList<ScanDiffEntry> Added,
	IReadOnlyList<ScanDiffEntry> Removed,
	IReadOnlyList<ScanDiffEntry> Grown,
	IReadOnlyList<ScanDiffEntry> Shrunk,
	ScanDiffSummary Summary);

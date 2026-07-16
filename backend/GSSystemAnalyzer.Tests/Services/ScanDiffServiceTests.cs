using System.Collections.Concurrent;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class ScanDiffServiceTests : IDisposable
{
	private readonly string _snapshotDir;
	private readonly DiskScannerEngine _engine;
	private readonly ScanSnapshotStore _snapshotStore;

	public ScanDiffServiceTests()
	{
		_snapshotDir = Path.Combine(Path.GetTempPath(), "GSAnalyzer_DiffTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_snapshotDir);

		var hubMock = new Mock<IHubContext<SystemHub>>();
		var settingsMock = new Mock<ISettingService>();
		settingsMock.Setup(s => s.Current).Returns(new AppSettingDto());

		_engine = new DiskScannerEngine(hubMock.Object, settingsMock.Object, NullLogger<DiskScannerEngine>.Instance);
		_engine.DirectorySizeCache.Clear();

		_snapshotStore = new ScanSnapshotStore(NullLogger<ScanSnapshotStore>.Instance, _snapshotDir);
	}

	public void Dispose()
	{
		if (Directory.Exists(_snapshotDir))
			Directory.Delete(_snapshotDir, recursive: true);
	}

	private ScanDiffService CreateService() =>
		new(_engine, _snapshotStore, NullLogger<ScanDiffService>.Instance);

	/// <summary>
	/// Helper to populate the engine cache with directory entries.
	/// </summary>
	private void SetCacheEntry(string path, long size, DateTime? lastUpdated = null)
	{
		_engine.DirectorySizeCache[path] = new CacheEntry
		{
			Size = size,
			LastUpdated = lastUpdated ?? DateTime.UtcNow,
			CachedAtUtc = DateTime.UtcNow,
			ScanRoot = "C:\\"
		};
	}

	// ─── First scan tests ───

	[Fact]
	public void FirstScan_ReturnsHasBaselineFalse_AndSavesBaseline()
	{
		var service = CreateService();

		SetCacheEntry("C:/Users", 1000);
		SetCacheEntry("C:/ProgramData", 2000);

		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.False(diff.HasBaseline);
		Assert.Null(diff.BaselineScannedAt);
		Assert.Empty(diff.Added);
		Assert.Empty(diff.Removed);
		Assert.Empty(diff.Grown);
		Assert.Empty(diff.Shrunk);
		Assert.Equal(0, diff.Summary.NetDeltaBytes);

		// Baseline should now exist
		var baseline = _snapshotStore.LoadBaseline("C:/", 10);
		Assert.NotNull(baseline);
		Assert.Equal(2, baseline.Count);
	}

	// ─── Classification tests ───

	[Fact]
	public void SecondScan_DetectsAddedDirectories()
	{
		var service = CreateService();

		// First scan — just C:/Users
		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — C:/Users + C:/NewFolder
		SetCacheEntry("C:/NewFolder", 5000);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.True(diff.HasBaseline);
		Assert.Single(diff.Added);
		Assert.Equal("C:/NewFolder", diff.Added[0].Path);
		Assert.Equal(DiffKind.Added, diff.Added[0].Kind);
		Assert.Equal(5000, diff.Added[0].CurrentBytes);
		Assert.Equal(0, diff.Added[0].PreviousBytes);
		Assert.Equal(5000, diff.Added[0].DeltaBytes);
	}

	[Fact]
	public void SecondScan_DetectsRemovedDirectories()
	{
		var service = CreateService();

		// First scan — C:/Users + C:/OldBackup
		SetCacheEntry("C:/Users", 1000);
		SetCacheEntry("C:/OldBackup", 3000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — C:/OldBackup is gone
		_engine.DirectorySizeCache.TryRemove("C:/OldBackup", out _);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.True(diff.HasBaseline);
		Assert.Single(diff.Removed);
		Assert.Equal("C:/OldBackup", diff.Removed[0].Path);
		Assert.Equal(DiffKind.Removed, diff.Removed[0].Kind);
		Assert.Equal(0, diff.Removed[0].CurrentBytes);
		Assert.Equal(3000, diff.Removed[0].PreviousBytes);
		Assert.Equal(-3000, diff.Removed[0].DeltaBytes);
	}

	[Fact]
	public void SecondScan_DetectsGrownDirectories()
	{
		var service = CreateService();

		// First scan
		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — C:/Users grew
		SetCacheEntry("C:/Users", 5000);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.True(diff.HasBaseline);
		Assert.Single(diff.Grown);
		Assert.Equal("C:/Users", diff.Grown[0].Path);
		Assert.Equal(DiffKind.Grown, diff.Grown[0].Kind);
		Assert.Equal(5000, diff.Grown[0].CurrentBytes);
		Assert.Equal(1000, diff.Grown[0].PreviousBytes);
		Assert.Equal(4000, diff.Grown[0].DeltaBytes);
	}

	[Fact]
	public void SecondScan_DetectsShrunkDirectories()
	{
		var service = CreateService();

		// First scan
		SetCacheEntry("C:/Users", 10000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — C:/Users shrank
		SetCacheEntry("C:/Users", 3000);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.True(diff.HasBaseline);
		Assert.Single(diff.Shrunk);
		Assert.Equal("C:/Users", diff.Shrunk[0].Path);
		Assert.Equal(DiffKind.Shrunk, diff.Shrunk[0].Kind);
		Assert.Equal(3000, diff.Shrunk[0].CurrentBytes);
		Assert.Equal(10000, diff.Shrunk[0].PreviousBytes);
		Assert.Equal(-7000, diff.Shrunk[0].DeltaBytes);
	}

	[Fact]
	public void UnchangedDirectories_AreNotEmitted()
	{
		var service = CreateService();

		SetCacheEntry("C:/Users", 1000);
		SetCacheEntry("C:/Stable", 5000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — same sizes
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.True(diff.HasBaseline);
		Assert.Empty(diff.Added);
		Assert.Empty(diff.Removed);
		Assert.Empty(diff.Grown);
		Assert.Empty(diff.Shrunk);
		Assert.Equal(0, diff.Summary.NetDeltaBytes);
	}

	// ─── Directory collapse tests ───

	[Fact]
	public void AddedDirectory_CollapsesDescendants()
	{
		var service = CreateService();

		// First scan — just C:/Users
		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — a whole new directory tree appears
		SetCacheEntry("C:/dev/project", 500);
		SetCacheEntry("C:/dev/project/src", 200);
		SetCacheEntry("C:/dev/project/bin", 100);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Should collapse into the top-level "C:/dev/project" with childCount
		// (C:/dev/project/src and C:/dev/project/bin are descendants)
		var addedPaths = diff.Added.Select(e => e.Path).ToList();
		Assert.Contains("C:/dev/project", addedPaths);
		Assert.DoesNotContain("C:/dev/project/src", addedPaths);
		Assert.DoesNotContain("C:/dev/project/bin", addedPaths);

		var topEntry = diff.Added.Single(e => e.Path == "C:/dev/project");
		Assert.NotNull(topEntry.ChildCount);
		Assert.Equal(2, topEntry.ChildCount); // src + bin
	}

	[Fact]
	public void RemovedDirectory_CollapsesDescendants()
	{
		var service = CreateService();

		// First scan — a whole directory tree exists
		SetCacheEntry("C:/Users", 1000);
		SetCacheEntry("C:/old_backup", 5000);
		SetCacheEntry("C:/old_backup/photos", 3000);
		SetCacheEntry("C:/old_backup/docs", 2000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — the whole tree is removed
		_engine.DirectorySizeCache.TryRemove("C:/old_backup", out _);
		_engine.DirectorySizeCache.TryRemove("C:/old_backup/photos", out _);
		_engine.DirectorySizeCache.TryRemove("C:/old_backup/docs", out _);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Should collapse to single "C:/old_backup" entry with childCount
		var removedPaths = diff.Removed.Select(e => e.Path).ToList();
		Assert.Contains("C:/old_backup", removedPaths);
		Assert.DoesNotContain("C:/old_backup/photos", removedPaths);
		Assert.DoesNotContain("C:/old_backup/docs", removedPaths);

		var topEntry = diff.Removed.Single(e => e.Path == "C:/old_backup");
		Assert.NotNull(topEntry.ChildCount);
		Assert.Equal(2, topEntry.ChildCount);
	}

	// ─── Summary tests ───

	[Fact]
	public void Summary_NetDeltaBytes_MatchesSum()
	{
		var service = CreateService();

		// First scan
		SetCacheEntry("C:/Users", 1000);
		SetCacheEntry("C:/Temp", 5000);
		SetCacheEntry("C:/OldBackup", 3000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — mixed changes
		SetCacheEntry("C:/Users", 2000);    // Grown by 1000
		SetCacheEntry("C:/Temp", 3000);     // Shrunk by 2000
		SetCacheEntry("C:/NewFolder", 4000); // Added 4000
		_engine.DirectorySizeCache.TryRemove("C:/OldBackup", out _); // Removed 3000

		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		var expectedNet = diff.Summary.AddedBytes + diff.Summary.RemovedBytes +
						  diff.Summary.GrownDeltaBytes + diff.Summary.ShrunkDeltaBytes;

		Assert.Equal(expectedNet, diff.Summary.NetDeltaBytes);

		// Verify individual counts
		Assert.Equal(1, diff.Summary.AddedCount);
		Assert.Equal(1, diff.Summary.RemovedCount);
		Assert.Equal(1, diff.Summary.GrownCount);
		Assert.Equal(1, diff.Summary.ShrunkCount);
	}

	// ─── Baseline management tests ───

	[Fact]
	public void DeleteBaseline_ClearsFile_NextScanIsFirstScan()
	{
		var service = CreateService();

		// First scan — establishes baseline
		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Delete baseline
		var deleted = service.DeleteBaseline("C:/", 10);
		Assert.True(deleted);

		// Next diff should be a "first scan" again
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);
		Assert.False(diff.HasBaseline);
	}

	// ─── Cache tests ───

	[Fact]
	public void GetCachedDiff_ReturnsNullBeforeFirstCompute()
	{
		var service = CreateService();

		var cached = service.GetCachedDiff("C:/");

		Assert.Null(cached);
	}

	[Fact]
	public void GetCachedDiff_ReturnsDiffAfterCompute()
	{
		var service = CreateService();

		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		var cached = service.GetCachedDiff("C:/");

		Assert.NotNull(cached);
		Assert.Equal("C:/", cached.Root);
	}

	// ─── Sorting tests ───

	[Fact]
	public void DiffIsSortedByMagnitude_AddedBySizeDesc()
	{
		var service = CreateService();

		// First scan — empty
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Second scan — multiple additions
		SetCacheEntry("C:/Small", 100);
		SetCacheEntry("C:/Medium", 5000);
		SetCacheEntry("C:/Large", 50000);

		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.Equal(3, diff.Added.Count);
		Assert.Equal("C:/Large", diff.Added[0].Path);
		Assert.Equal("C:/Medium", diff.Added[1].Path);
		Assert.Equal("C:/Small", diff.Added[2].Path);
	}

	[Fact]
	public void DiffIsSortedByMagnitude_GrownByAbsDeltaDesc()
	{
		var service = CreateService();

		SetCacheEntry("C:/A", 1000);
		SetCacheEntry("C:/B", 1000);
		SetCacheEntry("C:/C", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Grow by different amounts
		SetCacheEntry("C:/A", 1100);   // delta = 100
		SetCacheEntry("C:/B", 11000);  // delta = 10000
		SetCacheEntry("C:/C", 2000);   // delta = 1000

		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.Equal(3, diff.Grown.Count);
		Assert.Equal("C:/B", diff.Grown[0].Path);  // biggest delta first
		Assert.Equal("C:/C", diff.Grown[1].Path);
		Assert.Equal("C:/A", diff.Grown[2].Path);
	}

	// ─── MinDeltaBytes filtering (applied at controller level, but let's verify summary stays intact) ───

	[Fact]
	public void MinDeltaBytes_FiltersSmallMovers_SummaryRemainsAccurate()
	{
		var service = CreateService();

		SetCacheEntry("C:/Big", 1000);
		SetCacheEntry("C:/Small", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		SetCacheEntry("C:/Big", 10000);    // delta = 9000
		SetCacheEntry("C:/Small", 1010);   // delta = 10

		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// Both are Grown — summary has 2 entries
		Assert.Equal(2, diff.Summary.GrownCount);
		Assert.Equal(9010, diff.Summary.GrownDeltaBytes);

		// Apply minDeltaBytes filter manually (as controller would)
		var filtered = diff.Grown.Where(e => Math.Abs(e.DeltaBytes) >= 100).ToList();

		// Only the big mover passes the filter
		Assert.Single(filtered);
		Assert.Equal("C:/Big", filtered[0].Path);

		// Summary remains intact (controller doesn't recompute it)
		Assert.Equal(2, diff.Summary.GrownCount);
	}

	// ─── Cross-platform path tests ───

	[Fact]
	public void CrossPlatformPaths_WindowsStyle_WorkCorrectly()
	{
		var service = CreateService();

		SetCacheEntry("C:/Users/foo", 1000);
		var diff = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.False(diff.HasBaseline);
		Assert.Equal("C:/", diff.Root);

		// Baseline should be saved
		Assert.NotNull(_snapshotStore.LoadBaseline("C:/", 10));
	}

	[Fact]
	public void CrossPlatformPaths_LinuxStyle_WorkCorrectly()
	{
		var service = CreateService();

		SetCacheEntry("/home/user/docs", 2000);
		var diff = service.ComputeDiff("/home/user/", 10, DateTimeOffset.UtcNow);

		Assert.False(diff.HasBaseline);
		Assert.Equal("/home/user/", diff.Root);

		Assert.NotNull(_snapshotStore.LoadBaseline("/home/user/", 10));
	}

	// ─── Baseline promotion tests ───

	[Fact]
	public void BaselineIsPromoted_AfterDiffComputed()
	{
		var service = CreateService();

		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		// First diff was no-baseline. Now baseline should reflect current state.
		SetCacheEntry("C:/Users", 2000);
		var diff2 = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.True(diff2.HasBaseline);
		Assert.Single(diff2.Grown);
		Assert.Equal(1000, diff2.Grown[0].DeltaBytes); // grew from 1000 to 2000

		// Third diff — C:/Users didn't change, so nothing
		var diff3 = service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);
		Assert.True(diff3.HasBaseline);
		Assert.Empty(diff3.Grown);
	}

	[Fact]
	public void DeleteBaseline_AlsoClearsDiffCache()
	{
		var service = CreateService();

		SetCacheEntry("C:/Users", 1000);
		service.ComputeDiff("C:/", 10, DateTimeOffset.UtcNow);

		Assert.NotNull(service.GetCachedDiff("C:/"));

		service.DeleteBaseline("C:/", 10);

		Assert.Null(service.GetCachedDiff("C:/"));
	}
}

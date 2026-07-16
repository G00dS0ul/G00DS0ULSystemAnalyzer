using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class ScanSnapshotStoreTests : IDisposable
{
	private readonly string _tempDir;

	public ScanSnapshotStoreTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "GSAnalyzer_SnapshotTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose()
	{
		if (Directory.Exists(_tempDir))
			Directory.Delete(_tempDir, recursive: true);
	}

	private ScanSnapshotStore CreateStore() =>
		new(NullLogger<ScanSnapshotStore>.Instance, _tempDir);

	private static Dictionary<string, ScanSnapshotEntry> BuildSampleSnapshot() => new()
	{
		["C:/Users"] = new ScanSnapshotEntry(1024_000, true, DateTimeOffset.UtcNow),
		["C:/Users/Documents"] = new ScanSnapshotEntry(512_000, true, DateTimeOffset.UtcNow.AddMinutes(-5)),
		["C:/ProgramData"] = new ScanSnapshotEntry(2048_000, true, DateTimeOffset.UtcNow.AddHours(-1))
	};

	[Fact]
	public void SaveAndLoad_Roundtrip_ProducesIdenticalData()
	{
		var store = CreateStore();
		var snapshot = BuildSampleSnapshot();

		store.SaveBaseline("C:/", 10, snapshot);
		var loaded = store.LoadBaseline("C:/", 10);

		Assert.NotNull(loaded);
		Assert.Equal(snapshot.Count, loaded.Count);

		foreach (var kvp in snapshot)
		{
			Assert.True(loaded.ContainsKey(kvp.Key), $"Key {kvp.Key} missing from loaded snapshot");
			Assert.Equal(kvp.Value.SizeBytes, loaded[kvp.Key].SizeBytes);
			Assert.Equal(kvp.Value.IsDirectory, loaded[kvp.Key].IsDirectory);
		}
	}

	[Fact]
	public void Load_MissingFile_ReturnsNull()
	{
		var store = CreateStore();

		var result = store.LoadBaseline("D:/", 5);

		Assert.Null(result);
	}

	[Fact]
	public void Load_CorruptFile_ReturnsNull()
	{
		var store = CreateStore();

		// Write garbage to the expected file location
		var key = ScanSnapshotStore.SanitizeRootToKey("C:/");
		var filePath = Path.Combine(_tempDir, $"{key}_10.json");
		File.WriteAllText(filePath, "{ THIS IS NOT VALID JSON!!! [broken}");

		var result = store.LoadBaseline("C:/", 10);

		Assert.Null(result);
	}

	[Fact]
	public void Delete_ExistingFile_ReturnsTrue()
	{
		var store = CreateStore();
		store.SaveBaseline("C:/", 10, BuildSampleSnapshot());

		var result = store.DeleteBaseline("C:/", 10);

		Assert.True(result);

		// Should be gone now
		Assert.Null(store.LoadBaseline("C:/", 10));
	}

	[Fact]
	public void Delete_MissingFile_ReturnsFalse()
	{
		var store = CreateStore();

		var result = store.DeleteBaseline("Z:/", 5);

		Assert.False(result);
	}

	[Fact]
	public void GetBaselineTimestamp_ReturnsRecentTimestamp_AfterSave()
	{
		var store = CreateStore();
		var before = DateTimeOffset.UtcNow.AddSeconds(-1);

		store.SaveBaseline("C:/", 10, BuildSampleSnapshot());

		var timestamp = store.GetBaselineTimestamp("C:/", 10);

		Assert.NotNull(timestamp);
		Assert.True(timestamp.Value >= before, "Timestamp should be recent");
	}

	[Fact]
	public void GetBaselineTimestamp_MissingFile_ReturnsNull()
	{
		var store = CreateStore();

		var result = store.GetBaselineTimestamp("X:/", 10);

		Assert.Null(result);
	}

	[Fact]
	public void AtomicWrite_ProducesValidJson_NoTmpLeftover()
	{
		var store = CreateStore();
		store.SaveBaseline("C:/", 10, BuildSampleSnapshot());

		// Verify the file exists and is valid JSON
		var key = ScanSnapshotStore.SanitizeRootToKey("C:/");
		var filePath = Path.Combine(_tempDir, $"{key}_10.json");
		Assert.True(File.Exists(filePath));

		var json = File.ReadAllText(filePath);
		Assert.False(string.IsNullOrWhiteSpace(json));

		// Should not leave any .tmp files behind
		var tmpFiles = Directory.GetFiles(_tempDir, "*.tmp");
		Assert.Empty(tmpFiles);
	}

	[Fact]
	public void DifferentDepths_ProduceSeparateBaselines()
	{
		var store = CreateStore();
		var snapshot5 = new Dictionary<string, ScanSnapshotEntry>
		{
			["C:/shallow"] = new ScanSnapshotEntry(100, true, DateTimeOffset.UtcNow)
		};
		var snapshot10 = new Dictionary<string, ScanSnapshotEntry>
		{
			["C:/deep"] = new ScanSnapshotEntry(200, true, DateTimeOffset.UtcNow)
		};

		store.SaveBaseline("C:/", 5, snapshot5);
		store.SaveBaseline("C:/", 10, snapshot10);

		var loaded5 = store.LoadBaseline("C:/", 5);
		var loaded10 = store.LoadBaseline("C:/", 10);

		Assert.NotNull(loaded5);
		Assert.NotNull(loaded10);
		Assert.True(loaded5.ContainsKey("C:/shallow"));
		Assert.False(loaded5.ContainsKey("C:/deep"));
		Assert.True(loaded10.ContainsKey("C:/deep"));
		Assert.False(loaded10.ContainsKey("C:/shallow"));
	}

	[Fact]
	public void SanitizeRootToKey_WindowsPath_ProducesValidFilename()
	{
		Assert.Equal("C", ScanSnapshotStore.SanitizeRootToKey("C:/"));
		Assert.Equal("C", ScanSnapshotStore.SanitizeRootToKey("C:\\"));
		Assert.Equal("D_Users_foo", ScanSnapshotStore.SanitizeRootToKey("D:/Users/foo"));
	}

	[Fact]
	public void SanitizeRootToKey_LinuxPath_ProducesValidFilename()
	{
		Assert.Equal("_home_user", ScanSnapshotStore.SanitizeRootToKey("/home/user"));
		Assert.Equal("_root", ScanSnapshotStore.SanitizeRootToKey("/"));
	}

	[Fact]
	public void PersistenceRoundtrip_SurvivesNewStoreInstance()
	{
		var store1 = CreateStore();
		store1.SaveBaseline("C:/", 10, BuildSampleSnapshot());

		// Create a new store instance pointing to the same directory
		var store2 = CreateStore();
		var loaded = store2.LoadBaseline("C:/", 10);

		Assert.NotNull(loaded);
		Assert.Equal(3, loaded.Count);
	}
}

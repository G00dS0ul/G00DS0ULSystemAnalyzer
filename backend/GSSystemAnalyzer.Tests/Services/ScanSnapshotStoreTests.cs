using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;

namespace GSSystemAnalyzer.Tests.Services;

public class ScanSnapshotStoreTests : IDisposable
{
	private readonly string _tempDir;

	public ScanSnapshotStoreTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "gssnap_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose()
	{
		try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
	}

	private static string Root => OperatingSystem.IsWindows() ? @"C:\" : "/";

	private static ScanSnapshot Sample(long size = 1000) =>
		new(DateTimeOffset.UtcNow, new Dictionary<string, ScanSnapshotEntry>
		{
			["c:/foo"] = new ScanSnapshotEntry(size, true, DateTimeOffset.UtcNow)
		});

	[Fact]
	public void Load_ReturnsNull_WhenNoSnapshotExists()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		Assert.Null(store.Load(Root, 10));
	}

	[Fact]
	public void SaveThenLoad_RoundTripsEntries()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		store.Save(Root, 10, Sample(2048));

		var loaded = store.Load(Root, 10);

		Assert.NotNull(loaded);
		Assert.True(loaded!.Entries.ContainsKey("c:/foo"));
		Assert.Equal(2048, loaded.Entries["c:/foo"].SizeBytes);
	}

	[Fact]
	public void Save_OverwritesPreviousBaseline_Atomically()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		store.Save(Root, 10, Sample(1000));
		store.Save(Root, 10, Sample(9999));

		var loaded = store.Load(Root, 10);
		Assert.Equal(9999, loaded!.Entries["c:/foo"].SizeBytes);

		// Exactly one baseline file per (root, depth); no leftover temp files.
		Assert.Single(Directory.GetFiles(_tempDir, "*.json"));
		Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp"));
	}

	[Fact]
	public void DifferentDepths_StoreSeparateBaselines()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		store.Save(Root, 5, Sample(100));
		store.Save(Root, 10, Sample(200));

		Assert.Equal(100, store.Load(Root, 5)!.Entries["c:/foo"].SizeBytes);
		Assert.Equal(200, store.Load(Root, 10)!.Entries["c:/foo"].SizeBytes);
	}

	[Fact]
	public void Delete_RemovesBaseline()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		store.Save(Root, 10, Sample());
		store.Delete(Root, 10);

		Assert.Null(store.Load(Root, 10));
	}

	[Fact]
	public void Delete_IsNoOp_WhenAbsent()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		var ex = Record.Exception(() => store.Delete(Root, 10));
		Assert.Null(ex);
	}

	[Fact]
	public void Load_DiscardsCorruptFile_AndReturnsNull()
	{
		var store = new ScanSnapshotStore(testDir: _tempDir);
		store.Save(Root, 10, Sample());

		// Corrupt the on-disk baseline.
		var file = Directory.GetFiles(_tempDir, "*.json").Single();
		File.WriteAllText(file, "{ this is not valid json ]");

		Assert.Null(store.Load(Root, 10));
		Assert.False(File.Exists(file)); // corrupt file is discarded
	}

	[Fact]
	public void NormalizeDriveKey_IsCaseAndSeparatorInsensitive()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Equal(
				ScanSnapshotStore.NormalizeDriveKey(@"C:\Users\Foo"),
				ScanSnapshotStore.NormalizeDriveKey("c:/users/foo/"));
		}
		else
		{
			Assert.Equal(
				ScanSnapshotStore.NormalizeDriveKey("/home/Foo"),
				ScanSnapshotStore.NormalizeDriveKey("/home/foo/"));
		}
	}
}

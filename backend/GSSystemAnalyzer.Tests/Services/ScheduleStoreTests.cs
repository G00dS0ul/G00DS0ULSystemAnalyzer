using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class ScheduleStoreTests : IDisposable
{
	private readonly string _tempDir;
	private readonly string _tempFile;

	public ScheduleStoreTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "GSAnalyzer_StoreTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
		_tempFile = Path.Combine(_tempDir, "test_schedules.json");
	}

	public void Dispose()
	{
		if (Directory.Exists(_tempDir))
			Directory.Delete(_tempDir, recursive: true);
	}

	private ScheduleStore CreateStore() =>
		new(NullLogger<ScheduleStore>.Instance, _tempFile);

	[Fact]
	public void SaveAndLoad_Roundtrip_ProducesIdenticalData()
	{
		var store = CreateStore();

		var schedules = new List<ScheduledScan>
		{
			new()
			{
				Id = Guid.NewGuid(),
				Type = ScanType.Directory,
				Path = "C:/",
				Kind = ScheduleKind.Interval,
				IntervalMinutes = 30,
				Enabled = true,
				LastRun = DateTimeOffset.UtcNow.AddMinutes(-10),
				NextRun = DateTimeOffset.UtcNow.AddMinutes(20)
			},
			new()
			{
				Id = Guid.NewGuid(),
				Type = ScanType.LargeFiles,
				Path = "D:/",
				Kind = ScheduleKind.Cron,
				Cron = "0 3 * * *",
				Enabled = false,
				LastRun = null,
				NextRun = null
			}
		};

		store.SaveAll(schedules);

		var loaded = store.LoadAll();

		Assert.Equal(schedules.Count, loaded.Count);
		Assert.Equal(schedules[0].Id, loaded[0].Id);
		Assert.Equal(schedules[0].Path, loaded[0].Path);
		Assert.Equal(schedules[0].Kind, loaded[0].Kind);
		Assert.Equal(schedules[0].IntervalMinutes, loaded[0].IntervalMinutes);
		Assert.Equal(schedules[0].Enabled, loaded[0].Enabled);
		Assert.Equal(schedules[1].Id, loaded[1].Id);
		Assert.Equal(schedules[1].Cron, loaded[1].Cron);
		Assert.Equal(schedules[1].Type, loaded[1].Type);
	}

	[Fact]
	public void LoadFromMissingFile_ReturnsEmptyList()
	{
		var store = CreateStore();

		var result = store.LoadAll();

		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public void LoadFromCorruptFile_ReturnsEmptyList()
	{
		File.WriteAllText(_tempFile, "{ THIS IS NOT VALID JSON!!! [broken}");

		var store = CreateStore();
		var result = store.LoadAll();

		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public void AtomicWrite_ProducesValidJsonFile()
	{
		var store = CreateStore();

		var schedules = new List<ScheduledScan>
		{
			new()
			{
				Id = Guid.NewGuid(),
				Path = "C:/",
				Kind = ScheduleKind.Interval,
				IntervalMinutes = 15,
				Enabled = true
			}
		};

		store.SaveAll(schedules);

		Assert.True(File.Exists(_tempFile));
		var json = File.ReadAllText(_tempFile);
		Assert.False(string.IsNullOrWhiteSpace(json));

		// Should not leave any .tmp files behind
		var tmpFiles = Directory.GetFiles(_tempDir, "*.tmp");
		Assert.Empty(tmpFiles);
	}

	[Fact]
	public void ConcurrentWrites_NeverCorruptFile()
	{
		var store = CreateStore();

		var tasks = new List<Task>();
		for (int i = 0; i < 50; i++)
		{
			var iteration = i;
			tasks.Add(Task.Run(() =>
			{
				var schedules = new List<ScheduledScan>
				{
					new()
					{
						Id = Guid.NewGuid(),
						Path = $"Drive_{iteration}/",
						Kind = ScheduleKind.Interval,
						IntervalMinutes = iteration + 1,
						Enabled = true
					}
				};
				store.SaveAll(schedules);
			}));
		}

		Task.WaitAll(tasks.ToArray());

		// After all concurrent writes, file should be readable and valid
		var loaded = store.LoadAll();
		Assert.NotNull(loaded);
		Assert.Single(loaded); // Last writer wins — exactly 1 schedule
	}

	[Fact]
	public void Delete_RemovesMidList_RoundtripsCorrectly()
	{
		var store = CreateStore();

		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		var id3 = Guid.NewGuid();

		var schedules = new List<ScheduledScan>
		{
			new() { Id = id1, Path = "C:/", Kind = ScheduleKind.Interval, IntervalMinutes = 10 },
			new() { Id = id2, Path = "D:/", Kind = ScheduleKind.Interval, IntervalMinutes = 20 },
			new() { Id = id3, Path = "E:/", Kind = ScheduleKind.Interval, IntervalMinutes = 30 }
		};

		store.SaveAll(schedules);

		// Remove the middle one
		schedules.RemoveAll(s => s.Id == id2);
		store.SaveAll(schedules);

		var loaded = store.LoadAll();
		Assert.Equal(2, loaded.Count);
		Assert.DoesNotContain(loaded, s => s.Id == id2);
	}

	[Fact]
	public void PersistenceRoundtrip_SurvivesNewStoreInstance()
	{
		var store1 = CreateStore();
		var id = Guid.NewGuid();

		store1.SaveAll(new List<ScheduledScan>
		{
			new()
			{
				Id = id,
				Path = "C:/",
				Kind = ScheduleKind.Cron,
				Cron = "*/5 * * * *",
				Enabled = true
			}
		});

		// Create a new store instance pointing to the same file
		var store2 = CreateStore();
		var loaded = store2.LoadAll();

		Assert.Single(loaded);
		Assert.Equal(id, loaded[0].Id);
		Assert.Equal("*/5 * * * *", loaded[0].Cron);
	}
}

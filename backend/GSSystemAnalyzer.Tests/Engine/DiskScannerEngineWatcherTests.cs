using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GSSystemAnalyzer.Tests.Engine;

public sealed class DiskScannerEngineWatcherTests : IDisposable
{
	private readonly string _tempRoot = Path.Combine(
		Path.GetTempPath(),
		$"gs-radar-测试-{Guid.NewGuid():N}");
	private readonly List<WatcherHarness> _harnesses = [];

	public DiskScannerEngineWatcherTests()
	{
		Directory.CreateDirectory(_tempRoot);
	}

	[Fact]
	public async Task NestedChange_RefreshesWatchedRoot()
	{
		var nested = Directory.CreateDirectory(Path.Combine(_tempRoot, "nested"));
		var harness = CreateHarness();
		harness.Engine.MoveRadarToSector(_tempRoot);
		await Task.Delay(100);

		await File.WriteAllTextAsync(Path.Combine(nested.FullName, "sample.txt"), "data");

		var changedSector = await harness.FirstSectorChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(Normalize(_tempRoot), changedSector);
	}

	[Fact]
	public async Task RapidBurst_IsBroadcastOnceAfterQuietPeriod()
	{
		var harness = CreateHarness();
		harness.Engine.MoveRadarToSector(_tempRoot);
		var watcher = GetWatcher(harness.Engine);

		for (var i = 0; i < 3; i++)
		{
			TriggerWatcherEvent(harness.Engine, watcher, $"burst-{i}.txt");
			if (i < 2) await Task.Delay(75);
		}

		var lastWrite = Stopwatch.GetTimestamp();
		await Task.Delay(300);
		Assert.Empty(harness.Broadcasts);

		await harness.FirstSectorChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var firstBroadcast = Assert.Single(harness.Broadcasts);
		Assert.True(
			Stopwatch.GetElapsedTime(lastWrite, firstBroadcast.Timestamp) >= TimeSpan.FromMilliseconds(350),
			"The refresh should be sent after the burst's trailing quiet period.");

		await Task.Delay(700);
		Assert.Single(harness.Broadcasts);
	}

	[Fact]
	public async Task WatcherError_InvalidatesAndRefreshesWatchedRoot()
	{
		var harness = CreateHarness();
		harness.Engine.MoveRadarToSector(_tempRoot);

		var watcher = GetWatcher(harness.Engine);
		Assert.Equal(16 * 1024, watcher.InternalBufferSize);

		var raiseError = typeof(FileSystemWatcher).GetMethod(
			"OnError",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(raiseError);
		raiseError.Invoke(watcher, [new ErrorEventArgs(new InternalBufferOverflowException())]);

		harness.Cache.Verify(c => c.HandleWatcherOverflow(_tempRoot), Times.Once);
		var changedSector = await harness.FirstSectorChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(Normalize(_tempRoot), changedSector);
	}

	private WatcherHarness CreateHarness()
	{
		var proxy = new Mock<IClientProxy>();
		var clients = new Mock<IHubClients>();
		clients.SetupGet(c => c.All).Returns(proxy.Object);

		var hub = new Mock<IHubContext<SystemHub>>();
		hub.SetupGet(h => h.Clients).Returns(clients.Object);

		var cache = new Mock<IScanCacheService>();
		var settings = new Mock<ISettingService>();
		var harness = new WatcherHarness(
			new DiskScannerEngine(
				hub.Object,
				settings.Object,
				NullLogger<DiskScannerEngine>.Instance,
				cache.Object),
			cache);

		proxy
			.Setup(p => p.SendCoreAsync(
				"SectorChanged",
				It.IsAny<object?[]>(),
				It.IsAny<CancellationToken>()))
			.Callback<string, object?[], CancellationToken>((_, arguments, _) =>
			{
				var path = Assert.IsType<string>(arguments[0]);
				harness.Broadcasts.Enqueue((Stopwatch.GetTimestamp(), path));
				harness.FirstSectorChanged.TrySetResult(path);
			})
			.Returns(Task.CompletedTask);

		_harnesses.Add(harness);
		return harness;
	}

	private static string Normalize(string path) => path.Replace("\\", "/");

	private static FileSystemWatcher GetWatcher(DiskScannerEngine engine)
	{
		var watcherField = typeof(DiskScannerEngine).GetField(
			"_liveRader",
			BindingFlags.Instance | BindingFlags.NonPublic);
		return Assert.IsType<FileSystemWatcher>(watcherField?.GetValue(engine));
	}

	private void TriggerWatcherEvent(DiskScannerEngine engine, FileSystemWatcher watcher, string name)
	{
		var handler = typeof(DiskScannerEngine).GetMethod(
			"OnRadarTriggered",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(handler);
		handler.Invoke(engine, [watcher, new FileSystemEventArgs(WatcherChangeTypes.Changed, _tempRoot, name)]);
	}

	public void Dispose()
	{
		foreach (var harness in _harnesses)
		{
			(harness.Engine as IDisposable)?.Dispose();
		}

		try
		{
			Directory.Delete(_tempRoot, recursive: true);
		}
		catch (DirectoryNotFoundException) { }
	}

	private sealed class WatcherHarness(DiskScannerEngine engine, Mock<IScanCacheService> cache)
	{
		public DiskScannerEngine Engine { get; } = engine;
		public Mock<IScanCacheService> Cache { get; } = cache;
		public ConcurrentQueue<(long Timestamp, string Path)> Broadcasts { get; } = new();
		public TaskCompletionSource<string> FirstSectorChanged { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}

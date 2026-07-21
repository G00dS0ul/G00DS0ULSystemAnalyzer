using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GSSystemAnalyzer.Tests.Services
{
	public class FileExtensionBreakdownTest : IDisposable
	{
		private readonly MemoryCache _cache;
		private readonly DiskScannerEngine _engine;
		private readonly FileTypeScanner _scanner;
		private readonly string _root;

		public FileExtensionBreakdownTest()
		{
			var hub = new Mock<IHubContext<SystemHub>>().Object;
			var settings = new Mock<ISettingService>().Object;

			_cache = new MemoryCache(new MemoryCacheOptions());
			_engine = new DiskScannerEngine(hub, settings, NullLogger<DiskScannerEngine>.Instance);

			_engine.DirectorySizeCache.Clear();

			_scanner = new FileTypeScanner(_engine, _cache);

			_root = Path.Combine(Path.GetTempPath(), "gsa_ebtest_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_root);
		}

		private void SeedScan(Dictionary<string, FileTypeEntry> extensions)
		{
			_engine.DirectorySizeCache[Path.GetFullPath(_root)] = new CacheEntry
			{
				Size = extensions.Sum(e => e.Value.Bytes),
				LastUpdated = DateTime.UtcNow,
				Extensions = extensions
			};
		}

		[Fact]
		public void GetExtensionBreakdown_WhenScanCached_ReturnsAggregatedBreakdownSortedBySize()
		{
			SeedScan(new Dictionary<string, FileTypeEntry>(StringComparer.OrdinalIgnoreCase)
			{
				[".mp4"] = new FileTypeEntry
				{
					Count = 2,
					Bytes = 8000,
					LargestFileBytes = 6000,
					LargestFilePath = @"C:\media\movie.mp4"
				},
				[".txt"] = new FileTypeEntry
				{
					Count = 3,
					Bytes = 1500,
					LargestFileBytes = 700,
					LargestFilePath = @"C:\docs\notes.txt"
				},
				["no extension"] = new FileTypeEntry
				{
					Count = 1,
					Bytes = 500,
					LargestFileBytes = 500,
					LargestFilePath = @"C:\misc\LICENSE"
				},
			});

			var result = _scanner.GetExtensionBreakdown(_root);

			Assert.NotNull(result);
			Assert.Equal(3, result!.Extensions.Count);

			// Sorted by TotalBytes desc: .mp4 (8000) > .txt (1500) > (none) (500)
			var mp4 = result.Extensions[0];
			var txt = result.Extensions[1];
			var none = result.Extensions[2];

			Assert.Equal(".mp4", mp4.Ext);
			Assert.Equal("media", mp4.Category);
			Assert.Equal(2, mp4.FileCount);
			Assert.Equal(8000, mp4.TotalBytes);
			Assert.Equal(4000, mp4.AverageFileSizeBytes);
			Assert.Equal(80.0, mp4.PercentOfDisk);
			Assert.Equal(6000, mp4.LargestFileBytes);
			Assert.Equal(@"C:\media\movie.mp4", mp4.LargestFilePath);
			Assert.False(string.IsNullOrEmpty(mp4.SizeFormatted));

			Assert.Equal(".txt", txt.Ext);
			Assert.Equal("documents", txt.Category);
			Assert.Equal(15.0, txt.PercentOfDisk);
			Assert.Equal(500, txt.AverageFileSizeBytes);

			// "no extension" is surfaced as "(none)" and falls back to category "other".
			Assert.Equal("(none)", none.Ext);
			Assert.Equal("other", none.Category);
			Assert.Equal(5.0, none.PercentOfDisk);
		}

		[Fact]
		public void GetExtensionBreakdown_WhenNoScanCached_ReturnsNull()
		{
			// Nothing seeded -> scanner reports "not scanned" -> controller maps this to 409 NO_SCAN_CACHED.
			var result = _scanner.GetExtensionBreakdown(_root);

			Assert.Null(result);
		}

		[Fact]
		public void GetExtensionBreakdown_OnSecondCall_ReturnsCachedInstance()
		{
			SeedScan(new Dictionary<string, FileTypeEntry>(StringComparer.OrdinalIgnoreCase)
			{
				[".cs"] = new FileTypeEntry { Count = 1, Bytes = 1024, LargestFileBytes = 1024, LargestFilePath = @"C:\src\Program.cs" },
			});

			var first = _scanner.GetExtensionBreakdown(_root);
			var second = _scanner.GetExtensionBreakdown(_root);

			Assert.NotNull(first);
			// Result is memoized for 15 minutes under extbreakdown:{root}; same reference returned.
			Assert.Same(first, second);
		}

		public void Dispose()
		{
			_cache.Dispose();
			try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
			catch { /* best-effort cleanup */ }
		}
	}
}

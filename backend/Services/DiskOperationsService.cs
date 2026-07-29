using System.Drawing;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services
{
	public class DiskOperationsService : IDiskOperationService
	{
		private readonly DiskScannerEngine _scanner;
		private readonly IHubContext<SystemHub> _hubContext;
		private readonly ILogger<DiskOperationsService> _logger;

		public DiskOperationsService(DiskScannerEngine scanner, IHubContext<SystemHub> hubContext, ILogger<DiskOperationsService> logger)
		{
			_scanner = scanner;
			_hubContext = hubContext;
			_logger = logger;
		}

		public DriveTelemetryDto GetDriveTelemetry(string driveLetter)
		{
			var drive = new DriveInfo(driveLetter);

			var total = drive.TotalSize;
			var free = drive.TotalFreeSpace;
			var used = total - free;

			return new DriveTelemetryDto
			{
				TotalBytes = total,
				FreeBytes = free,
				UsedBytes = used,
				PercentageFree = Math.Round((double)free / total * 100, 1)
			};
		}

		public IEnumerable<StorageNode> ScanDirectory(string path, Guid scanId)
		{
			try
			{
				var items = _scanner.LoadDirectoryItems(path);
				PurgeDeadMemory(path, items);
				_scanner.CalculateMissingSizesAsync(items, scanId).GetAwaiter().GetResult();

				var nodes = items.Select(item =>
				{
					DateTime safeDate;
					try
					{
						safeDate = item.LastWriteTime;
					}
					catch
					{
						safeDate = DateTime.UtcNow;
					}

					long itemSize = 0;
					if (item is FileInfo f) itemSize = f.Length;
					else if (item is DirectoryInfo d &&
							 _scanner.DirectorySizeCache.TryGetValue(d.FullName, out var cachedSize))
					{
						itemSize = cachedSize.Size;
					}


					return new StorageNode
					{
						Name = item.Name,
						Path = item.FullName,
						Type = item.Attributes.HasFlag(FileAttributes.Directory) ? "Directory" : "File",
						SizeBytes = itemSize,
						LastModified = safeDate
					};
				}).ToList();

				var actualFolderSize = nodes.Sum(n => n.SizeBytes);

				var normalizedPath = Path.GetFullPath(path);
				_scanner.DirectorySizeCache.TryGetValue(normalizedPath, out var oldEntry);
				_scanner.DirectorySizeCache[normalizedPath] = new CacheEntry
				{
					Size = actualFolderSize,
					LastUpdated = DateTime.UtcNow,
					Extensions = oldEntry?.Extensions
				};

				Task.Run(() => _scanner.SaveMemoryToDisk());

				return nodes;
			}
			finally
			{
				_scanner.EndScanSession(scanId);
			}
		}

		private void PurgeDeadMemory(string currentPath, List<FileSystemInfo> actualItems)
		{
			var memoryChanged = false;

			var actualPaths =
				new HashSet<string>(actualItems.Select(i => i.FullName), StringComparer.OrdinalIgnoreCase);

			var pathWithSlash = currentPath.EndsWith(Path.DirectorySeparatorChar.ToString())
				? currentPath
				: currentPath + Path.DirectorySeparatorChar;

			var keysToCheck = _scanner.DirectorySizeCache.Keys
				.Where(k => k.StartsWith(pathWithSlash, StringComparison.OrdinalIgnoreCase))
				.Where(k => k.Length >= pathWithSlash.Length && k.IndexOf(Path.DirectorySeparatorChar, pathWithSlash.Length) == -1)
				.ToList();

			foreach (var key in keysToCheck)
			{
				if (!actualPaths.Contains(key))
				{
					_scanner.DirectorySizeCache.TryRemove(key, out _);
					memoryChanged = true;
					_logger.LogDebug("Cache purged: removed ghost folder {Key}", key);
				}
			}

			if (memoryChanged)
			{
				Task.Run(() => _scanner.SaveMemoryToDisk());
			}
		}

		public Guid BeginScan(Guid? scanId = null) => _scanner.BeginScanSession(scanId);

		public void TriggerScanAbort(Guid? scanId = null)
		{
			_scanner.TriggerScanAbort(scanId);
		}
	}
}

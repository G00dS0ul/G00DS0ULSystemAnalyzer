using System;
using System.Collections.Generic;
using System.IO;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Interfaces;
using System.Linq.Expressions;

namespace GSSystemAnalyzer.Services;

// public interface IDriveDetectionService
// {
//     List<DriveMetric> GetReadyDrives();
// }

public class DriveDetectionService : IDriveDetectionService
{
	public List<DriveMetric> GetReadyDrives()
	{
		var result = new List<DriveMetric>();
		var drives = DriveInfo.GetDrives();

		foreach (var drive in drives)
		{
			if (!drive.IsReady) continue;

			try
			{
				long total = drive.TotalSize;
				long free = drive.AvailableFreeSpace;
				long used = total - free;

				double percent = total > 0 ? Math.Round((double)used / total * 100, 1) : 0;

				result.Add(new DriveMetric
				{
					Name = drive.Name,
					Label = drive.VolumeLabel,
					Type = drive.DriveType.ToString(),
					TotalBytes = total,
					FreeBytes = free,
					UsedBytes = used,
					UsedPercent = percent,
					Format = drive.DriveFormat,
					IsReady = drive.IsReady
				});
			}
			catch (IOException) { /* To skip unreadable drives */ }
			catch (UnauthorizedAccessException) { /* To skip locked drives */ }
		}

		return result;
	}
}

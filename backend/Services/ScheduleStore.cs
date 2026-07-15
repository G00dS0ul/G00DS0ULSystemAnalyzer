using System.Text.Json;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class ScheduleStore : IScheduleStore
{
	private readonly string _filePath;
	private readonly object _fileLock = new();
	private readonly ILogger<ScheduleStore> _logger;

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public ScheduleStore(ILogger<ScheduleStore> logger, string? testFilePath = null)
	{
		_logger = logger;

		if (testFilePath != null)
		{
			_filePath = testFilePath;
		}
		else
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var appFolder = Path.Combine(appData, "GSAnalyzer");
			Directory.CreateDirectory(appFolder);
			_filePath = Path.Combine(appFolder, "scheduled_scans.json");
		}
	}

	public List<ScheduledScan> LoadAll()
	{
		lock (_fileLock)
		{
			if (!File.Exists(_filePath))
				return new List<ScheduledScan>();

			try
			{
				var json = File.ReadAllText(_filePath);
				var schedules = JsonSerializer.Deserialize<List<ScheduledScan>>(json, _jsonOptions);
				return schedules ?? new List<ScheduledScan>();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Corrupt schedule file detected, returning empty list");
				return new List<ScheduledScan>();
			}
		}
	}

	public void SaveAll(List<ScheduledScan> schedules)
	{
		lock (_fileLock)
		{
			try
			{
				var dir = Path.GetDirectoryName(_filePath)!;
				Directory.CreateDirectory(dir);

				var json = JsonSerializer.Serialize(schedules, _jsonOptions);
				var tmpPath = _filePath + ".tmp";
				File.WriteAllText(tmpPath, json);

				// Atomic swap — readers see either the old file or the new one, never a stump.
				File.Move(tmpPath, _filePath, overwrite: true);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to save schedules to disk");
			}
		}
	}
}

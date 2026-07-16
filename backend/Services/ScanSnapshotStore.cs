using System.Text.Json;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class ScanSnapshotStore : IScanSnapshotStore
{
	private readonly string _basePath;
	private readonly object _fileLock = new();
	private readonly ILogger<ScanSnapshotStore> _logger;

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public ScanSnapshotStore(ILogger<ScanSnapshotStore> logger, string? testBasePath = null)
	{
		_logger = logger;

		if (testBasePath != null)
		{
			_basePath = testBasePath;
		}
		else
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			_basePath = Path.Combine(appData, "GSAnalyzer", "scan_snapshots");
		}

		Directory.CreateDirectory(_basePath);
	}

	public Dictionary<string, ScanSnapshotEntry>? LoadBaseline(string root, int depth)
	{
		var filePath = GetFilePath(root, depth);

		lock (_fileLock)
		{
			if (!File.Exists(filePath))
				return null;

			try
			{
				var json = File.ReadAllText(filePath);
				var snapshot = JsonSerializer.Deserialize<Dictionary<string, ScanSnapshotEntry>>(json, _jsonOptions);
				return snapshot;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Corrupt snapshot file detected for {Root} depth {Depth}, returning null", root, depth);
				return null;
			}
		}
	}

	public void SaveBaseline(string root, int depth, Dictionary<string, ScanSnapshotEntry> snapshot)
	{
		var filePath = GetFilePath(root, depth);

		lock (_fileLock)
		{
			try
			{
				Directory.CreateDirectory(_basePath);

				var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
				var tmpPath = filePath + ".tmp";
				File.WriteAllText(tmpPath, json);

				// Atomic swap — readers see either the old file or the new one, never a stump.
				File.Move(tmpPath, filePath, overwrite: true);

				_logger.LogDebug("Snapshot baseline saved for {Root} depth {Depth} ({Count} entries)", root, depth, snapshot.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to save snapshot baseline for {Root} depth {Depth}", root, depth);
			}
		}
	}

	public DateTimeOffset? GetBaselineTimestamp(string root, int depth)
	{
		var filePath = GetFilePath(root, depth);

		lock (_fileLock)
		{
			if (!File.Exists(filePath))
				return null;

			try
			{
				return new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero);
			}
			catch
			{
				return null;
			}
		}
	}

	public bool DeleteBaseline(string root, int depth)
	{
		var filePath = GetFilePath(root, depth);

		lock (_fileLock)
		{
			if (!File.Exists(filePath))
				return false;

			try
			{
				File.Delete(filePath);
				_logger.LogInformation("Snapshot baseline deleted for {Root} depth {Depth}", root, depth);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete snapshot baseline for {Root} depth {Depth}", root, depth);
				return false;
			}
		}
	}

	private string GetFilePath(string root, int depth)
	{
		var driveKey = SanitizeRootToKey(root);
		return Path.Combine(_basePath, $"{driveKey}_{depth}.json");
	}

	/// <summary>
	/// Converts a root path into a safe filename key.
	/// E.g. "C:/" → "C_", "/home/user" → "_home_user"
	/// </summary>
	internal static string SanitizeRootToKey(string root)
	{
		var key = root.Replace("\\", "/").TrimEnd('/');
		key = key.Replace(":", "").Replace("/", "_");

		if (string.IsNullOrEmpty(key))
			key = "_root";

		return key;
	}
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Hubs;

namespace GSSystemAnalyzer.BackgroundWorkers;

public class DriveMonitorService : BackgroundService
{
	private readonly IDriveDetectionService _driveService;
	private readonly IHubContext<SystemHub> _hubContext;
	private readonly ILogger<DriveMonitorService> _logger;

	private string _lastHardwareSignature = string.Empty;
	private int _secondsSincelastSpaceCheck = 0;

	private const double AlertThresholdPercent = 90.0;
	private readonly HashSet<string> _alertedDrives = new();
	private readonly Dictionary<string, DateTime> _lastAlertUtc = new();
	private static readonly TimeSpan AlertReArmInterval = TimeSpan.FromMinutes(30);

	public DriveMonitorService(
		IDriveDetectionService driveService,
		IHubContext<SystemHub> hubContext,
		ILogger<DriveMonitorService> logger)
	{
		_driveService = driveService;
		_hubContext = hubContext;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Drive Monitor Background Service is starting.");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var drives = _driveService.GetReadyDrives();

				// Checking for hardware changes (every 5 seconds)
				var currentSignature = string.Join("|", drives.Select(d => $"{d.Name}-{d.Label}-{d.TotalBytes}"));

				if (currentSignature != _lastHardwareSignature)
				{
					_logger.LogInformation("Hardware change detected. Broadcasting DriveListUpdate.");

					await _hubContext.Clients.All.SendAsync("DriveListUpdate", new { drives = drives }, stoppingToken);

					_lastHardwareSignature = currentSignature;
				}

				// Checking for space thresholds (every 60 seconds)
				if (_secondsSincelastSpaceCheck >= 60)
				{
					foreach (var drive in drives)
					{
						var isOverThreshold = drive.UsedPercent >= AlertThresholdPercent;
						var alreadyAlerted = _alertedDrives.Contains(drive.Name);

						if (isOverThreshold)
						{
							var dueForReminder =
								_lastAlertUtc.TryGetValue(drive.Name, out var last) &&
								(DateTime.UtcNow - last) >= AlertReArmInterval;

							// Fire only on the first crossing, or once the re-arm window has elapsed.
							if (!alreadyAlerted || dueForReminder)
							{
								_logger.LogWarning(
									"Disk Alert: {DriveName} is critically full ({UsedPercent}%).",
									drive.Name, drive.UsedPercent);

								await _hubContext.Clients.All.SendAsync("DiskAlert", new
								{
									driveName = drive.Name,
									label = drive.Label,
									usedPercent = drive.UsedPercent,
									freeFormatted = FormatSize(drive.FreeBytes)
								}, stoppingToken);

								_alertedDrives.Add(drive.Name);
								_lastAlertUtc[drive.Name] = DateTime.UtcNow;
							}
						}
						else if (alreadyAlerted)
						{
							// Drive recovered below the threshold — clear state and let the HUD dismiss it.
							_alertedDrives.Remove(drive.Name);
							_lastAlertUtc.Remove(drive.Name);

							await _hubContext.Clients.All.SendAsync("DiskAlertCleared", new
							{
								driveName = drive.Name,
								label = drive.Label,
								usedPercent = drive.UsedPercent
							}, stoppingToken);
						}
					}

					// Prune state for drives that have been removed/unmounted.
					var presentNames = drives.Select(d => d.Name).ToHashSet();
					_alertedDrives.RemoveWhere(name => !presentNames.Contains(name));
					foreach (var stale in _lastAlertUtc.Keys.Where(k => !presentNames.Contains(k)).ToList())
					{
						_lastAlertUtc.Remove(stale);
					}

					_secondsSincelastSpaceCheck = 0;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred in the Drive Monitor loop.");
			}

			await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			_secondsSincelastSpaceCheck += 5;
		}
	}

	private string FormatSize(long bytes)
	{
		string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
		int counter = 0;
		decimal number = bytes;
		// Stop at the largest known suffix so we can never index past the array.
		while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
		{
			number /= 1024;
			counter++;
		}
		return string.Format("{0:n1} {1}", number, suffixes[counter]);
	}
}

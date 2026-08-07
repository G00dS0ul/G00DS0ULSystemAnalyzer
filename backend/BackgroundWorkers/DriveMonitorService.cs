using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using GSSystemAnalyzer.Services;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Hubs;

namespace GSSystemAnalyzer.BackgroundWorkers;

public record DriveAlertState(
	bool IsAlerting,
	DateTimeOffset? LastAlertedAt,
	double LastPercent,
	DateTimeOffset? FirstDetectedAt = null);

public class DriveMonitorService : BackgroundService
{
	private const double DefaultThresholdPercent = 90.0;
	private const double RemovableDriveThresholdPercent = 95.0;
	private const double CriticalSeverityThresholdPercent = 98.0;
	private const double HysteresisBand = 5.0;
	private const double ReAlertPercentIncrease = 5.0;

	private readonly IDriveDetectionService _driveService;
	private readonly IHubContext<SystemHub> _hubContext;
	private readonly ISettingService _settings;
	private readonly ILogger<DriveMonitorService> _logger;
	private readonly Func<DateTimeOffset> _timeProvider;
	private readonly TimeSpan _cooldownInterval;

	private readonly Dictionary<string, DriveAlertState> _driveStates = new(StringComparer.OrdinalIgnoreCase);
	private readonly object _stateLock = new();
	private readonly SemaphoreSlim _wakeSignal = new(0, 1);

	private string _lastHardwareSignature = string.Empty;
	private int _secondsSinceLastSpaceCheck = 60;

	public IReadOnlyDictionary<string, DriveAlertState> DriveStates
	{
		get
		{
			lock (_stateLock)
			{
				return new Dictionary<string, DriveAlertState>(_driveStates, StringComparer.OrdinalIgnoreCase);
			}
		}
	}

	public DriveMonitorService(
		IDriveDetectionService driveService,
		IHubContext<SystemHub> hubContext,
		ISettingService settings,
		ILogger<DriveMonitorService> logger)
		: this(driveService, hubContext, settings, logger, () => DateTimeOffset.UtcNow, TimeSpan.FromMinutes(60))
	{
	}

	public DriveMonitorService(
		IDriveDetectionService driveService,
		IHubContext<SystemHub> hubContext,
		ISettingService settings,
		ILogger<DriveMonitorService> logger,
		Func<DateTimeOffset>? timeProvider,
		TimeSpan? cooldownInterval = null)
	{
		_driveService = driveService;
		_hubContext = hubContext;
		_settings = settings;
		_logger = logger;
		_timeProvider = timeProvider ?? (() => DateTimeOffset.UtcNow);
		_cooldownInterval = cooldownInterval ?? TimeSpan.FromMinutes(60);

		_settings.OnSettingsChanged += HandleSettingsChanged;
	}

	private void HandleSettingsChanged(object? sender, AppSettingDto newSettings)
	{
		_logger.LogInformation("Settings changed: triggering immediate drive alert re-evaluation.");
		_secondsSinceLastSpaceCheck = 60;
		if (_wakeSignal.CurrentCount == 0)
		{
			try
			{
				_wakeSignal.Release();
			}
			catch (SemaphoreFullException)
			{
				// Ignore if already signaled
			}
		}
	}

	public async Task EvaluateDrivesAsync(CancellationToken cancellationToken = default)
	{
		var drives = _driveService.GetReadyDrives();
		var now = _timeProvider();

		var presentNames = drives.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

		// Prune removed drives
		lock (_stateLock)
		{
			var staleKeys = _driveStates.Keys.Where(k => !presentNames.Contains(k)).ToList();
			foreach (var stale in staleKeys)
			{
				_driveStates.Remove(stale);
			}
		}

		foreach (var drive in drives)
		{
			double threshold = GetThresholdForDrive(drive);
			double usedPercent = drive.UsedPercent;

			DriveAlertState state;
			lock (_stateLock)
			{
				if (!_driveStates.TryGetValue(drive.Name, out state!))
				{
					state = new DriveAlertState(false, null, usedPercent, null);
					_driveStates[drive.Name] = state;
				}
			}

			if (!state.IsAlerting)
			{
				// Normal -> Alerting transition
				if (usedPercent >= threshold)
				{
					var firstDetectedAt = state.FirstDetectedAt ?? now;
					var newState = new DriveAlertState(true, now, usedPercent, firstDetectedAt);
					lock (_stateLock)
					{
						_driveStates[drive.Name] = newState;
					}

					await SendDiskAlertAsync(drive, threshold, usedPercent, firstDetectedAt, cancellationToken);
				}
			}
			else
			{
				// Alerting -> Normal recovery (5-point hysteresis band)
				if (usedPercent <= (threshold - HysteresisBand))
				{
					var newState = new DriveAlertState(false, null, usedPercent, null);
					lock (_stateLock)
					{
						_driveStates[drive.Name] = newState;
					}

					await SendDiskAlertClearedAsync(drive, usedPercent, cancellationToken);
				}
				// While Alerting: Cooldown & Progression re-fire check
				else
				{
					bool cooldownElapsed = state.LastAlertedAt.HasValue && (now - state.LastAlertedAt.Value) >= _cooldownInterval;
					bool usageClimbed = usedPercent >= (state.LastPercent + ReAlertPercentIncrease);

					if (cooldownElapsed && usageClimbed)
					{
						var firstDetectedAt = state.FirstDetectedAt ?? now;
						var newState = new DriveAlertState(true, now, usedPercent, firstDetectedAt);
						lock (_stateLock)
						{
							_driveStates[drive.Name] = newState;
						}

						await SendDiskAlertAsync(drive, threshold, usedPercent, firstDetectedAt, cancellationToken);
					}
				}
			}
		}
	}

	private double GetThresholdForDrive(DriveMetric drive)
	{
		if (string.Equals(drive.Type, "Removable", StringComparison.OrdinalIgnoreCase))
		{
			return RemovableDriveThresholdPercent;
		}

		var configured = _settings.Current?.Alerts?.DiskThresholdPercent;
		if (configured.HasValue && configured.Value > 0)
		{
			return configured.Value;
		}

		return DefaultThresholdPercent;
	}

	private async Task SendDiskAlertAsync(
		DriveMetric drive,
		double threshold,
		double usedPercent,
		DateTimeOffset firstDetectedAt,
		CancellationToken cancellationToken)
	{
		string severity = usedPercent >= CriticalSeverityThresholdPercent ? "critical" : "warning";

		_logger.LogWarning(
			"Disk Alert [{Severity}]: {DriveName} ({Label}) is critically full ({UsedPercent}% >= {Threshold}%). Free: {FreeFormatted}",
			severity, drive.Name, drive.Label, usedPercent, threshold, FormatSize(drive.FreeBytes));

		var payload = new
		{
			driveName = drive.Name,
			label = drive.Label,
			driveType = drive.Type,
			usedPercent = usedPercent,
			freeBytes = drive.FreeBytes,
			freeFormatted = FormatSize(drive.FreeBytes),
			thresholdPercent = threshold,
			severity = severity,
			firstDetectedAt = firstDetectedAt.UtcDateTime.ToString("o")
		};

		await _hubContext.Clients.All.SendAsync("DiskAlert", payload, cancellationToken);
	}

	private async Task SendDiskAlertClearedAsync(
		DriveMetric drive,
		double usedPercent,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Disk Alert Cleared: {DriveName} ({Label}) recovered to {UsedPercent}%.",
			drive.Name, drive.Label, usedPercent);

		var payload = new
		{
			driveName = drive.Name,
			label = drive.Label,
			usedPercent = usedPercent
		};

		await _hubContext.Clients.All.SendAsync("DiskAlertCleared", payload, cancellationToken);
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

				// Checking for space thresholds (every 60 seconds or on immediate wake)
				if (_secondsSinceLastSpaceCheck >= 60)
				{
					await EvaluateDrivesAsync(stoppingToken);
					_secondsSinceLastSpaceCheck = 0;
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred in the Drive Monitor loop.");
			}

			try
			{
				await _wakeSignal.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}

			_secondsSinceLastSpaceCheck += 5;
		}
	}

	private static string FormatSize(long bytes)
	{
		string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
		int counter = 0;
		decimal number = bytes;
		while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
		{
			number /= 1024;
			counter++;
		}
		return string.Format("{0:n1} {1}", number, suffixes[counter]);
	}
}

using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.BackgroundWorkers;

public class ScheduledScanWorker : BackgroundService
{
	private readonly IScheduleService _scheduleService;
	private readonly ISettingService _settings;
	private readonly DiskScannerEngine _engine;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IHubContext<SystemHub> _hubContext;
	private readonly ILogger<ScheduledScanWorker> _logger;

	private bool _defaultScheduleSeeded;

	/// <summary>
	/// Interval between tick evaluations in milliseconds.
	/// Exposed as internal for test overriding.
	/// </summary>
	internal int TickIntervalMs { get; set; } = 30_000;

	public ScheduledScanWorker(
		IScheduleService scheduleService,
		ISettingService settings,
		DiskScannerEngine engine,
		IServiceScopeFactory scopeFactory,
		IHubContext<SystemHub> hubContext,
		ILogger<ScheduledScanWorker> logger)
	{
		_scheduleService = scheduleService;
		_settings = settings;
		_engine = engine;
		_scopeFactory = scopeFactory;
		_hubContext = hubContext;
		_logger = logger;

		// Hot-reload: settings changes are picked up on the next tick automatically
		// since we read _settings.Current on every iteration.
		_settings.OnSettingsChanged += OnSettingsChanged;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("ScheduledScanWorker is starting");

		// Allow the rest of the app to finish startup before we start ticking.
		await Task.Delay(2000, stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await TickAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unhandled error in ScheduledScanWorker tick loop");
			}

			await Task.Delay(TickIntervalMs, stoppingToken);
		}

		_logger.LogInformation("ScheduledScanWorker is stopping");
	}

	/// <summary>
	/// Single tick: evaluate all due schedules and execute them if the engine is free.
	/// Exposed as internal for unit testing.
	/// </summary>
	internal async Task TickAsync(CancellationToken stoppingToken)
	{
		var monitoring = _settings.Current.Monitoring;

		// Master switch — if disabled, do nothing.
		if (!monitoring.EnableScheduledScans)
		{
			_defaultScheduleSeeded = false; // Reset so toggling back on re-seeds
			return;
		}

		// Seed the default interval schedule for the system drive if not already done.
		EnsureDefaultSchedule(monitoring);

		var now = DateTimeOffset.UtcNow;
		var dueSchedules = _scheduleService.GetDueSchedules(now);

		if (dueSchedules.Count == 0)
			return;

		foreach (var schedule in dueSchedules)
		{
			if (stoppingToken.IsCancellationRequested)
				break;

			// Overlap guard — never run a scan while one is already in progress.
			if (_engine.IsScanning)
			{
				_logger.LogInformation(
					"Skipping scheduled scan {Id} for {Path} — a scan is already running",
					schedule.Id, schedule.Path);
				continue;
			}

			await ExecuteScheduledScanAsync(schedule, stoppingToken);
		}
	}

	private async Task ExecuteScheduledScanAsync(ScheduledScan schedule, CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"Starting scheduled scan {Id}: {Type} for {Path}",
			schedule.Id, schedule.Type, schedule.Path);

		try
		{
			using var scope = _scopeFactory.CreateScope();
			var diskService = scope.ServiceProvider.GetRequiredService<IDiskOperationService>();

			var scanId = diskService.BeginScan();

			switch (schedule.Type)
			{
				case ScanType.Directory:
					diskService.ScanDirectory(schedule.Path, scanId);
					break;
				// LargeFiles and Duplicates can be added here when their services
				// are integrated into the scheduled pipeline.
				default:
					_logger.LogWarning("ScanType {Type} is not yet supported for scheduled scans", schedule.Type);
					return;
			}

			var completedAt = DateTimeOffset.UtcNow;
			_scheduleService.MarkCompleted(schedule.Id, completedAt);

			// Broadcast AutoScanComplete — diff field is null until the Scan Result Diff
			// feature is implemented, which will call IScanDiffService.ComputeDiff() here.
			await _hubContext.Clients.All.SendAsync("AutoScanComplete", new
			{
				scheduleId = schedule.Id,
				root = schedule.Path,
				scannedAt = completedAt,
				// TODO: Hook into IScanDiffService to attach flat-map JSON diff here once merged.
				diff = (object?)null
			}, stoppingToken);

			_logger.LogInformation(
				"Scheduled scan {Id} completed for {Path} at {CompletedAt}",
				schedule.Id, schedule.Path, completedAt);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
			_logger.LogInformation("Scheduled scan {Id} cancelled due to shutdown", schedule.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Scheduled scan {Id} failed for {Path}", schedule.Id, schedule.Path);

			// Still mark as completed to avoid infinite retry loops.
			// The next scheduled run will try again.
			_scheduleService.MarkCompleted(schedule.Id, DateTimeOffset.UtcNow);
		}
	}

	private void EnsureDefaultSchedule(MonitoringSettingDto monitoring)
	{
		if (_defaultScheduleSeeded) return;

		var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
		var normalizedSystemDrive = systemDrive.Replace("\\", "/");

		// Check if an interval schedule already exists for the system drive
		var existingSchedules = _scheduleService.GetAll();
		var hasDefault = existingSchedules.Any(s =>
			s.Kind == ScheduleKind.Interval &&
			s.Path.Equals(normalizedSystemDrive, StringComparison.OrdinalIgnoreCase));

		if (!hasDefault)
		{
			_logger.LogInformation(
				"Seeding default interval schedule for system drive {Drive} at {Interval}min",
				normalizedSystemDrive, monitoring.ScheduledScanIntervalMinutes);

			_scheduleService.Create(new CreateScheduleRequest
			{
				Type = ScanType.Directory,
				Path = normalizedSystemDrive,
				Kind = ScheduleKind.Interval,
				IntervalMinutes = monitoring.ScheduledScanIntervalMinutes,
				Enabled = true
			});
		}

		_defaultScheduleSeeded = true;
	}

	private void OnSettingsChanged(object? sender, AppSettingDto settings)
	{
		// Hot-reload is automatic — the next tick reads _settings.Current.
		// We just log the change for observability.
		_logger.LogInformation(
			"Settings changed — scheduled scans {State}, interval = {Interval}min",
			settings.Monitoring.EnableScheduledScans ? "enabled" : "disabled",
			settings.Monitoring.ScheduledScanIntervalMinutes);
	}

	public override void Dispose()
	{
		_settings.OnSettingsChanged -= OnSettingsChanged;
		base.Dispose();
	}
}

using Cronos;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class ScheduleService : IScheduleService
{
	private readonly List<ScheduledScan> _schedules;
	private readonly IScheduleStore _store;
	private readonly IHubContext<SystemHub> _hub;
	private readonly ILogger<ScheduleService> _logger;
	private readonly object _lock = new();

	public ScheduleService(
		IScheduleStore store,
		IHubContext<SystemHub> hub,
		ILogger<ScheduleService> logger)
	{
		_store = store;
		_hub = hub;
		_logger = logger;
		_schedules = _store.LoadAll();
		_logger.LogInformation("Loaded {Count} persisted schedules", _schedules.Count);
	}

	public List<ScheduledScan> GetAll()
	{
		lock (_lock)
		{
			return _schedules.ToList();
		}
	}

	public ScheduledScan? GetById(Guid id)
	{
		lock (_lock)
		{
			return _schedules.FirstOrDefault(s => s.Id == id);
		}
	}

	public ScheduledScan Create(CreateScheduleRequest request)
	{
		ValidateRequest(request.Kind, request.Cron, request.IntervalMinutes);

		var schedule = new ScheduledScan
		{
			Id = Guid.NewGuid(),
			Type = request.Type,
			Path = NormalizePath(request.Path),
			Kind = request.Kind,
			Cron = request.Kind == ScheduleKind.Cron ? request.Cron : null,
			IntervalMinutes = request.Kind == ScheduleKind.Interval ? request.IntervalMinutes : null,
			Enabled = request.Enabled,
			LastRun = null,
			NextRun = ComputeNextRun(request.Kind, request.Cron, request.IntervalMinutes, lastRun: null)
		};

		lock (_lock)
		{
			_schedules.Add(schedule);
			Persist();
		}

		_logger.LogInformation(
			"Created schedule {Id}: {Kind} for {Path} (NextRun={NextRun})",
			schedule.Id, schedule.Kind, schedule.Path, schedule.NextRun);

		BroadcastUpdate();
		return schedule;
	}

	public ScheduledScan? Update(Guid id, UpdateScheduleRequest request)
	{
		lock (_lock)
		{
			var schedule = _schedules.FirstOrDefault(s => s.Id == id);
			if (schedule == null) return null;

			if (request.Type.HasValue)
				schedule.Type = request.Type.Value;

			if (request.Kind.HasValue)
				schedule.Kind = request.Kind.Value;

			if (request.Cron != null)
				schedule.Cron = request.Cron;

			if (request.IntervalMinutes.HasValue)
				schedule.IntervalMinutes = request.IntervalMinutes.Value;

			if (request.Enabled.HasValue)
				schedule.Enabled = request.Enabled.Value;

			// Validate the updated state
			ValidateRequest(schedule.Kind, schedule.Cron, schedule.IntervalMinutes);

			// Recompute next run based on updated settings
			schedule.NextRun = ComputeNextRun(schedule.Kind, schedule.Cron, schedule.IntervalMinutes, schedule.LastRun);

			Persist();

			_logger.LogInformation(
				"Updated schedule {Id}: Enabled={Enabled}, NextRun={NextRun}",
				schedule.Id, schedule.Enabled, schedule.NextRun);

			BroadcastUpdate();
			return schedule;
		}
	}

	public bool Delete(Guid id)
	{
		lock (_lock)
		{
			var removed = _schedules.RemoveAll(s => s.Id == id);
			if (removed == 0) return false;

			Persist();
		}

		_logger.LogInformation("Deleted schedule {Id}", id);
		BroadcastUpdate();
		return true;
	}

	public List<ScheduledScan> GetDueSchedules(DateTimeOffset now)
	{
		lock (_lock)
		{
			return _schedules
				.Where(s => s.Enabled && s.NextRun.HasValue && s.NextRun.Value <= now)
				.ToList();
		}
	}

	public void MarkCompleted(Guid id, DateTimeOffset completedAt)
	{
		lock (_lock)
		{
			var schedule = _schedules.FirstOrDefault(s => s.Id == id);
			if (schedule == null) return;

			schedule.LastRun = completedAt;
			schedule.NextRun = ComputeNextRun(schedule.Kind, schedule.Cron, schedule.IntervalMinutes, completedAt);

			Persist();

			_logger.LogInformation(
				"Schedule {Id} completed at {CompletedAt}, next run at {NextRun}",
				id, completedAt, schedule.NextRun);
		}

		BroadcastUpdate();
	}

	private DateTimeOffset ComputeNextRun(ScheduleKind kind, string? cron, int? intervalMinutes, DateTimeOffset? lastRun)
	{
		if (kind == ScheduleKind.Interval)
		{
			var minutes = intervalMinutes ?? 15;
			var from = lastRun ?? DateTimeOffset.UtcNow;
			return from.AddMinutes(minutes);
		}

		// Cron
		if (string.IsNullOrWhiteSpace(cron))
			throw new ArgumentException("Cron expression is required for Cron schedules.");

		var expression = CronExpression.Parse(cron);
		var from2 = lastRun?.UtcDateTime ?? DateTime.UtcNow;
		var next = expression.GetNextOccurrence(from2, inclusive: false);

		if (next == null)
			throw new ArgumentException($"Cron expression '{cron}' does not produce a future occurrence.");

		return new DateTimeOffset(next.Value, TimeSpan.Zero);
	}

	private static void ValidateRequest(ScheduleKind kind, string? cron, int? intervalMinutes)
	{
		if (kind == ScheduleKind.Interval)
		{
			if (!intervalMinutes.HasValue)
				throw new ArgumentException("IntervalMinutes is required for Interval schedules.");

			if (intervalMinutes.Value < 1 || intervalMinutes.Value > 1440)
				throw new ArgumentException("IntervalMinutes must be between 1 and 1440.");
		}
		else if (kind == ScheduleKind.Cron)
		{
			if (string.IsNullOrWhiteSpace(cron))
				throw new ArgumentException("Cron expression is required for Cron schedules.");

			try
			{
				CronExpression.Parse(cron);
			}
			catch (CronFormatException ex)
			{
				throw new ArgumentException($"Invalid cron expression: {ex.Message}", ex);
			}
		}
	}

	private static string NormalizePath(string path)
	{
		return path.Replace("\\", "/");
	}

	private void Persist()
	{
		// Called inside _lock, so the snapshot is consistent
		_store.SaveAll(_schedules.ToList());
	}

	private void BroadcastUpdate()
	{
		try
		{
			_ = _hub.Clients.All.SendAsync("ScheduleUpdate", new { schedules = GetAll() });
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to broadcast schedule update");
		}
	}
}

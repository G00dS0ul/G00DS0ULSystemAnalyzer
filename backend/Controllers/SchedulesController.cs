using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace GSSystemAnalyzer.Controllers;

[ApiController]
[Route("api/schedules")]
public class SchedulesController : ControllerBase
{
	private readonly IScheduleService _scheduleService;
	private readonly IDriveDetectionService _driveService;
	private readonly DiskScannerEngine _engine;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IHubContext<SystemHub> _hubContext;

	public SchedulesController(
		IScheduleService scheduleService,
		IDriveDetectionService driveService,
		DiskScannerEngine engine,
		IServiceScopeFactory scopeFactory,
		IHubContext<SystemHub> hubContext)
	{
		_scheduleService = scheduleService;
		_driveService = driveService;
		_engine = engine;
		_scopeFactory = scopeFactory;
		_hubContext = hubContext;
	}

	/// <summary>List all schedules (with lastRun + nextRun).</summary>
	[HttpGet]
	public IActionResult GetAll()
	{
		return Ok(_scheduleService.GetAll());
	}

	/// <summary>Create a new schedule.</summary>
	[HttpPost]
	public IActionResult Create([FromBody] CreateScheduleRequest request)
	{
		var pathError = ValidateDrivePath(request.Path);
		if (pathError != null) return pathError;

		try
		{
			var schedule = _scheduleService.Create(request);
			return CreatedAtAction(nameof(GetAll), new { id = schedule.Id }, schedule);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
		}
	}

	/// <summary>Update an existing schedule (enable/disable, change interval/cron/type).</summary>
	[HttpPut("{id:guid}")]
	public IActionResult Update(Guid id, [FromBody] UpdateScheduleRequest request)
	{
		try
		{
			var updated = _scheduleService.Update(id, request);
			if (updated == null)
				return NotFound(new { error = "NOT_FOUND", message = $"Schedule {id} not found." });

			return Ok(updated);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
		}
	}

	/// <summary>Remove a schedule.</summary>
	[HttpDelete("{id:guid}")]
	public IActionResult Delete(Guid id)
	{
		var deleted = _scheduleService.Delete(id);
		if (!deleted)
			return NotFound(new { error = "NOT_FOUND", message = $"Schedule {id} not found." });

		return Ok(new { message = "Schedule deleted." });
	}

	/// <summary>
	/// Trigger a scheduled scan immediately. Still honours the overlap guard —
	/// returns 409 if a scan is already running.
	/// </summary>
	[HttpPost("{id:guid}/run-now")]
	public IActionResult RunNow(Guid id)
	{
		var schedule = _scheduleService.GetById(id);
		if (schedule == null)
			return NotFound(new { error = "NOT_FOUND", message = $"Schedule {id} not found." });

		// Overlap guard
		if (_engine.IsScanning)
			return Conflict(new { error = "SCAN_IN_PROGRESS", message = "A scan is already running. Try again later." });

		// Fire-and-forget the scan on a background thread (same pattern as stream-sector).
		_ = Task.Run(async () =>
		{
			try
			{
				using var scope = _scopeFactory.CreateScope();
				var diskService = scope.ServiceProvider.GetRequiredService<IDiskOperationService>();

				var scanId = diskService.BeginScan();
				diskService.ScanDirectory(schedule.Path, scanId);

				var completedAt = DateTimeOffset.UtcNow;
				_scheduleService.MarkCompleted(schedule.Id, completedAt);

				await _hubContext.Clients.All.SendAsync("AutoScanComplete", new
				{
					scheduleId = schedule.Id,
					root = schedule.Path,
					scannedAt = completedAt,
					diff = (object?)null
				});
			}
			catch (Exception)
			{
				// Scan failures are logged by the engine; mark completed to advance NextRun.
				_scheduleService.MarkCompleted(schedule.Id, DateTimeOffset.UtcNow);
			}
		});

		return Ok(new { message = "Scan triggered.", scheduleId = schedule.Id });
	}

	/// <summary>
	/// Validates that the path corresponds to a mounted, ready drive.
	/// Returns a BadRequest result if not; null if the path is valid.
	/// </summary>
	private IActionResult? ValidateDrivePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return BadRequest(new { error = "PATH_REQUIRED", message = "path is required." });

		var normalized = Path.GetPathRoot(path)?.ToUpperInvariant() ?? path.ToUpperInvariant();
		if (!normalized.EndsWith(Path.DirectorySeparatorChar.ToString()))
			normalized = normalized.TrimEnd('\\', ':', '/') + Path.DirectorySeparatorChar;

		var readyDrives = _driveService.GetReadyDrives();
		if (!readyDrives.Any(d => d.Name.ToUpperInvariant() == normalized))
			return BadRequest(new { error = "DRIVE_NOT_READY", message = $"Drive not ready or not found: {path}" });

		// Protected roots check
		var pathUpper = path.Replace("\\", "/").ToUpperInvariant();
		if (pathUpper.StartsWith("C:/WINDOWS") || pathUpper.StartsWith("C:/PROGRAM FILES"))
			return BadRequest(new { error = "PROTECTED_ROOT", message = "Protected system directories cannot be scheduled for background scanning." });

		return null;
	}
}

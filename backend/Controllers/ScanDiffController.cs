using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers
{
	[ApiController]
	[Route("api/scan/diff")]
	public class ScanDiffController : ControllerBase
	{
		private readonly IScanDiffService _scanDiff;

		public ScanDiffController(IScanDiffService scanDiff)
		{
			_scanDiff = scanDiff;
		}

		[HttpGet]
		public IActionResult GetDiff(
			[FromQuery] string? root,
			[FromQuery] long minDeltaBytes,
			[FromServices] IDriveDetectionService driveService)
		{
			var target = ResolveRoot(root);

			var driveError = ValidateDriveReady(target, driveService);
			if (driveError != null) return driveError;

			var diff = _scanDiff.GetCachedDiff(target, minDeltaBytes);
			if (diff is null)
				return Conflict(new
				{
					error = "NO_SCAN_CACHED",
					message = "No scan result found for this root. Run a scan first."
				});

			return Ok(diff);
		}

		[HttpDelete("baseline")]
		public IActionResult ClearBaseline(
			[FromQuery] string? root,
			[FromServices] IDriveDetectionService driveService)
		{
			var target = ResolveRoot(root);

			var driveError = ValidateDriveReady(target, driveService);
			if (driveError != null) return driveError;

			_scanDiff.ClearBaseline(target);
			return Ok(new { success = true, message = "Baseline cleared." });
		}

		private static string ResolveRoot(string? requested) =>
			string.IsNullOrWhiteSpace(requested)
				? (Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\")
				: requested;

		private IActionResult? ValidateDriveReady(string root, IDriveDetectionService driveService)
		{
			var normalized = NormalizeDriveRoot(root);
			var readyDrives = driveService.GetReadyDrives();
			if (!readyDrives.Any(d => NormalizeDriveRoot(d.Name) == normalized))
				return BadRequest(new { error = "DRIVE_NOT_READY", message = "Drive not ready or not found" });

			return null;
		}

		private static string NormalizeDriveRoot(string path)
		{
			var root = Path.GetPathRoot(path) ?? path;
			return root.Replace('\\', '/').TrimEnd('/').ToUpperInvariant();
		}
	}
}

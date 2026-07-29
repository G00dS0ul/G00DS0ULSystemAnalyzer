using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers
{
	[ApiController]
	[Route("api/telemetry")]
	public class TelemetryHistoryController : ControllerBase
	{
		private readonly ITelemetryHistoryBuffer _historyBuffer;

		public TelemetryHistoryController(ITelemetryHistoryBuffer historyBuffer)
		{
			_historyBuffer = historyBuffer;
		}

		/// <summary>
		/// GET /api/telemetry/history?metric=cpu&amp;minutes=5
		/// Returns the rolling history for a given metric within the requested time window.
		/// </summary>
		[HttpGet("history")]
		public IActionResult GetHistory(
			[FromQuery] string metric = "",
			[FromQuery] int minutes = 5)
		{
			if (string.IsNullOrWhiteSpace(metric))
			{
				return BadRequest(new
				{
					message = "The 'metric' query parameter is required.",
					supportedMetrics = _historyBuffer.GetSupportedMetrics()
				});
			}

			// Normalise to lowercase for case-insensitive matching
			metric = metric.Trim().ToLowerInvariant();
			minutes = Math.Clamp(minutes, 1, 60);

			var result = _historyBuffer.GetHistory(metric, minutes);

			if (result == null)
			{
				return BadRequest(new
				{
					message = $"Unknown metric '{metric}'.",
					supportedMetrics = _historyBuffer.GetSupportedMetrics()
				});
			}

			return Ok(result);
		}

		/// <summary>
		/// GET /api/telemetry/history/metrics
		/// Returns the list of all supported metric keys.
		/// </summary>
		[HttpGet("history/metrics")]
		public IActionResult GetSupportedMetrics()
		{
			return Ok(new
			{
				metrics = _historyBuffer.GetSupportedMetrics()
			});
		}
	}
}

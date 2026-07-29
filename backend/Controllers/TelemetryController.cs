using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GSSystemAnalyzer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class TelemetryController : ControllerBase
	{
		private readonly ICpuMetricsProvider _cpuProvider;

		public TelemetryController(ICpuMetricsProvider cpuProvider)
		{
			_cpuProvider = cpuProvider;
		}

		[HttpPost("ram/start")]
		public IActionResult StartRamRadar([FromServices] RamMonitoringEngine ramEngine)
		{
			ramEngine.StartRadar();
			return Ok(new { Message = "RAM Radar Online" });
		}

		[HttpPost("ram/stop")]
		public IActionResult StopRamRadar([FromServices] RamMonitoringEngine ramEngine)
		{
			ramEngine.StopRadar();
			return Ok(new { Message = "RAM Radar Offline" });
		}

		[HttpPost("ram/kill")]
		public IActionResult KillProcess([FromServices] RamMonitoringEngine ramEngine, [FromBody] List<int> pids)
		{
			if (pids == null || !pids.Any()) return BadRequest(new { Message = "No PIDs provided." });

			var killCount = ramEngine.ExecuteOrder66(pids);
			return Ok(new { Message = $"{killCount} PIDs Terminated" });
		}

		[HttpGet("cpu-load")]
		public async Task<IActionResult> GetCurrentCpuLoad()
		{
			try
			{
				var telemetry = await _cpuProvider.GetNextSampleAsync();


				return Ok(new
				{
					success = true,
					data = telemetry,
					timestamp = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "Failed to fetch CPU telemetry snapshot",
					error = ex.Message
				});
			}
		}
	}
}

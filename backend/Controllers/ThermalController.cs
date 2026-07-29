using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ThermalController : ControllerBase
	{
		private readonly IThermalProvider _thermalProvider;

		public ThermalController(IThermalProvider thermalController)
		{
			_thermalProvider = thermalController;
		}

		[HttpGet("current")]
		public async Task<IActionResult> GetCurrentThermalData()
		{
			try
			{
				var payload = await _thermalProvider.GetThermalDataAsync();

				if (payload == null)
				{
					return NotFound(new
					{
						success = false,
						message = "Thermal sensor could not be read on this host"
					});
				}

				return Ok(new
				{
					success = true,
					message = "Thermal snapshot retrieved successfully",
					data = payload
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					success = false,
					message = $"Hardware bridge failure: {ex.Message}"
				});
			}
		}
	}
}

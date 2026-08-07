using System;
using System.Linq;
using System.Threading.Tasks;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class NetworkController : ControllerBase
	{
		private readonly INetworkEngine _networkEngine;
		private readonly ISettingService _settingsService;
		private readonly INetworkInterfaceProvider _interfaceProvider;

		public NetworkController(
			INetworkEngine networkEngine,
			ISettingService settingsService,
			INetworkInterfaceProvider interfaceProvider)
		{
			_networkEngine = networkEngine;
			_settingsService = settingsService;
			_interfaceProvider = interfaceProvider;
		}

		/// <summary>
		/// GET /api/network/interfaces
		/// Returns the current NetworkSnapshot with all monitorable adapters, rates, and session totals.
		/// </summary>
		[HttpGet("interfaces")]
		public IActionResult GetInterfaces()
		{
			var snapshot = _networkEngine.GetCurrentSnapshot();
			return Ok(snapshot);
		}

		/// <summary>
		/// POST /api/network/primary
		/// Sets or clears the preferred network interface.
		/// </summary>
		[HttpPost("primary")]
		public async Task<IActionResult> SetPrimaryInterface([FromBody] SetPreferredInterfaceRequest request)
		{
			var settings = _settingsService.Current;

			if (string.IsNullOrWhiteSpace(request?.InterfaceId))
			{
				settings.Monitoring.PreferredNetworkInterfaceId = null;
				await _settingsService.SaveAsync(settings);

				return Ok(new
				{
					success = true,
					message = "Primary interface preference cleared (auto-select active)",
					preferredInterfaceId = (string?)null
				});
			}

			// Validate that the requested interface ID exists on the machine
			var availableInterfaces = _interfaceProvider.GetInterfaces();
			var target = availableInterfaces.FirstOrDefault(i => string.Equals(i.Id, request.InterfaceId, StringComparison.OrdinalIgnoreCase));

			if (target == null)
			{
				return BadRequest(new
				{
					success = false,
					message = "Interface not found"
				});
			}

			settings.Monitoring.PreferredNetworkInterfaceId = target.Id;
			await _settingsService.SaveAsync(settings);

			return Ok(new
			{
				success = true,
				message = $"Primary interface preference set to {target.Name} ({target.Id})",
				preferredInterfaceId = target.Id
			});
		}
	}
}

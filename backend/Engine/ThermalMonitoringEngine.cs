using System.Runtime.InteropServices;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Engine
{
	public class ThermalMonitoringEngine : BackgroundService
	{
		private readonly IHubContext<SystemHub> _hubContext;
		private readonly IThermalProvider _thermalProvider;
		private readonly ITelemetryHistoryBuffer _historyBuffer;
		private readonly ILogger<ThermalMonitoringEngine> _logger;
		private TimeSpan _pollInterval;

		public ThermalMonitoringEngine(IHubContext<SystemHub> hubContext, IThermalProvider thermalProvider, ISettingService settings, ITelemetryHistoryBuffer historyBuffer, ILogger<ThermalMonitoringEngine> logger)
		{
			_hubContext = hubContext;
			_thermalProvider = thermalProvider;
			_historyBuffer = historyBuffer;
			_logger = logger;

			_pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.ThermalPollIntervalMs);

			settings.OnSettingsChanged += (_, s) => _pollInterval = TimeSpan.FromMilliseconds(s.Monitoring.ThermalPollIntervalMs);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Delay(5000, stoppingToken);

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					ThermalTelemetryDto payload = null;

					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						payload = await _thermalProvider.GetThermalDataAsync();
					}
					else
					{
						payload = new ThermalTelemetryDto();
					}

					if (payload != null)
					{
						await _hubContext.Clients.All.SendAsync("ReceiveThermalTelemetry", payload, stoppingToken);

						// Record to history buffer for historical charts
						if (payload.CpuPackageCelsius != null)
							_historyBuffer.Record("thermal_cpu_package", payload.CpuPackageCelsius.Value);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Thermal engine radar failure");
				}

				await Task.Delay(_pollInterval, stoppingToken);
			}
		}
	}
}


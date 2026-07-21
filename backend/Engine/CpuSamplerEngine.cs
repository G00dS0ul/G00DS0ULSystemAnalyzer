using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Engine
{
	public class CpuSamplerEngine : BackgroundService
	{
		private readonly ICpuMetricsProvider _cpuProvider;
		private readonly IHubContext<SystemHub> _hubContext;
		private readonly ITelemetryHistoryBuffer _historyBuffer;
		private readonly ILogger<CpuSamplerEngine> _logger;
		private TimeSpan _pollInterval;

		public CpuSamplerEngine(ICpuMetricsProvider cpuProvider, IHubContext<SystemHub> hubContext, ISettingService settings, ITelemetryHistoryBuffer historyBuffer, ILogger<CpuSamplerEngine> logger)
		{
			_cpuProvider = cpuProvider;
			_hubContext = hubContext;
			_historyBuffer = historyBuffer;
			_logger = logger;

			_pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.CpuPollIntervalMs);
			settings.OnSettingsChanged += (_, s) =>
			{
				_pollInterval = TimeSpan.FromMilliseconds(s.Monitoring.CpuPollIntervalMs);
				_logger.LogDebug("CPU sampler poll interval updated to {IntervalMs}ms", _pollInterval.TotalMilliseconds);
			};
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var telemetry = await _cpuProvider.GetNextSampleAsync();
					await _hubContext.Clients.All.SendAsync("ReceiveCpuTelemetry", telemetry, cancellationToken: stoppingToken);

					// Record to history buffer for historical charts
					_historyBuffer.Record("cpu", telemetry.AverageLoad);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "CPU sampler fault");
				}

				await Task.Delay(_pollInterval, stoppingToken);
			}
		}
	}
}

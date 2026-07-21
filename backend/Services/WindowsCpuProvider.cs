using GSSystemAnalyzer.Interfaces;
using System.Diagnostics;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Services
{
	public class WindowsCpuProvider : ICpuMetricsProvider
	{
#if WINDOWS
		private readonly List<PerformanceCounter> _coreCounters = new();
		private double _previousAverage = 0;
		private readonly PerformanceCounter? _baseFreqCounter;
		private readonly PerformanceCounter? _perfRatioCounter;
		private readonly PerformanceCounter? _systemProcesses;
		private readonly PerformanceCounter? _systemThreads;
		private readonly PerformanceCounter? _systemHandles;


		public WindowsCpuProvider()
		{
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var pc = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
				pc.NextValue();
				_coreCounters.Add(pc);
			}

			try
			{
				_baseFreqCounter = new PerformanceCounter("Processor Information", "Processor Frequency", "_Total");
				_perfRatioCounter =
					new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
				_baseFreqCounter.NextValue();
				_perfRatioCounter.NextValue();
			}
			catch
			{
				_baseFreqCounter = null;
				_perfRatioCounter = null;
			}

			_systemProcesses = new PerformanceCounter("System", "Processes");
			_systemThreads = new PerformanceCounter("System", "Threads");
			_systemHandles = new PerformanceCounter("Process", "Handle Count", "_Total");
		}
#else
        public WindowsCpuProvider() =>
            throw new PlatformNotSupportedException("WindowsCpuProvider is only supported on Windows.");
#endif

		public async Task<CpuTelemetryDto> GetNextSampleAsync()
		{
#if WINDOWS
			var loads = _coreCounters.Select(c => Math.Round(c.NextValue(), 1)).ToList();
			double currentAverage = Math.Round(loads.Average(), 1);
			double currentGhz = 0.0;
			try
			{
				if (_baseFreqCounter != null && _perfRatioCounter != null)
				{
					double baseMhz = _baseFreqCounter.NextValue();
					double perfRatio = _perfRatioCounter.NextValue();

					double currentMhz = baseMhz * (perfRatio / 100.0);
					currentGhz = Math.Round(currentMhz / 1000.0, 2);
				}
			}
			catch
			{
			}

			var dto = new CpuTelemetryDto
			{
				AverageLoad = currentAverage,
				Delta = Math.Round(currentAverage - _previousAverage, 1),
				CurrentFrequencyGhz = currentGhz,
				TotalProcesses = (int)_systemProcesses.NextValue(),
				TotalThreads = (int)_systemThreads.NextValue(),
				TotalHandles = (int)_systemHandles.NextValue(),

				L1Cache = "256 KB",
				L2Cache = "1.0 MB",
				L3Cache = "8.0 MB"
			}
			;
			for (int i = 0; i < loads.Count; i += 4)
			{
				int end = Math.Min(i + 3, loads.Count - 1);
				dto.CoreGroups.Add($"CORE {i}-{end}", loads.Skip(i).Take(4).ToList());
			}

			_previousAverage = currentAverage;
			return await Task.FromResult(dto);
#else

            return await Task.FromException<CpuTelemetryDto>(new PlatformNotSupportedException());
#endif
		}
	}
}

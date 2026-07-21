using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface ICpuMetricsProvider
	{
		Task<CpuTelemetryDto> GetNextSampleAsync();
	}
}

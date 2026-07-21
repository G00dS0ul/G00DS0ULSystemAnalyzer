using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface IThermalProvider : IDisposable
	{
		Task<ThermalTelemetryDto> GetThermalDataAsync();
	}
}

using GSSystemAnalyzer.Models;
using DellOemTelemetry = GSSystemAnalyzer.Services.Oem.Dell.DellOemTelemetry;

namespace GSSystemAnalyzer.Interfaces
{
	public interface IDellOemTelemetry
	{
		DellOemDto? TryGetDellOemTelemetry();
	}
}

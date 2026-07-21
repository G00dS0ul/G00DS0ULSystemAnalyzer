namespace GSSystemAnalyzer.Services;

public interface IWmiThermalFallback
{
	double? GetCpuTemperatureCelsius();
}

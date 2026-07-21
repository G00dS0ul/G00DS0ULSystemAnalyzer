namespace GSSystemAnalyzer.Models;

public sealed class DellOemDto
{
	public int? CpuFanRpm { get; set; }
	public int? GpuFanRpm { get; set; }
	public int? ChassisFanRpm { get; set; }

	public double? CpuTempCelsius { get; set; }
	public double? RamCelsius { get; set; }
	public double? AmbientCelsius { get; set; }
	public double? MotherboardCelsius { get; set; }
}

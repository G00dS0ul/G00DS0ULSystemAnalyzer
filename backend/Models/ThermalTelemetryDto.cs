namespace GSSystemAnalyzer.Models
{
	public class ThermalTelemetryDto
	{
		public double? CpuPackageCelsius { get; set; }
		public List<double>? CoreCelsius { get; set; }
		public double? MotherboardCelsius { get; set; }
		public double? ChipsetCelsius { get; set; }
		public double? NvmeCelsius { get; set; }
		public double? RamCelsius { get; set; }
		public double? AmbientCelsius { get; set; }
		public double? CpuPowerWatts { get; set; }
		public bool IsThermalThrottling { get; set; }
		public int? CpuFanRpm { get; set; }
		public int? ChassisFan1Rpm { get; set; }
		public int? ChassisFan2Rpm { get; set; }
		public int? PumpRpm { get; set; }

		//Advanced Tier (Null for now)
		public double? GpuCoreCelsius { get; set; }
		public double? GpuHotSpotCelsius { get; set; }
		public double? GpuVramCelsius { get; set; }
		public int? GpuFanRpm { get; set; }

		public ThermalTelemetryDto() => CoreCelsius = new List<double>();
	}
}

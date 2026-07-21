namespace GSSystemAnalyzer.Models.SettingDtos
{
	public class AlertSettingDto
	{
		public int DiskThresholdPercent { get; set; } = 90;
		public int RamThresholdPercent { get; set; } = 85;
		public int CpuThresholdPercent { get; set; } = 95;
		public int ThermalThresholdCelsius { get; set; } = 85;
		public bool EnableDeskopNotifications { get; set; } = true;
	}

}

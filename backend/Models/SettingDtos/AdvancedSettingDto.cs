namespace GSSystemAnalyzer.Models.SettingDtos
{
	public class AdvancedSettingDto
	{
		public int BackendPort { get; set; } = 5200;
		public int SignalrReconnectDelaysMs { get; set; } = 3000;
		public int MaxSignalrRetries { get; set; } = 10;
		public bool EnableDebugLogs { get; set; } = false;
	}
}

namespace GSSystemAnalyzer.Models.SettingDtos
{
	public class CacheSettingDto
	{
		public int ScanCacheTtlMinutes { get; set; } = 15;
		public int MaxCacheScans { get; set; } = 5;
	}
}

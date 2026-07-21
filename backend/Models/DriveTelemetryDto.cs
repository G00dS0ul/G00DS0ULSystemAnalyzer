namespace GSSystemAnalyzer.Models
{
	public class DriveTelemetryDto
	{
		public long TotalBytes { get; set; }
		public long FreeBytes { get; set; }
		public long UsedBytes { get; set; }
		public double PercentageFree { get; set; }
	}
}

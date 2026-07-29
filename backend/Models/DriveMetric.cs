namespace GSSystemAnalyzer.Models;

public class DriveMetric
{
	public string Name { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public long TotalBytes { get; set; }
	public long FreeBytes { get; set; }
	public long UsedBytes { get; set; }
	public double UsedPercent { get; set; }
	public string Format { get; set; } = string.Empty;
	public bool IsReady { get; set; }
}

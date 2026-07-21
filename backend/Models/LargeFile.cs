namespace GSSystemAnalyzer.Models;

public class LargeFile
{
	public string Path { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public string SizeFormatted { get; set; } = string.Empty;
}

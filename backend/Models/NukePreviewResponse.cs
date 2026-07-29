using System.Collections.Generic;

namespace GSSystemAnalyzer.Models;

public class NukePreviewResponse
{
	public int TotalFiles { get; set; }
	public long TotalBytes { get; set; }
	public string TotalFormatted { get; set; } = string.Empty;
	public string PlanToken { get; set; } = string.Empty;
	public List<NukePathBreakdown> Breakdown { get; set; } = new();
}

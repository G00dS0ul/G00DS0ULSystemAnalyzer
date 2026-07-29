using System.Collections.Generic;

namespace GSSystemAnalyzer.Models;

/// <summary>Single temp/cache location with its enumerated stats.</summary>
public class TempLocationPreview
{
	public string Path { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;
	public string Category { get; set; } = "Temp";
	public long SizeBytes { get; set; }
	public string SizeFormatted { get; set; } = string.Empty;
	public int FileCount { get; set; }
}

/// <summary>Aggregated preview across all detected temp locations.</summary>
public class TempPreviewResponse
{
	public long TotalBytes { get; set; }
	public string TotalFormatted { get; set; } = string.Empty;
	public List<TempLocationPreview> Locations { get; set; } = new();
}

/// <summary>Request body for the clean endpoint — user selects which paths to purge.</summary>
public class TempCleanRequest
{
	public List<string> Paths { get; set; } = new();
}

/// <summary>Post-clean summary.</summary>
public class TempCleanResult
{
	public int DeletedFiles { get; set; }
	public long FreedBytes { get; set; }
	public string FreedFormatted { get; set; } = string.Empty;
	public int SkippedFiles { get; set; }
}

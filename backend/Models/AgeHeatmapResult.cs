namespace GSSystemAnalyzer.Models;

public class AgeHeatmapNode
{
	public string Path { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public string AgeBucket { get; set; } = string.Empty;
	public DateTime LastModified { get; set; }
}

public class AgeBucketSummary
{
	public int Count { get; set; }
	public long TotalBytes { get; set; }
}

public class AgeHeatmapResult
{
	public string Root { get; set; } = string.Empty;
	public List<AgeHeatmapNode> Nodes { get; set; } = new();
	public Dictionary<string, AgeBucketSummary> Summary { get; set; } = new();
}

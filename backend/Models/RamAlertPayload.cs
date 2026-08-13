namespace GSSystemAnalyzer.Models;

/// <summary>
/// Payload for the "RamAlert" SignalR event.
/// Reports available physical memory (not commit charge) and the top consumers by working set.
/// </summary>
public class RamAlertPayload
{
	public double UsedPercent { get; set; }
	public long AvailablePhysicalBytes { get; set; }
	public string AvailableFormatted { get; set; } = string.Empty;
	public double ThresholdPercent { get; set; }
	public int MinimumFreeMb { get; set; }
	public string Severity { get; set; } = "warning";
	public int SustainedForSeconds { get; set; }
	public List<TopConsumerEntry> TopConsumers { get; set; } = new();
}

public class TopConsumerEntry
{
	public int Pid { get; set; }
	public string Name { get; set; } = string.Empty;
	public double RamMb { get; set; }
}

using System.Text.Json.Serialization;

namespace GSSystemAnalyzer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScanType { Directory, LargeFiles, Duplicates }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleKind { Interval, Cron }

public class ScheduledScan
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public ScanType Type { get; set; } = ScanType.Directory;
	public string Path { get; set; } = string.Empty;
	public ScheduleKind Kind { get; set; } = ScheduleKind.Interval;
	public string? Cron { get; set; }
	public int? IntervalMinutes { get; set; }
	public bool Enabled { get; set; } = true;
	public DateTimeOffset? LastRun { get; set; }
	public DateTimeOffset? NextRun { get; set; }
}

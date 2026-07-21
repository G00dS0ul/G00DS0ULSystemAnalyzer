namespace GSSystemAnalyzer.Models;

public class CreateScheduleRequest
{
	public ScanType Type { get; set; } = ScanType.Directory;
	public string Path { get; set; } = string.Empty;
	public ScheduleKind Kind { get; set; } = ScheduleKind.Interval;
	public string? Cron { get; set; }
	public int? IntervalMinutes { get; set; }
	public bool Enabled { get; set; } = true;
}

public class UpdateScheduleRequest
{
	public ScanType? Type { get; set; }
	public ScheduleKind? Kind { get; set; }
	public string? Cron { get; set; }
	public int? IntervalMinutes { get; set; }
	public bool? Enabled { get; set; }
}

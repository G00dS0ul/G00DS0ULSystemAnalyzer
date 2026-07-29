namespace GSSystemAnalyzer.Models
{
	public class ProcessTelemetry
	{
		public int ProcessId { get; set; }
		public string Name { get; set; }
		public long WorkingSetBytes { get; set; }
		public double RamMb => Math.Round(WorkingSetBytes / 1024.0 / 1024.0, 2);

		// Process Explorer fields
		public string User { get; set; } = "SYSTEM";
		public double CpuPercent { get; set; }
		public string Status { get; set; } = "RUNNING";
	}
}

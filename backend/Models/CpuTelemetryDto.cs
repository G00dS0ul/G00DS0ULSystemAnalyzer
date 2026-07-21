namespace GSSystemAnalyzer.Models
{
	public class CpuTelemetryDto
	{
		public double AverageLoad { get; set; }
		public double Delta { get; set; }
		public double CurrentFrequencyGhz { get; set; }

		public int TotalProcesses { get; set; }
		public int TotalThreads { get; set; }
		public int TotalHandles { get; set; }

		public string L1Cache { get; set; } = "N/A";
		public string L2Cache { get; set; } = "N/A";
		public string L3Cache { get; set; } = "N/A";
		public Dictionary<string, List<double>> CoreGroups { get; set; } = new();
	}
}

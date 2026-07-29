namespace GSSystemAnalyzer.Models
{
	public class TelemetryHistoryResponse
	{
		public string Metric { get; set; } = string.Empty;
		public int Minutes { get; set; }
		public string Unit { get; set; } = string.Empty;
		public List<TelemetryPoint> Points { get; set; } = new();
		public TelemetryStats Stats { get; set; } = new();
	}

	public class TelemetryStats
	{
		public double Min { get; set; }
		public double Max { get; set; }
		public double Avg { get; set; }
		public double Current { get; set; }
	}
}

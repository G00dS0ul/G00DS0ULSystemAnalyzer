using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface ITelemetryHistoryBuffer
	{
		/// <summary>
		/// Record a new data point for the specified metric.
		/// </summary>
		void Record(string metric, double value);

		/// <summary>
		/// Retrieve the history for a metric within the given time window.
		/// Returns null if the metric is not in the supported registry.
		/// </summary>
		TelemetryHistoryResponse? GetHistory(string metric, int minutes);

		/// <summary>
		/// Returns all metric keys registered in the buffer (both wired and TODO).
		/// </summary>
		IReadOnlyCollection<string> GetSupportedMetrics();
	}
}

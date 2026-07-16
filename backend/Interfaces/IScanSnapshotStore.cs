using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IScanSnapshotStore
{
	Dictionary<string, ScanSnapshotEntry>? LoadBaseline(string root, int depth);
	void SaveBaseline(string root, int depth, Dictionary<string, ScanSnapshotEntry> snapshot);
	DateTimeOffset? GetBaselineTimestamp(string root, int depth);
	bool DeleteBaseline(string root, int depth);
}

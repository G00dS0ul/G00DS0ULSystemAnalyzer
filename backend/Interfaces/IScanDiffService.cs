using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IScanDiffService
{
	ScanDiff ComputeDiff(string root, int depth, DateTimeOffset currentScannedAt);
	ScanDiff? GetCachedDiff(string root);
	bool DeleteBaseline(string root, int depth);
}

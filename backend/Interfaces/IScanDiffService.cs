using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

public interface IScanDiffService
{
	ScanDiff ComputeAndPromote(string root);

	ScanDiff? GetCachedDiff(string root, long minDeltaBytes = 0);

	void ClearBaseline(string root);
}

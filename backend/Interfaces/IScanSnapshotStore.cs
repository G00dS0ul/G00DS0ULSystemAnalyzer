using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces;

/// <summary>
/// Persists exactly one scan baseline per (root, depth) on disk, separate from the volatile
/// in-memory scan cache, so a diff survives cache eviction and app/backend restarts.
/// </summary>
public interface IScanSnapshotStore
{
	/// <summary>Loads the stored baseline for a (root, depth), or null if none exists / is unreadable.</summary>
	ScanSnapshot? Load(string root, int depth);

	/// <summary>Atomically writes the baseline for a (root, depth), overwriting any existing one.</summary>
	void Save(string root, int depth, ScanSnapshot snapshot);

	/// <summary>Deletes the stored baseline for a (root, depth). No-op if absent.</summary>
	void Delete(string root, int depth);
}

namespace GSSystemAnalyzer.Interfaces;

public interface IProcessOwnerResolver
{
	/// <summary>
	/// Resolves the owning username for a given process ID.
	/// Returns "SYSTEM" or "UNKNOWN" when the owner cannot be determined.
	/// </summary>
	string Resolve(int processId);

	/// <summary>
	/// Refreshes the internal owner cache. Call once per tick before resolving individual PIDs.
	/// </summary>
	void RefreshCache();
}

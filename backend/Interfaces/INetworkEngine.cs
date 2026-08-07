using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface INetworkEngine
	{
		NetworkSnapshot GetCurrentSnapshot();
	}
}

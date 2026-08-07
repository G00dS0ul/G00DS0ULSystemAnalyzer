using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace GSSystemAnalyzer.Interfaces
{
	public class NetworkInterfaceInfo
	{
		public string Id { get; init; } = string.Empty;
		public string Name { get; init; } = string.Empty;
		public string Description { get; init; } = string.Empty;
		public NetworkInterfaceType NetworkInterfaceType { get; init; }
		public OperationalStatus OperationalStatus { get; init; }
		public long Speed { get; init; } = -1;
		public long BytesReceived { get; init; }
		public long BytesSent { get; init; }
	}

	public interface INetworkInterfaceProvider
	{
		IReadOnlyList<NetworkInterfaceInfo> GetInterfaces();
	}
}

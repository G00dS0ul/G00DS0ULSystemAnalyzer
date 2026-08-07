using System;
using System.Collections.Generic;

namespace GSSystemAnalyzer.Models
{
	public record NetInterfaceSnapshot(
		string Id,
		string Name,
		string Description,
		string InterfaceType,
		bool IsUp,
		long LinkSpeedBitsPerSec,
		double RxBytesPerSec,
		double TxBytesPerSec,
		double? UtilisationPercent,
		long SessionRxBytes,
		long SessionTxBytes);

	public record NetworkSnapshot(
		DateTimeOffset Timestamp,
		string? PrimaryInterfaceId,
		IReadOnlyList<NetInterfaceSnapshot> Interfaces);

	public record SetPreferredInterfaceRequest(
		string? InterfaceId);
}

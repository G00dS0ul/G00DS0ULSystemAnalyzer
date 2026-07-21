using System.Collections.Generic;
using System.Threading;

namespace GSSystemAnalyzer.Interfaces
{
	public interface IDiskScannerEngine
	{
		bool IsScanning { get; }
		void ClearCache();
		CancellationToken NukeToken();
		void TriggerNukeAbort();
		void InvalidatePaths(IEnumerable<string> paths);
		Guid BeginScanSession(Guid? scanId = null);
		CancellationToken GetScanToken(Guid scanId);
		void EndScanSession(Guid scanId);
		void TriggerScanAbort(Guid? scanId = null);
	}
}

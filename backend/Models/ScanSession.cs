using System;
using System.Threading;

namespace GSSystemAnalyzer.Models
{
	public class ScanSession : IDisposable
	{
		public Guid ScanId { get; }
		public CancellationTokenSource Cts { get; }

		public ScanSession(Guid scanId)
		{
			ScanId = scanId;
			Cts = new CancellationTokenSource();
		}

		public void Dispose()
		{
			Cts?.Dispose();
		}
	}
}

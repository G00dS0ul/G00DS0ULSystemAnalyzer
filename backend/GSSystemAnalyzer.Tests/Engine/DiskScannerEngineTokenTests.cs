using System;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Engine
{
	public class DiskScannerEngineTokenTests
	{
		private readonly DiskScannerEngine _engine;

		public DiskScannerEngineTokenTests()
		{
			var hubMock = new Mock<IHubContext<SystemHub>>();
			var settingsMock = new Mock<ISettingService>();
			var loggerMock = new Mock<ILogger<DiskScannerEngine>>();

			_engine = new DiskScannerEngine(hubMock.Object, settingsMock.Object, loggerMock.Object);
		}

		[Fact]
		public void BeginScanSession_ReturnsLiveToken()
		{
			var id = _engine.BeginScanSession();
			var token = _engine.GetScanToken(id);
			Assert.False(token.IsCancellationRequested);
		}

		[Fact]
		public void TriggerScanAbort_SpecificId_CancelsOnlyThatToken()
		{
			var id1 = _engine.BeginScanSession();
			var id2 = _engine.BeginScanSession();

			var token1 = _engine.GetScanToken(id1);
			var token2 = _engine.GetScanToken(id2);

			_engine.TriggerScanAbort(id1);

			Assert.True(token1.IsCancellationRequested);
			Assert.False(token2.IsCancellationRequested);
		}

		[Fact]
		public void TriggerScanAbort_NullId_CancelsAllTokens()
		{
			var id1 = _engine.BeginScanSession();
			var id2 = _engine.BeginScanSession();

			var token1 = _engine.GetScanToken(id1);
			var token2 = _engine.GetScanToken(id2);

			_engine.TriggerScanAbort(null);

			Assert.True(token1.IsCancellationRequested);
			Assert.True(token2.IsCancellationRequested);
		}
	}
}

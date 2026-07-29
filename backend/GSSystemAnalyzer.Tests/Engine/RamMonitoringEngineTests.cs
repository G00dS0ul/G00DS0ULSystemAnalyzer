using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GSSystemAnalyzer.Tests.Engine;

public class RamMonitoringEngineTests
{
	private RamMonitoringEngine CreateEngine()
	{
		var mockHub = new Mock<IHubContext<SystemHub>>();
		var mockClients = new Mock<IHubClients>();
		var mockClient = new Mock<IClientProxy>();
		mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
		mockClients.Setup(c => c.All).Returns(mockClient.Object);

		var mockSettings = new Mock<ISettingService>();
		mockSettings.Setup(s => s.Current).Returns(new AppSettingDto
		{
			Monitoring = new MonitoringSettingDto { RamPollIntervalMs = 2000 }
		});

		var mockResolver = new Mock<IProcessOwnerResolver>();
		mockResolver.Setup(r => r.Resolve(It.IsAny<int>())).Returns("SYSTEM");

		var mockHistoryBuffer = new Mock<ITelemetryHistoryBuffer>();

		return new RamMonitoringEngine(mockHub.Object, mockSettings.Object, mockResolver.Object, mockHistoryBuffer.Object, NullLogger<RamMonitoringEngine>.Instance);
	}


	[Fact]
	public void ExecuteOrder66_EmptyList_ReturnsZero()
	{
		var engine = CreateEngine();
		var killed = engine.ExecuteOrder66(new List<int>());
		Assert.Equal(0, killed);
	}

	[Fact]
	public void ExecuteOrder66_AllInvalidPids_ReturnsZero()
	{
		var engine = CreateEngine();
		var killed = engine.ExecuteOrder66(new List<int> { -1, int.MaxValue, 99_999_999 });
		Assert.Equal(0, killed);
	}

	[Fact]
	public void ExecuteOrder66_DoesNotThrowOnAnyInput()
	{
		var engine = CreateEngine();
		var ex = Record.Exception(() =>
			engine.ExecuteOrder66(new List<int> { 0, -99, int.MaxValue }));
		Assert.Null(ex);
	}

	[Fact]
	public void ExecuteOrder66_NullList_ThrowsArgumentNullException()
	{
		var engine = CreateEngine();
		Assert.Throws<NullReferenceException>(() => engine.ExecuteOrder66(null!));
	}


	[Fact]
	public void StartRadar_CanBeCalledMultipleTimes_WithoutThrowing()
	{
		var engine = CreateEngine();
		var ex = Record.Exception(() =>
		{
			engine.StartRadar();
			engine.StartRadar(); // second call is a no-op (guard inside)
		});
		engine.StopRadar();
		Assert.Null(ex);
	}

	[Fact]
	public void StopRadar_BeforeStart_DoesNotThrow()
	{
		var engine = CreateEngine();
		var ex = Record.Exception(() => engine.StopRadar());
		Assert.Null(ex);
	}
}

using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Engine;

public class LoadDirectoryItemsTests
{
    private readonly DiskScannerEngine _engine;

    public LoadDirectoryItemsTests()
    {
        var hubMock = new Mock<IHubContext<SystemHub>>();
        var settingsMock = new Mock<ISettingService>();
        var loggerMock = new Mock<ILogger<DiskScannerEngine>>();

        _engine = new DiskScannerEngine(
            hubMock.Object,
            settingsMock.Object,
            loggerMock.Object);
    }

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	
	public void LoadDirectoryItems_InvalidPath_ReturnsEmptyList(string? path)
	{
		var result = _engine.LoadDirectoryItems(path!);

		Assert.Empty(result);
	}
}

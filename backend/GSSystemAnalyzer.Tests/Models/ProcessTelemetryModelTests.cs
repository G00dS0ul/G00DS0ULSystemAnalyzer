using GSSystemAnalyzer.Models;
using Xunit;

namespace GSSystemAnalyzer.Tests.Models;

public class ProcessTelemetryModelTests
{
	[Fact]
	public void RamMb_ConvertsWorkingSetBytesCorrectly()
	{
		var p = new ProcessTelemetry { WorkingSetBytes = 104_857_600 }; // 100 MB
		Assert.Equal(100.0, p.RamMb);
	}

	[Fact]
	public void RamMb_IsRoundedToTwoDecimalPlaces()
	{
		var p = new ProcessTelemetry { WorkingSetBytes = 1_500_000 }; // 1.43 MB
		Assert.Equal(Math.Round(1_500_000 / 1024.0 / 1024.0, 2), p.RamMb);
	}

	[Fact]
	public void RamMb_IsZeroWhenWorkingSetIsZero()
	{
		var p = new ProcessTelemetry { WorkingSetBytes = 0 };
		Assert.Equal(0.0, p.RamMb);
	}

	[Fact]
	public void DefaultUser_IsSYSTEM()
	{
		var p = new ProcessTelemetry();
		Assert.Equal("SYSTEM", p.User);
	}

	[Fact]
	public void DefaultStatus_IsRUNNING()
	{
		var p = new ProcessTelemetry();
		Assert.Equal("RUNNING", p.Status);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void RamMb_NeverGoesNegative(long bytes)
	{
		var p = new ProcessTelemetry { WorkingSetBytes = bytes };
		Assert.True(p.RamMb <= 0);
	}

	[Fact]
	public void AllPropertiesAssignableAndReadBack()
	{
		var p = new ProcessTelemetry
		{
			ProcessId = 1234,
			Name = "devenv",
			WorkingSetBytes = 52_428_800,
			CpuPercent = 12.5,
			Status = "SLEEPING",
			User = "G00dS0ul"
		};

		Assert.Equal(1234, p.ProcessId);
		Assert.Equal("devenv", p.Name);
		Assert.Equal(12.5, p.CpuPercent);
		Assert.Equal("SLEEPING", p.Status);
		Assert.Equal("G00dS0ul", p.User);
		Assert.Equal(50.0, p.RamMb); // 50 MB
	}
}

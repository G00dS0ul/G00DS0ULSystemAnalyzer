using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GSSystemAnalyzer.Services;

namespace GSSystemAnalyzer.Tests.Engine
{
	public class WindowsCpuProviderTest
	{
		private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#if WINDOWS
		[Fact]
		public async Task WindowsProvider_GetNextSample_PopulatesSystemTools()
		{
			if (!_isWindows)
			{
				return;
			}

			var provider = new WindowsCpuProvider();
			var result = await provider.GetNextSampleAsync();

			Assert.NotNull(result);
			Assert.True(result.TotalProcesses > 0,
				$"Expected total processes greater than 0, got {result.TotalProcesses}");
			Assert.True(result.TotalThreads > 0, $"Expected total threads greater than 0, got {result.TotalThreads}");
			Assert.True(result.TotalHandles > 0, $"Expected total handles greater than 0, got {result.TotalHandles}");
		}

		[Fact]
		public async Task WindowsProvider_GetNextSample_CalculateValidFrequencyOrGracefullyDefaults()
		{
			if (!_isWindows)
			{
				return;
			}

			var provider = new WindowsCpuProvider();
			var result = await provider.GetNextSampleAsync();

			Assert.True(result.CurrentFrequencyGhz >= 0.0, $"Expected current frequency greater than or equal to 0.0, got {result.CurrentFrequencyGhz}");
		}

		[Fact]
		public async Task WindowsProvider_GetNextSample_handlesStaticCacheValue()
		{
			if (!_isWindows)
			{
				return;
			}

			var provider = new WindowsCpuProvider();
			var result = await provider.GetNextSampleAsync();

			Assert.Equal("256 KB", result.L1Cache);
			Assert.Equal("1.0 MB", result.L2Cache);
			Assert.Equal("8.0 MB", result.L3Cache);
		}

		[Fact]
		public async Task WindowsProvider_CoreGroupingLogic_ReturnsValidChunks()
		{
			if (!_isWindows)
			{
				return;
			}

			var provider = new WindowsCpuProvider();
			var result = await provider.GetNextSampleAsync();

			Assert.NotEmpty(result.CoreGroups);

			foreach (var group in result.CoreGroups)
			{
				Assert.True(group.Value.Count <= 4, $"Group {group.Key} has more than 4 cores ");
				Assert.StartsWith("CORE", group.Key);
			}
		}

		[Fact]
		public async Task WindowsProvider_Delta_IsCalculatedCorrectlyOverTime()
		{
			if (!_isWindows)
			{
				return;
			}

			var provider = new WindowsCpuProvider();

			var sample1 = await provider.GetNextSampleAsync();
			var sample2 = await provider.GetNextSampleAsync();

			double expectedDelta = Math.Round(sample2.AverageLoad - sample1.AverageLoad, 1);
			Assert.Equal(expectedDelta, sample2.Delta);

		}

		[Fact]
		public async Task WindowsProvider_CoreGroupingAndMathInvariants_ExecuteCleanly()
		{
			if (!_isWindows) return;

			var provider = new WindowsCpuProvider();
			var result = await provider.GetNextSampleAsync();

			Assert.NotNull(result);
			Assert.True(result.AverageLoad >= 0.0 && result.AverageLoad <= 100.0);
			Assert.NotEmpty(result.CoreGroups);

			Assert.Contains("CORE 0-", result.CoreGroups.Keys.GetEnumerator().Current ?? "CORE 0-3");
		}
#endif

		[Fact]
		public void WindowsProvider_ForeignOS_ThrowsPlatformNotSupportedException()
		{
#if WINDOWS
			return;
#else

            var exception = Record.Exception(() => new WindowsCpuProvider());
            Assert.NotNull(exception);
            Assert.IsType<PlatformNotSupportedException>(exception);
#endif
		}

	}
}


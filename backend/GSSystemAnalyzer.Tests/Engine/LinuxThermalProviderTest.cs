using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Services;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Engine
{
	public class LinuxThermalProviderTests
	{
		private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#if !WINDOWS
        // ??? 1 & 2: Thermal Zones and Mapping
        [Fact]
        public async Task GetThermalData_ThermalZones_MapsCorrectlyAndIgnoresMissing()
        {
            if (_isWindows) return;

            var mockFs = new Mock<IFileSystemProvider>();
            mockFs.Setup(fs => fs.DirectoryExists("/sys/class/thermal/")).Returns(true);
            mockFs.Setup(fs => fs.GetDirectories("/sys/class/thermal/", "thermal_zone*"))
                  .Returns(new[] { "/sys/class/thermal/thermal_zone0", "/sys/class/thermal/thermal_zone1" });

            // Zone 0: CPU Package (x86_pkg_temp)
            mockFs.Setup(fs => fs.FileExists(Path.Combine("/sys/class/thermal/thermal_zone0", "type"))).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText(Path.Combine("/sys/class/thermal/thermal_zone0", "type"))).Returns("x86_pkg_temp");
            mockFs.Setup(fs => fs.FileExists(Path.Combine("/sys/class/thermal/thermal_zone0", "temp"))).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText(Path.Combine("/sys/class/thermal/thermal_zone0", "temp"))).Returns("45500"); // 45.5C

            // Zone 1: Motherboard (acpitz)
            mockFs.Setup(fs => fs.FileExists(Path.Combine("/sys/class/thermal/thermal_zone1", "type"))).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText(Path.Combine("/sys/class/thermal/thermal_zone1", "type"))).Returns("acpitz");
            mockFs.Setup(fs => fs.FileExists(Path.Combine("/sys/class/thermal/thermal_zone1", "temp"))).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText(Path.Combine("/sys/class/thermal/thermal_zone1", "temp"))).Returns("38000"); // 38.0C

            var provider = new LinuxThermalProvider(mockFs.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Equal(45.5, result.CpuPackageCelsius);
            Assert.Equal(38.0, result.MotherboardCelsius);
        }

        // ??? 3: FANS (Return 0 when absent)
        [Fact]
        public async Task GetThermalData_Fans_ReturnZeroWhenAbsent()
        {
            if (_isWindows) return;

            var mockFs = new Mock<IFileSystemProvider>();
            // Return no directories to simulate an absent fan controller
            mockFs.Setup(fs => fs.DirectoryExists("/sys/class/hwmon/")).Returns(false); 

            var provider = new LinuxThermalProvider(mockFs.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Equal(0, result.CpuFanRpm);
            Assert.Equal(0, result.ChassisFan1Rpm);
            Assert.Equal(0, result.ChassisFan2Rpm);
            Assert.Null(result.PumpRpm); // Pump should remain null
        }

        // ??? 4: RAPL Power Calculation (1 Second Delta)
        [Fact]
        public async Task GetThermalData_RaplWatts_CalculatesDeltaSuccessfully()
        {
            if (_isWindows) return;

            var mockFs = new Mock<IFileSystemProvider>();
            string raplPath = "/sys/class/powercap/intel-rapl/intel-rapl:0/energy_uj";
            
            mockFs.Setup(fs => fs.FileExists(raplPath)).Returns(true);

            // SetupSequence forces the mock to return a different value the second time it is called!
            mockFs.SetupSequence(fs => fs.ReadAllText(raplPath))
                  .Returns("1000000")   // Tick 1
                  .Returns("4500000");  // Tick 2 (1 second later) -> Delta is 3,500,000 uj = 3.5 Watts!

            var provider = new LinuxThermalProvider(mockFs.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Equal(3.5, result.CpuPowerWatts);
        }

        // ??? 5: Throttling Check
        [Fact]
        public async Task GetThermalData_Throttling_DetectedOnFrequencyDrop_DoesNotThrowIfMissing()
        {
            if (_isWindows) return;

            var mockFs = new Mock<IFileSystemProvider>();
            
            // Missing Files Test (Does not throw, stays false)
            var missingProvider = new LinuxThermalProvider(mockFs.Object);
            var missingResult = await missingProvider.GetThermalDataAsync();
            Assert.False(missingResult.IsThermalThrottling);

            // Throttling Logic Test (e.g. Max 4000, Cur 3000 -> < 80%!)
            mockFs.Setup(fs => fs.FileExists("/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq")).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText("/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq")).Returns("3000");
            
            mockFs.Setup(fs => fs.FileExists("/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq")).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText("/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq")).Returns("4000");

            var provider = new LinuxThermalProvider(mockFs.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.True(result.IsThermalThrottling);
        }

        // ??? 6: NVMe Thermal
        [Fact]
        public async Task GetThermalData_Nvme_DividesBy1000()
        {
            if (_isWindows) return;

            var mockFs = new Mock<IFileSystemProvider>();
            mockFs.Setup(fs => fs.DirectoryExists("/sys/class/nvme/")).Returns(true);
            mockFs.Setup(fs => fs.GetDirectories("/sys/class/nvme/")).Returns(new[] { "/sys/class/nvme/nvme0" });
            
            var hwmonPath = Path.Combine("/sys/class/nvme/nvme0", "hwmon");
            mockFs.Setup(fs => fs.DirectoryExists(hwmonPath)).Returns(true);
            mockFs.Setup(fs => fs.GetDirectories(hwmonPath)).Returns(new[] { Path.Combine(hwmonPath, "hwmon0") });

            var tempFilePath = Path.Combine(hwmonPath, "hwmon0", "temp1_input");
            mockFs.Setup(fs => fs.FileExists(tempFilePath)).Returns(true);
            mockFs.Setup(fs => fs.ReadAllText(tempFilePath)).Returns("41200"); // 41.2C

            var provider = new LinuxThermalProvider(mockFs.Object);
            var result = await provider.GetThermalDataAsync();

            Assert.Equal(41.2, result.NvmeCelsius);
        }
#else
		[Fact]
		public async Task LinuxThermalProvider_OnWindows_ThrowsPlatformNotSupportedException()
		{
			var provider = new LinuxThermalProvider();
			var exception = await Record.ExceptionAsync(() => provider.GetThermalDataAsync());
			Assert.NotNull(exception);
			Assert.IsType<PlatformNotSupportedException>(exception);
		}
#endif
	}
}

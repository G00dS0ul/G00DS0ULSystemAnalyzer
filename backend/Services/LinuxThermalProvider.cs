using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GSSystemAnalyzer.Services
{
#if !WINDOWS
    public class LinuxThermalProvider : IThermalProvider
    {
        private readonly IFileSystemProvider _fileSystem;

        // ?? INJECT THE FILE SYSTEM (Defaults to real physical OS in production)
        public LinuxThermalProvider(IFileSystemProvider? fileSystem = null)
        {
            _fileSystem = fileSystem ?? new PhysicalFileSystemProvider();
        }

        public async Task<ThermalTelemetryDto> GetThermalDataAsync()
        {
            var payload = new ThermalTelemetryDto
            {
                // ?? FIXED: Default missing fans to 0, just like we did for Windows!
                CpuFanRpm = 0,
                ChassisFan1Rpm = 0,
                ChassisFan2Rpm = 0,
                CoreCelsius = new List<double>(),
                IsThermalThrottling = false
            };

            await Task.Run(async () =>
            {
                // THERMAL ZONES (Temperatures)
                try
                {
                    if (_fileSystem.DirectoryExists("/sys/class/thermal/"))
                    {
                        foreach (var dir in _fileSystem.GetDirectories("/sys/class/thermal/", "thermal_zone*"))
                        {
                            var type = ReadSysFsSafe(Path.Combine(dir, "type"));
                            var tempStr = ReadSysFsSafe(Path.Combine(dir, "temp"));

                            if (type != null && double.TryParse(tempStr, out double milliCelsius))
                            {
                                double celsius = Math.Round(milliCelsius / 1000.0, 1);

                                if (type.Contains("x86_pkg_temp", StringComparison.OrdinalIgnoreCase))
                                    payload.CpuPackageCelsius = celsius;
                                else if (type.Contains("acpitz", StringComparison.OrdinalIgnoreCase))
                                    payload.MotherboardCelsius = celsius;
                                else if (type.Contains("core", StringComparison.OrdinalIgnoreCase))
                                    payload.CoreCelsius!.Add(celsius);
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // FANS (RPM)
                try
                {
                    if (_fileSystem.DirectoryExists("/sys/class/hwmon/"))
                    {
                        int fanIndex = 0;
                        foreach (var hwmon in _fileSystem.GetDirectories("/sys/class/hwmon/"))
                        {
                            foreach (var fanFile in _fileSystem.GetFiles(hwmon, "fan*_input"))
                            {
                                var rpmStr = ReadSysFsSafe(fanFile);
                                if (int.TryParse(rpmStr, out int rpm))
                                {
                                    if (fanIndex == 0) payload.CpuFanRpm = rpm;
                                    else if (fanIndex == 1) payload.ChassisFan1Rpm = rpm;
                                    else if (fanIndex == 2) payload.ChassisFan2Rpm = rpm;
                                    else if (fanIndex == 3) payload.PumpRpm = rpm;
                                    fanIndex++;
                                }
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // THROTTLING (CPU Frequencies)
                try
                {
                    var curFreqStr = ReadSysFsSafe("/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq");
                    var maxFreqStr = ReadSysFsSafe("/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq");

                    if (double.TryParse(curFreqStr, out double cur) && double.TryParse(maxFreqStr, out double max))
                    {
                        if (max > 0 && (cur / max) < 0.8) 
                        {
                            payload.IsThermalThrottling = true;
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // NVMe TEMPERATURES
                try
                {
                    if (_fileSystem.DirectoryExists("/sys/class/nvme/"))
                    {
                        foreach (var nvmeDir in _fileSystem.GetDirectories("/sys/class/nvme/"))
                        {
                            var hwmonPath = Path.Combine(nvmeDir, "hwmon");
                            if (_fileSystem.DirectoryExists(hwmonPath))
                            {
                                foreach (var hwmon in _fileSystem.GetDirectories(hwmonPath))
                                {
                                    var tempStr = ReadSysFsSafe(Path.Combine(hwmon, "temp1_input"));
                                    if (double.TryParse(tempStr, out double milliCelsius))
                                    {
                                        double celsius = Math.Round(milliCelsius / 1000.0, 1);
                                        if (payload.NvmeCelsius == null || celsius > payload.NvmeCelsius)
                                        {
                                            payload.NvmeCelsius = celsius;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }

                // INTEL RAPL (Power Watts via Delta)
                try
                {
                    string raplPath = "/sys/class/powercap/intel-rapl/intel-rapl:0/energy_uj";
                    var energy1Str = ReadSysFsSafe(raplPath);

                    if (long.TryParse(energy1Str, out long energy1))
                    {
                        await Task.Delay(1000); // 1-second delta Wait

                        var energy2Str = ReadSysFsSafe(raplPath);
                        if (long.TryParse(energy2Str, out long energy2))
                        {
                            long delta = energy2 - energy1;
                            if (delta > 0)
                            {
                                payload.CpuPowerWatts = Math.Round(delta / 1000000.0, 1);
                            }
                        }
                    }
                }
                catch { /* Graceful degradation */ }
            });

            return payload;
        }

        // ?? SAFE READ: Uses the injected File System so Moq can track it!
        private string? ReadSysFsSafe(string path)
        {
            try
            {
                if (_fileSystem.FileExists(path))
                {
                    return _fileSystem.ReadAllText(path).Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose() { }
    }
#else
	public class LinuxThermalProvider : IThermalProvider
	{
		public Task<ThermalTelemetryDto> GetThermalDataAsync() =>
			throw new PlatformNotSupportedException("LinuxThermalProvider is only supported on Linux.");

		public void Dispose() { }
	}
#endif
}

using System;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services
{
	public class WmiThermalFallback : IWmiThermalFallback
	{
		private readonly ILogger<WmiThermalFallback> _logger;

		public WmiThermalFallback(ILogger<WmiThermalFallback> logger)
		{
			_logger = logger;
		}

		public double? GetCpuTemperatureCelsius()
		{
			try
			{
#pragma warning disable CA1416
				using var searcher = new System.Management.ManagementObjectSearcher("root\\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
				foreach (System.Management.ManagementObject obj in searcher.Get())
				{
					var tempK = Convert.ToDouble(obj["CurrentTemperature"]);
					var validTemp = ConvertKelvinToCelsius(tempK);

					if (validTemp.HasValue)
					{
						return validTemp;
					}
				}
#pragma warning restore CA1416
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "WMI thermal fallback query failed");
			}

			return null;
		}

		public static double? ConvertKelvinToCelsius(double tempK)
		{
			var celsius = (tempK / 10.0) - 273.15;

			if (celsius > 10 && celsius < 120)
			{
				return Math.Round(celsius, 1);
			}
			return null;
		}
	}
}

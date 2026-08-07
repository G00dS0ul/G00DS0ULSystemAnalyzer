using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using GSSystemAnalyzer.Interfaces;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services
{
	public class SystemNetworkInterfaceProvider : INetworkInterfaceProvider
	{
		private readonly ILogger<SystemNetworkInterfaceProvider> _logger;

		public SystemNetworkInterfaceProvider(ILogger<SystemNetworkInterfaceProvider> logger)
		{
			_logger = logger;
		}

		public IReadOnlyList<NetworkInterfaceInfo> GetInterfaces()
		{
			var result = new List<NetworkInterfaceInfo>();

			try
			{
				var interfaces = NetworkInterface.GetAllNetworkInterfaces();

				foreach (var nic in interfaces)
				{
					try
					{
						long rx = 0;
						long tx = 0;

						try
						{
							var stats = nic.GetIPStatistics();
							rx = stats.BytesReceived;
							tx = stats.BytesSent;
						}
						catch (Exception ex)
						{
							_logger.LogDebug(ex, "Failed to read IP statistics for adapter {AdapterId} ({AdapterName})", nic.Id, nic.Name);
						}

						long speed = -1;
						try
						{
							speed = nic.Speed;
						}
						catch
						{
							speed = -1;
						}

						result.Add(new NetworkInterfaceInfo
						{
							Id = nic.Id,
							Name = nic.Name,
							Description = nic.Description,
							NetworkInterfaceType = nic.NetworkInterfaceType,
							OperationalStatus = nic.OperationalStatus,
							Speed = speed,
							BytesReceived = rx,
							BytesSent = tx
						});
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to process network interface {AdapterId}", nic.Id);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to enumerate network interfaces from host system");
			}

			return result;
		}
	}
}

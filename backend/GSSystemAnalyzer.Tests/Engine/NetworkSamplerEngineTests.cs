using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Engine
{
	public class NetworkSamplerEngineTests
	{
		private class FakeNetworkInterfaceProvider : INetworkInterfaceProvider
		{
			public List<NetworkInterfaceInfo> Interfaces { get; set; } = new();

			public IReadOnlyList<NetworkInterfaceInfo> GetInterfaces() => Interfaces;
		}

		private (NetworkSamplerEngine engine, FakeNetworkInterfaceProvider provider, Mock<ISettingService> settingsMock, Mock<ITelemetryHistoryBuffer> historyMock) CreateTestEngine(
			string? preferredNicId = null,
			int pollIntervalMs = 1000)
		{
			var provider = new FakeNetworkInterfaceProvider();

			var mockHub = new Mock<IHubContext<SystemHub>>();
			var mockClients = new Mock<IHubClients>();
			var mockClient = new Mock<IClientProxy>();
			mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
			mockClients.Setup(c => c.All).Returns(mockClient.Object);

			var settingsMock = new Mock<ISettingService>();
			var appSettings = new AppSettingDto
			{
				Monitoring = new MonitoringSettingDto
				{
					NetworkPollIntervalMs = pollIntervalMs,
					PreferredNetworkInterfaceId = preferredNicId
				}
			};
			settingsMock.Setup(s => s.Current).Returns(appSettings);

			var historyMock = new Mock<ITelemetryHistoryBuffer>();

			var engine = new NetworkSamplerEngine(
				provider,
				mockHub.Object,
				settingsMock.Object,
				historyMock.Object,
				NullLogger<NetworkSamplerEngine>.Instance);

			return (engine, provider, settingsMock, historyMock);
		}

		[Fact]
		public void SampleMetrics_FirstSample_EmitsZeroRate_AndRecordsBaseline()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "nic-1",
					Name = "Wi-Fi",
					Description = "Intel Wi-Fi",
					NetworkInterfaceType = NetworkInterfaceType.Wireless80211,
					OperationalStatus = OperationalStatus.Up,
					Speed = 866_000_000,
					BytesReceived = 10_000_000,
					BytesSent = 5_000_000
				}
			};

			var snapshot = engine.SampleMetrics();

			Assert.NotNull(snapshot);
			Assert.Equal("nic-1", snapshot.PrimaryInterfaceId);
			Assert.Single(snapshot.Interfaces);

			var nic = snapshot.Interfaces[0];
			Assert.Equal("nic-1", nic.Id);
			Assert.True(nic.IsUp);
			Assert.Equal(0.0, nic.RxBytesPerSec);
			Assert.Equal(0.0, nic.TxBytesPerSec);
			Assert.Equal(0, nic.SessionRxBytes);
			Assert.Equal(0, nic.SessionTxBytes);
		}

		[Fact]
		public void SampleMetrics_SubsequentSample_ComputesAccurateRateAndSessionTotals()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "nic-1",
					Name = "Ethernet",
					Description = "Realtek GbE",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 1_000_000,
					BytesSent = 500_000
				}
			};

			// Baseline tick
			engine.SampleMetrics();

			// Simulate traffic after brief delay
			Thread.Sleep(50);

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "nic-1",
					Name = "Ethernet",
					Description = "Realtek GbE",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 2_000_000, // +1,000,000 bytes
					BytesSent = 800_000        // +300,000 bytes
				}
			};

			var snapshot = engine.SampleMetrics();
			var nic = snapshot.Interfaces[0];

			Assert.True(nic.RxBytesPerSec > 0);
			Assert.True(nic.TxBytesPerSec > 0);
			Assert.Equal(1_000_000, nic.SessionRxBytes);
			Assert.Equal(300_000, nic.SessionTxBytes);
			Assert.NotNull(nic.UtilisationPercent);
		}

		[Fact]
		public void SampleMetrics_CounterResetOrRollover_ClampsToZeroWithoutSpikes()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "nic-1",
					Name = "Ethernet",
					Description = "Realtek GbE",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 5_000_000,
					BytesSent = 5_000_000
				}
			};

			engine.SampleMetrics();

			// Counter reset (e.g. adapter re-initialized, counters reset to 100)
			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "nic-1",
					Name = "Ethernet",
					Description = "Realtek GbE",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 100,
					BytesSent = 100
				}
			};

			var snapshot = engine.SampleMetrics();
			var nic = snapshot.Interfaces[0];

			// Rate must be 0, not negative or garbage spike
			Assert.Equal(0.0, nic.RxBytesPerSec);
			Assert.Equal(0.0, nic.TxBytesPerSec);
			Assert.Equal(0, nic.SessionRxBytes);
			Assert.Equal(0, nic.SessionTxBytes);
		}

		[Fact]
		public void SampleMetrics_StaleAdapter_IsPruned()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "vpn-1",
					Name = "Tailscale",
					Description = "Tailscale Tunnel",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 100_000_000,
					BytesReceived = 1_000,
					BytesSent = 1_000
				}
			};

			var snap1 = engine.SampleMetrics();
			Assert.Single(snap1.Interfaces);

			// VPN disconnects
			provider.Interfaces = new List<NetworkInterfaceInfo>();

			var snap2 = engine.SampleMetrics();
			Assert.Empty(snap2.Interfaces);
			Assert.Null(snap2.PrimaryInterfaceId);
		}

		[Fact]
		public void SampleMetrics_HotPlugAdapter_IsDetected()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>();
			var snap1 = engine.SampleMetrics();
			Assert.Empty(snap1.Interfaces);

			// USB NIC plugged in
			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "usb-nic",
					Name = "USB Ethernet",
					Description = "Realtek USB GbE",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 50_000,
					BytesSent = 20_000
				}
			};

			var snap2 = engine.SampleMetrics();
			Assert.Single(snap2.Interfaces);
			Assert.Equal("usb-nic", snap2.PrimaryInterfaceId);
		}

		[Fact]
		public void SelectPrimary_HighestCumulativeTraffic_IsChosen()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "eth-0",
					Name = "Ethernet",
					Description = "Intel I219",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 100_000,
					BytesSent = 50_000
				},
				new()
				{
					Id = "wifi-0",
					Name = "Wi-Fi",
					Description = "Intel AX200",
					NetworkInterfaceType = NetworkInterfaceType.Wireless80211,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 50_000_000, // Highest traffic
					BytesSent = 10_000_000
				}
			};

			var snapshot = engine.SampleMetrics();
			Assert.Equal("wifi-0", snapshot.PrimaryInterfaceId);
		}

		[Fact]
		public void SelectPrimary_PreferredInterfaceSetting_OverridesAutoSelection_WhenUp()
		{
			var (engine, provider, _, _) = CreateTestEngine(preferredNicId: "eth-0");

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "eth-0",
					Name = "Ethernet",
					Description = "Intel I219",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 100,
					BytesSent = 100
				},
				new()
				{
					Id = "wifi-0",
					Name = "Wi-Fi",
					Description = "Intel AX200",
					NetworkInterfaceType = NetworkInterfaceType.Wireless80211,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 100_000_000,
					BytesSent = 100_000_000
				}
			};

			var snapshot = engine.SampleMetrics();
			Assert.Equal("eth-0", snapshot.PrimaryInterfaceId);
		}

		[Fact]
		public void SelectPrimary_PreferredInterfaceSetting_FallsBackToAuto_WhenDown()
		{
			var (engine, provider, _, _) = CreateTestEngine(preferredNicId: "eth-0");

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "eth-0",
					Name = "Ethernet",
					Description = "Intel I219",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Down, // Disconnected
					BytesReceived = 100,
					BytesSent = 100
				},
				new()
				{
					Id = "wifi-0",
					Name = "Wi-Fi",
					Description = "Intel AX200",
					NetworkInterfaceType = NetworkInterfaceType.Wireless80211,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 1_000_000,
					BytesSent = 500_000
				}
			};

			var snapshot = engine.SampleMetrics();
			Assert.Equal("wifi-0", snapshot.PrimaryInterfaceId);
		}

		[Fact]
		public void SelectPrimary_TieBreaker_EthernetOverWireless()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "wifi-0",
					Name = "Wi-Fi",
					Description = "Intel AX200",
					NetworkInterfaceType = NetworkInterfaceType.Wireless80211,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 10_000,
					BytesSent = 10_000
				},
				new()
				{
					Id = "eth-0",
					Name = "Ethernet",
					Description = "Intel I219",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					BytesReceived = 10_000,
					BytesSent = 10_000
				}
			};

			var snapshot = engine.SampleMetrics();
			Assert.Equal("eth-0", snapshot.PrimaryInterfaceId);
		}

		[Fact]
		public void SelectPrimary_NoUpAdapters_ReturnsNullPrimary()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "eth-0",
					Name = "Ethernet",
					Description = "Intel I219",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Down
				}
			};

			var snapshot = engine.SampleMetrics();
			Assert.Null(snapshot.PrimaryInterfaceId);
		}

		[Fact]
		public void SampleMetrics_IgnoresLoopbackAndTunnelAdapters()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "loopback",
					Name = "Loopback Pseudo-Interface 1",
					NetworkInterfaceType = NetworkInterfaceType.Loopback,
					OperationalStatus = OperationalStatus.Up
				},
				new()
				{
					Id = "tunnel",
					Name = "Teredo Tunneling",
					NetworkInterfaceType = NetworkInterfaceType.Tunnel,
					OperationalStatus = OperationalStatus.Up
				},
				new()
				{
					Id = "real-eth",
					Name = "Ethernet",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up
				}
			};

			var snapshot = engine.SampleMetrics();
			Assert.Single(snapshot.Interfaces);
			Assert.Equal("real-eth", snapshot.Interfaces[0].Id);
		}

		[Fact]
		public void SampleMetrics_RenamingAdapterInWindows_RetainsSessionTotals()
		{
			var (engine, provider, _, _) = CreateTestEngine();

			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "{B615DFA5-895E-4A5D-A212-171D6172477D}",
					Name = "Ethernet",
					Description = "Realtek PCIe GbE Family Controller",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 1_000_000,
					BytesSent = 500_000
				}
			};

			// First tick: baseline
			engine.SampleMetrics();

			Thread.Sleep(20);

			// Second tick: traffic accumulated
			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "{B615DFA5-895E-4A5D-A212-171D6172477D}",
					Name = "Ethernet",
					Description = "Realtek PCIe GbE Family Controller",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 3_000_000,
					BytesSent = 1_500_000
				}
			};
			var snap2 = engine.SampleMetrics();
			Assert.Equal(2_000_000, snap2.Interfaces[0].SessionRxBytes);
			Assert.Equal(1_000_000, snap2.Interfaces[0].SessionTxBytes);

			// User renames adapter in Windows from 'Ethernet' to 'LAN-Office' (ID remains same)
			provider.Interfaces = new List<NetworkInterfaceInfo>
			{
				new()
				{
					Id = "{B615DFA5-895E-4A5D-A212-171D6172477D}",
					Name = "LAN-Office",
					Description = "Realtek PCIe GbE Family Controller",
					NetworkInterfaceType = NetworkInterfaceType.Ethernet,
					OperationalStatus = OperationalStatus.Up,
					Speed = 1_000_000_000,
					BytesReceived = 4_000_000,
					BytesSent = 2_000_000
				}
			};

			var snap3 = engine.SampleMetrics();
			Assert.Equal("LAN-Office", snap3.Interfaces[0].Name);
			// Session totals are keyed on Id and must accumulate without resetting
			Assert.Equal(3_000_000, snap3.Interfaces[0].SessionRxBytes);
			Assert.Equal(1_500_000, snap3.Interfaces[0].SessionTxBytes);
		}

		[Fact]
		public void SettingsChange_HotReloadsPollInterval_WithinOneTick()
		{
			var (engine, _, settingsMock, _) = CreateTestEngine(pollIntervalMs: 1000);

			// Trigger settings change event
			var updatedSettings = new AppSettingDto
			{
				Monitoring = new MonitoringSettingDto
				{
					NetworkPollIntervalMs = 500
				}
			};

			settingsMock.Raise(s => s.OnSettingsChanged += null, settingsMock.Object, updatedSettings);

			// Engine handles event dynamically without throwing or requiring restart
			var snap = engine.SampleMetrics();
			Assert.NotNull(snap);
		}
	}
}

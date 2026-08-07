using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Controllers
{
	public class NetworkControllerTests
	{
		[Fact]
		public void GetInterfaces_ReturnsSnapshotFromEngine()
		{
			var engineMock = new Mock<INetworkEngine>();
			var settingsMock = new Mock<ISettingService>();
			var providerMock = new Mock<INetworkInterfaceProvider>();

			var expectedSnapshot = new NetworkSnapshot(
				DateTimeOffset.UtcNow,
				"nic-1",
				new List<NetInterfaceSnapshot>
				{
					new("nic-1", "Wi-Fi", "Intel AX200", "Wireless80211", true, 866000000, 1000, 500, 1.5, 5000, 2500)
				});

			engineMock.Setup(e => e.GetCurrentSnapshot()).Returns(expectedSnapshot);

			var controller = new NetworkController(engineMock.Object, settingsMock.Object, providerMock.Object);

			var result = controller.GetInterfaces();

			var okResult = Assert.IsType<OkObjectResult>(result);
			var snapshot = Assert.IsType<NetworkSnapshot>(okResult.Value);
			Assert.Equal("nic-1", snapshot.PrimaryInterfaceId);
			Assert.Single(snapshot.Interfaces);
		}

		[Fact]
		public async Task SetPrimaryInterface_ValidId_SavesPreferenceAndReturnsOk()
		{
			var engineMock = new Mock<INetworkEngine>();
			var settingsMock = new Mock<ISettingService>();
			var providerMock = new Mock<INetworkInterfaceProvider>();

			var appSettings = new AppSettingDto();
			settingsMock.Setup(s => s.Current).Returns(appSettings);
			settingsMock.Setup(s => s.SaveAsync(It.IsAny<AppSettingDto>())).Returns(Task.CompletedTask);

			providerMock.Setup(p => p.GetInterfaces()).Returns(new List<NetworkInterfaceInfo>
			{
				new() { Id = "nic-target", Name = "Ethernet 2", NetworkInterfaceType = NetworkInterfaceType.Ethernet, OperationalStatus = OperationalStatus.Up }
			});

			var controller = new NetworkController(engineMock.Object, settingsMock.Object, providerMock.Object);

			var result = await controller.SetPrimaryInterface(new SetPreferredInterfaceRequest("nic-target"));

			var okResult = Assert.IsType<OkObjectResult>(result);
			Assert.Equal("nic-target", appSettings.Monitoring.PreferredNetworkInterfaceId);
			settingsMock.Verify(s => s.SaveAsync(appSettings), Times.Once);
		}

		[Fact]
		public async Task SetPrimaryInterface_NullOrEmptyId_ClearsPreferenceAndReturnsOk()
		{
			var engineMock = new Mock<INetworkEngine>();
			var settingsMock = new Mock<ISettingService>();
			var providerMock = new Mock<INetworkInterfaceProvider>();

			var appSettings = new AppSettingDto
			{
				Monitoring = new MonitoringSettingDto { PreferredNetworkInterfaceId = "old-nic" }
			};
			settingsMock.Setup(s => s.Current).Returns(appSettings);
			settingsMock.Setup(s => s.SaveAsync(It.IsAny<AppSettingDto>())).Returns(Task.CompletedTask);

			var controller = new NetworkController(engineMock.Object, settingsMock.Object, providerMock.Object);

			var result = await controller.SetPrimaryInterface(new SetPreferredInterfaceRequest(null));

			var okResult = Assert.IsType<OkObjectResult>(result);
			Assert.Null(appSettings.Monitoring.PreferredNetworkInterfaceId);
			settingsMock.Verify(s => s.SaveAsync(appSettings), Times.Once);
		}

		[Fact]
		public async Task SetPrimaryInterface_InvalidId_ReturnsBadRequest()
		{
			var engineMock = new Mock<INetworkEngine>();
			var settingsMock = new Mock<ISettingService>();
			var providerMock = new Mock<INetworkInterfaceProvider>();

			var appSettings = new AppSettingDto();
			settingsMock.Setup(s => s.Current).Returns(appSettings);

			providerMock.Setup(p => p.GetInterfaces()).Returns(new List<NetworkInterfaceInfo>
			{
				new() { Id = "existing-nic", Name = "Ethernet", NetworkInterfaceType = NetworkInterfaceType.Ethernet }
			});

			var controller = new NetworkController(engineMock.Object, settingsMock.Object, providerMock.Object);

			var result = await controller.SetPrimaryInterface(new SetPreferredInterfaceRequest("non-existent-nic"));

			var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
			settingsMock.Verify(s => s.SaveAsync(It.IsAny<AppSettingDto>()), Times.Never);
		}
	}
}

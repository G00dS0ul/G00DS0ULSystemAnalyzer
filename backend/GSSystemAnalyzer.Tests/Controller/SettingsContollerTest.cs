using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GSSystemAnalyzer.Tests.Controller
{
	public class SettingsControllerTests
	{
		private static SettingsController MakeController(
			Mock<ISettingService> mockService,
			Mock<IDiskScannerEngine>? mockScanner = null)
		{
			var scanner = mockScanner ?? new Mock<IDiskScannerEngine>();
			return new SettingsController(mockService.Object, scanner.Object);
		}

		[Fact]
		public async Task SaveSettings_WhenValidationFails_ReturnsBadRequestAndDoesNotSave()
		{
			var mockService = new Mock<ISettingService>();
			var controller = MakeController(mockService);

			var badSettings = AppSettingDto.GetFactoryDefaults();
			badSettings.Advanced.BackendPort = 10;

			var result = await controller.SaveSettings(badSettings);

			Assert.IsType<BadRequestObjectResult>(result);
			mockService.Verify(s => s.SaveAsync(It.IsAny<AppSettingDto>()), Times.Never);
		}

		[Fact]
		public async Task SaveSettings_WhenValidationPasses_ReturnsOkAndCallsSave()
		{
			var mockService = new Mock<ISettingService>();
			var validSettings = AppSettingDto.GetFactoryDefaults();
			mockService.Setup(s => s.Current).Returns(validSettings);

			var controller = MakeController(mockService);

			var result = await controller.SaveSettings(validSettings);

			Assert.IsType<OkObjectResult>(result);
			mockService.Verify(s => s.SaveAsync(validSettings), Times.Once);
		}

		[Fact]
		public async Task ResetSettings_Always_OverwritesWithFactoryDefaults()
		{
			var mockService = new Mock<ISettingService>();
			var controller = MakeController(mockService);

			var result = await controller.ResetSettings();

			Assert.IsType<OkObjectResult>(result);
			mockService.Verify(s => s.SaveAsync(It.IsAny<AppSettingDto>()), Times.Once);
		}
	}
}

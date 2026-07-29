using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Tests.Controller;

public class ScanControllerMultiDriveTests
{
	private readonly StorageController _controller;
	private readonly IDriveDetectionService _fakeDriveService;
	private readonly ILargeFileHunterService _fakeHunterService;

	public ScanControllerMultiDriveTests()
	{
		_fakeDriveService = new FakeDriveDetectionService();
		_fakeHunterService = new FakeLargeFileHunter();

		// Properly injects nulls for unused dependencies
		_controller = new StorageController(null, null, null);
	}

	[Fact]
	public async Task GetLargeFiles_WithValidMountedDrive_ReturnsOk()
	{
		// Arrange
		// Uses a directory we absolutely know exists on your physical motherboard
		string requestedRoot = Environment.SystemDirectory;

		// Act
		var result = await _controller.GetLargeFiles(
			_fakeHunterService,
			_fakeDriveService,
			CancellationToken.None,
			requestedRoot);

		// Assert - Expecting total success
		var okResult = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<ApiResponse<IEnumerable<LargeFile>>>(okResult.Value);
		Assert.True(response.Success);
	}

	[Fact]
	public async Task GetLargeFiles_WithUnpluggedGhostDrive_ReturnsBadRequest()
	{
		// Arrange
		string requestedRoot = "Z:\\"; // Z almost certainly doesn't exist

		// Act
		var result = await _controller.GetLargeFiles(
			_fakeHunterService,
			_fakeDriveService,
			CancellationToken.None,
			requestedRoot);

		// Assert - Expecting the hardware gatekeeper to block it
		var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
		var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);

		Assert.False(response.Success);
		Assert.Contains("Drive not ready", response.Message);
	}

	[Fact]
	public async Task GetLargeFiles_WithEmptyRoot_DefaultsToSystemDriveAndValidates()
	{
		// Arrange
		string requestedRoot = "";

		// Act
		var result = await _controller.GetLargeFiles(
			_fakeHunterService,
			_fakeDriveService,
			CancellationToken.None,
			requestedRoot);

		// Assert - Expecting total success because the controller will cleanly fall back to C:\
		var okResult = Assert.IsType<OkObjectResult>(result);
		var response = Assert.IsType<ApiResponse<IEnumerable<LargeFile>>>(okResult.Value);
		Assert.True(response.Success);
	}
}

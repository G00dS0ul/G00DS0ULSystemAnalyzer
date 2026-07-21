using System;
using System.Collections.Generic;
using System.IO;
using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Controller
{
	public class FileExtensionBreakdownControllerTest : IDisposable
	{
		private readonly Mock<IFileTypeScanner> _scanner;
		private readonly StorageController _controller;
		private readonly string _existingRoot;

		public FileExtensionBreakdownControllerTest()
		{
			_scanner = new Mock<IFileTypeScanner>();

			var diskService = new Mock<IDiskOperationService>().Object;
			var duplicateDetector = new Mock<IDuplicateFileDetector>().Object;
			var scopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>().Object;
			_controller = new StorageController(diskService, duplicateDetector, scopeFactory);

			_existingRoot = Path.Combine(Path.GetTempPath(), "gsa_ctrltest_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_existingRoot);
		}

		[Fact]
		public void GetExtensions_WhenRootMissing_ReturnsBadRequest_RootRequired()
		{
			var result = _controller.GetExtensions("", _scanner.Object);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("ROOT_REQUIRED", GetErrorCode(badRequest.Value));
			// Guard short-circuits before the service is consulted.
			_scanner.Verify(s => s.GetExtensionBreakdown(It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public void GetExtensions_WhenDirectoryDoesNotExist_ReturnsBadRequest_DriveNotFound()
		{
			var ghostPath = Path.Combine(Path.GetTempPath(), "gsa_missing_" + Guid.NewGuid().ToString("N"));

			var result = _controller.GetExtensions(ghostPath, _scanner.Object);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("DRIVE_NOT_FOUND", GetErrorCode(badRequest.Value));
			_scanner.Verify(s => s.GetExtensionBreakdown(It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public void GetExtensions_WhenNoScanCached_ReturnsConflict_NoScanCached()
		{
			_scanner.Setup(s => s.GetExtensionBreakdown(_existingRoot))
					.Returns((ExtensionBreakdownResult?)null);

			var result = _controller.GetExtensions(_existingRoot, _scanner.Object);

			var conflict = Assert.IsType<ConflictObjectResult>(result);
			Assert.Equal("NO_SCAN_CACHED", GetErrorCode(conflict.Value));
		}

		[Fact]
		public void GetExtensions_WhenScanCached_ReturnsOkWithBreakdown()
		{
			var expected = new ExtensionBreakdownResult
			{
				Root = _existingRoot,
				Extensions = new List<ExtensionBreakdownItem>
				{
					new ExtensionBreakdownItem { Ext = ".cs", Category = "code", FileCount = 3, TotalBytes = 4096 }
				}
			};
			_scanner.Setup(s => s.GetExtensionBreakdown(_existingRoot)).Returns(expected);

			var result = _controller.GetExtensions(_existingRoot, _scanner.Object);

			var ok = Assert.IsType<OkObjectResult>(result);
			Assert.Same(expected, ok.Value);
		}

		// The endpoint returns anonymous objects ({ error, message }); read 'error' reflectively.
		private static string? GetErrorCode(object? value) =>
			value?.GetType().GetProperty("error")?.GetValue(value)?.ToString();

		public void Dispose()
		{
			try { if (Directory.Exists(_existingRoot)) Directory.Delete(_existingRoot, true); }
			catch { /* best-effort cleanup */ }
		}
	}
}

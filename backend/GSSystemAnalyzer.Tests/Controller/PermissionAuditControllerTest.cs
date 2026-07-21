using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Controller
{
	public class PermissionAuditControllerTest : IDisposable
	{
		private readonly Mock<IPermissionAuditService> _auditService;
		private readonly AuditController _controller;
		private readonly string _existingRoot;

		public PermissionAuditControllerTest()
		{
			_auditService = new Mock<IPermissionAuditService>();
			_controller = new AuditController(_auditService.Object);

			_existingRoot = Path.Combine(Path.GetTempPath(), "gsa_audit_test_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_existingRoot);
		}

		[Fact]
		public async Task Post_WhenRootIsNull_ReturnsBadRequest_RootRequired()
		{
			var result = await _controller.AuditPermissions(
				new PermissionAuditRequest { Root = "" }, CancellationToken.None);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("ROOT_REQUIRED", GetErrorCode(badRequest.Value));
			_auditService.Verify(s => s.AuditAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Post_WhenRootIsWhitespace_ReturnsBadRequest_RootRequired()
		{
			var result = await _controller.AuditPermissions(
				new PermissionAuditRequest { Root = "   " }, CancellationToken.None);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("ROOT_REQUIRED", GetErrorCode(badRequest.Value));
		}

		[Fact]
		public async Task Post_WhenDirectoryDoesNotExist_ReturnsBadRequest_DirectoryNotFound()
		{
			var ghostPath = Path.Combine(Path.GetTempPath(), "gsa_missing_" + Guid.NewGuid().ToString("N"));

			var result = await _controller.AuditPermissions(
				new PermissionAuditRequest { Root = ghostPath }, CancellationToken.None);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("DIRECTORY_NOT_FOUND", GetErrorCode(badRequest.Value));
			_auditService.Verify(s => s.AuditAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Post_WhenValidRoot_ReturnsOkWithAuditResult()
		{
			var expected = new PermissionAuditResult
			{
				Root = _existingRoot,
				AuditedAt = DateTime.UtcNow,
				TotalScanned = 42,
				Issues = new List<PermissionIssue>
				{
					new PermissionIssue
					{
						Path = Path.Combine(_existingRoot, "setup.exe"),
						Severity = "high",
						Type = "executable_in_data_dir",
						Description = "Executable (.exe) found in Downloads directory"
					}
				}
			};

			_auditService.Setup(s => s.AuditAsync(_existingRoot, It.IsAny<CancellationToken>()))
						 .ReturnsAsync(expected);

			var result = await _controller.AuditPermissions(
				new PermissionAuditRequest { Root = _existingRoot }, CancellationToken.None);

			var ok = Assert.IsType<OkObjectResult>(result);
			var actual = Assert.IsType<PermissionAuditResult>(ok.Value);
			Assert.Equal(_existingRoot, actual.Root);
			Assert.Equal(42, actual.TotalScanned);
			Assert.Single(actual.Issues);
			Assert.Equal("high", actual.Issues[0].Severity);
		}

		[Fact]
		public async Task Post_WhenServiceThrows_Returns500()
		{
			_auditService.Setup(s => s.AuditAsync(_existingRoot, It.IsAny<CancellationToken>()))
						 .ThrowsAsync(new InvalidOperationException("Boom"));

			var result = await _controller.AuditPermissions(
				new PermissionAuditRequest { Root = _existingRoot }, CancellationToken.None);

			var statusResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(500, statusResult.StatusCode);
			Assert.Equal("AUDIT_FAILED", GetErrorCode(statusResult.Value));
		}

		[Fact]
		public async Task Post_WhenCancelled_Returns499()
		{
			_auditService.Setup(s => s.AuditAsync(_existingRoot, It.IsAny<CancellationToken>()))
						 .ThrowsAsync(new OperationCanceledException());

			var result = await _controller.AuditPermissions(
				new PermissionAuditRequest { Root = _existingRoot }, CancellationToken.None);

			var statusResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(499, statusResult.StatusCode);
			Assert.Equal("AUDIT_CANCELLED", GetErrorCode(statusResult.Value));
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

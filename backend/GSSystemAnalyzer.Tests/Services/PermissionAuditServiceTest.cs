using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services
{
	public class PermissionAuditServiceTest : IDisposable
	{
		private readonly string _testRoot;
		private readonly PermissionAuditService _service;

		public PermissionAuditServiceTest()
		{
			_testRoot = Path.Combine(Path.GetTempPath(), "gsa_audit_svc_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_testRoot);

			var settingsMock = new Mock<ISettingService>();
			settingsMock.Setup(s => s.Current).Returns(new AppSettingDto
			{
				Scan = new ScanSettingDto
				{
					Depth = 10,
					ExcludedPaths = new List<string>(),
					SkipHiddenFiles = false,
					SkipSystemFiles = false
				}
			});

			var loggerMock = new Mock<ILogger<PermissionAuditService>>();
			var hubMock = new Mock<IHubContext<SystemHub>>();
			var clientsMock = new Mock<IHubClients>();
			var clientProxyMock = new Mock<IClientProxy>();
			clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
			hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

			_service = new PermissionAuditService(settingsMock.Object, loggerMock.Object, hubMock.Object);
		}

		[Fact]
		public async Task Audit_EmptyDirectory_ReturnsZeroIssues()
		{
			var result = await _service.AuditAsync(_testRoot);

			Assert.Equal(_testRoot, result.Root);
			Assert.Equal(0, result.TotalScanned);
			Assert.Empty(result.Issues);
			Assert.True(result.AuditedAt <= DateTime.UtcNow);
		}

		[Fact]
		public async Task Audit_FlagsExecutableInDataDir()
		{
			// Create a .exe file inside a non-system directory
			var exePath = Path.Combine(_testRoot, "sneaky.exe");
			await File.WriteAllTextAsync(exePath, "MZ fake executable");

			var result = await _service.AuditAsync(_testRoot);

			Assert.True(result.TotalScanned >= 1);

			var exeIssue = result.Issues.FirstOrDefault(i =>
				i.Type == "executable_in_data_dir" && i.Path.Contains("sneaky.exe"));
			Assert.NotNull(exeIssue);
			Assert.Equal("high", exeIssue.Severity);
		}

		[Fact]
		public async Task Audit_FlagsDllInDataDir()
		{
			var dllPath = Path.Combine(_testRoot, "payload.dll");
			await File.WriteAllTextAsync(dllPath, "fake dll content");

			var result = await _service.AuditAsync(_testRoot);

			var dllIssue = result.Issues.FirstOrDefault(i =>
				i.Type == "executable_in_data_dir" && i.Path.Contains("payload.dll"));
			Assert.NotNull(dllIssue);
			Assert.Equal("high", dllIssue.Severity);
		}

		[Fact]
		public async Task Audit_DoesNotFlagSafeExtensions()
		{
			// Create a harmless .txt file — should never be flagged as executable_in_data_dir
			var txtPath = Path.Combine(_testRoot, "notes.txt");
			await File.WriteAllTextAsync(txtPath, "hello world");

			var result = await _service.AuditAsync(_testRoot);

			Assert.DoesNotContain(result.Issues, i =>
				i.Type == "executable_in_data_dir" && i.Path.Contains("notes.txt"));
		}

		[Fact]
		public async Task Audit_RespectsExcludedPaths()
		{
			// Create a subdirectory and put an .exe in it
			var subDir = Path.Combine(_testRoot, "excluded_zone");
			Directory.CreateDirectory(subDir);
			await File.WriteAllTextAsync(Path.Combine(subDir, "hidden.exe"), "MZ");

			// Rebuild service with this subdirectory excluded
			var settingsMock = new Mock<ISettingService>();
			settingsMock.Setup(s => s.Current).Returns(new AppSettingDto
			{
				Scan = new ScanSettingDto
				{
					Depth = 10,
					ExcludedPaths = new List<string> { subDir },
					SkipHiddenFiles = false,
					SkipSystemFiles = false
				}
			});
			var loggerMock = new Mock<ILogger<PermissionAuditService>>();
			var hubMock = new Mock<IHubContext<SystemHub>>();
			var clientsMock = new Mock<IHubClients>();
			clientsMock.Setup(c => c.All).Returns(new Mock<IClientProxy>().Object);
			hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);
			var service = new PermissionAuditService(settingsMock.Object, loggerMock.Object, hubMock.Object);

			var result = await service.AuditAsync(_testRoot);

			// The excluded .exe should not appear in issues
			Assert.DoesNotContain(result.Issues, i => i.Path.Contains("hidden.exe"));
		}

		[Fact]
		public async Task Audit_RespectsDepthLimit()
		{
			// Create a deeply nested .exe (depth = 3) but limit scan to depth 1
			var nested = Path.Combine(_testRoot, "a", "b", "c");
			Directory.CreateDirectory(nested);
			await File.WriteAllTextAsync(Path.Combine(nested, "deep.bat"), "@echo hi");

			var settingsMock = new Mock<ISettingService>();
			settingsMock.Setup(s => s.Current).Returns(new AppSettingDto
			{
				Scan = new ScanSettingDto
				{
					Depth = 1,
					ExcludedPaths = new List<string>(),
					SkipHiddenFiles = false,
					SkipSystemFiles = false
				}
			});
			var loggerMock = new Mock<ILogger<PermissionAuditService>>();
			var hubMock = new Mock<IHubContext<SystemHub>>();
			var clientsMock = new Mock<IHubClients>();
			clientsMock.Setup(c => c.All).Returns(new Mock<IClientProxy>().Object);
			hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);
			var service = new PermissionAuditService(settingsMock.Object, loggerMock.Object, hubMock.Object);

			var result = await service.AuditAsync(_testRoot);

			// deep.bat is at depth 3 — should NOT be flagged with depth limit 1
			Assert.DoesNotContain(result.Issues, i => i.Path.Contains("deep.bat"));
		}

		[Fact]
		public async Task Audit_SupportsCancellation()
		{
			// Create some files
			for (int i = 0; i < 5; i++)
				await File.WriteAllTextAsync(Path.Combine(_testRoot, $"file{i}.txt"), "data");

			using var cts = new CancellationTokenSource();
			cts.Cancel(); // Pre-cancel

			await Assert.ThrowsAnyAsync<OperationCanceledException>(
				() => _service.AuditAsync(_testRoot, cts.Token));
		}

		[Fact]
		public async Task Audit_ScansSubdirectories()
		{
			var subDir = Path.Combine(_testRoot, "subdir");
			Directory.CreateDirectory(subDir);
			await File.WriteAllTextAsync(Path.Combine(subDir, "nested.exe"), "MZ");

			var result = await _service.AuditAsync(_testRoot);

			var nestedIssue = result.Issues.FirstOrDefault(i =>
				i.Type == "executable_in_data_dir" && i.Path.Contains("nested.exe"));
			Assert.NotNull(nestedIssue);
		}

		public void Dispose()
		{
			try { if (Directory.Exists(_testRoot)) Directory.Delete(_testRoot, true); }
			catch { /* best-effort cleanup */ }
		}
	}
}

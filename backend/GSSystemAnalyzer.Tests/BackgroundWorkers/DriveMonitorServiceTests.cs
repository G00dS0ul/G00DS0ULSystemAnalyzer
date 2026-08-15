using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.BackgroundWorkers;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.BackgroundWorkers
{
	public class DriveMonitorServiceTests
	{
		private class FakeDriveDetectionService : IDriveDetectionService
		{
			public List<DriveMetric> Drives { get; set; } = new();

			public List<DriveMetric> GetReadyDrives() => Drives;
		}

		private class TestContext
		{
			public DriveMonitorService Service { get; set; } = null!;
			public FakeDriveDetectionService DriveService { get; set; } = null!;
			public Mock<IHubContext<SystemHub>> HubMock { get; set; } = null!;
			public Mock<IClientProxy> ClientProxyMock { get; set; } = null!;
			public Mock<ISettingService> SettingsMock { get; set; } = null!;
			public AppSettingDto AppSettings { get; set; } = null!;
			public DateTimeOffset CurrentTime { get; set; } = DateTimeOffset.UtcNow;
			public List<(string Method, object? Payload)> Broadcasts { get; set; } = new();

			public void AdvanceTime(TimeSpan duration)
			{
				CurrentTime = CurrentTime.Add(duration);
			}

			public void ChangeSettings(Action<AppSettingDto> mutate)
			{
				mutate(AppSettings);
				SettingsMock.Raise(s => s.OnSettingsChanged += null, SettingsMock.Object, AppSettings);
			}
		}

		private TestContext CreateContext(int diskThreshold = 90, TimeSpan? cooldown = null)
		{
			var driveService = new FakeDriveDetectionService();

			var hubMock = new Mock<IHubContext<SystemHub>>();
			var clientsMock = new Mock<IHubClients>();
			var clientProxyMock = new Mock<IClientProxy>();

			hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);
			clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

			var broadcasts = new List<(string Method, object? Payload)>();
			clientProxyMock
				.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
				.Callback<string, object?[], CancellationToken>((method, args, _) =>
				{
					broadcasts.Add((method, args?.FirstOrDefault()));
				})
				.Returns(Task.CompletedTask);

			var appSettings = new AppSettingDto
			{
				Alerts = new AlertSettingDto
				{
					DiskThresholdPercent = diskThreshold
				}
			};

			var settingsMock = new Mock<ISettingService>();
			settingsMock.Setup(s => s.Current).Returns(appSettings);

			var ctx = new TestContext
			{
				DriveService = driveService,
				HubMock = hubMock,
				ClientProxyMock = clientProxyMock,
				SettingsMock = settingsMock,
				AppSettings = appSettings,
				Broadcasts = broadcasts,
				CurrentTime = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero)
			};

			ctx.Service = new DriveMonitorService(
				driveService,
				hubMock.Object,
				settingsMock.Object,
				NullLogger<DriveMonitorService>.Instance,
				() => ctx.CurrentTime,
				cooldown ?? TimeSpan.FromMinutes(60));

			return ctx;
		}

		[Fact]
		public async Task InitialCrossing_AboveThreshold_EmitsDiskAlertWithWarning()
		{
			var ctx = CreateContext(diskThreshold: 90);
			ctx.DriveService.Drives = new List<DriveMetric>
			{
				new()
				{
					Name = @"C:\",
					Label = "Windows (C:)",
					Type = "Fixed",
					TotalBytes = 100_000_000_000,
					FreeBytes = 8_000_000_000,
					UsedBytes = 92_000_000_000,
					UsedPercent = 92.0,
					IsReady = true
				}
			};

			await ctx.Service.EvaluateDrivesAsync();

			Assert.Single(ctx.Broadcasts);
			Assert.Equal("DiskAlert", ctx.Broadcasts[0].Method);

			var payload = ctx.Broadcasts[0].Payload;
			Assert.NotNull(payload);

			dynamic p = payload;
			Assert.Equal(@"C:\", (string)p.driveName);
			Assert.Equal("Windows (C:)", (string)p.label);
			Assert.Equal("Fixed", (string)p.driveType);
			Assert.Equal(92.0, (double)p.usedPercent);
			Assert.Equal(90.0, (double)p.thresholdPercent);
			Assert.Equal("warning", (string)p.severity);
			Assert.NotNull((string)p.freeFormatted);
			Assert.NotNull((string)p.firstDetectedAt);

			Assert.True(ctx.Service.DriveStates[@"C:\"].IsAlerting);
			Assert.Equal(92.0, ctx.Service.DriveStates[@"C:\"].LastPercent);
		}

		[Fact]
		public async Task SubThreshold_DoesNotEmitAlert()
		{
			var ctx = CreateContext(diskThreshold: 90);
			ctx.DriveService.Drives = new List<DriveMetric>
			{
				new()
				{
					Name = @"C:\",
					Label = "System",
					Type = "Fixed",
					TotalBytes = 100_000_000_000,
					FreeBytes = 10_100_000_000,
					UsedBytes = 89_900_000_000,
					UsedPercent = 89.9,
					IsReady = true
				}
			};

			await ctx.Service.EvaluateDrivesAsync();

			Assert.Empty(ctx.Broadcasts);
			Assert.False(ctx.Service.DriveStates[@"C:\"].IsAlerting);
		}

		[Fact]
		public async Task Hysteresis_OscillationAroundThreshold_FiresAlertOnlyOnce()
		{
			var ctx = CreateContext(diskThreshold: 90);
			var drive = new DriveMetric
			{
				Name = @"C:\",
				Label = "System",
				Type = "Fixed",
				TotalBytes = 100_000_000_000,
				FreeBytes = 9_900_000_000,
				UsedBytes = 90_100_000_000,
				UsedPercent = 90.1,
				IsReady = true
			};
			ctx.DriveService.Drives = new List<DriveMetric> { drive };

			// Tick 1: Crosses threshold (90.1%) -> fires alert 1
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);
			Assert.Equal("DiskAlert", ctx.Broadcasts[0].Method);

			// Tick 2: Dips slightly to 89.9% (within 5-point hysteresis band, > 85%) -> no clear, no alert
			drive.UsedPercent = 89.9;
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);
			Assert.True(ctx.Service.DriveStates[@"C:\"].IsAlerting);

			// Tick 3: Climbs back to 90.1% -> still in alerting state, does NOT re-fire
			drive.UsedPercent = 90.1;
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);

			// Tick 4: Dips to 87.0% -> still within hysteresis band
			drive.UsedPercent = 87.0;
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);
			Assert.True(ctx.Service.DriveStates[@"C:\"].IsAlerting);
		}

		[Fact]
		public async Task Recovery_BelowHysteresisBand_EmitsDiskAlertClearedAndResetsState()
		{
			var ctx = CreateContext(diskThreshold: 90);
			var drive = new DriveMetric
			{
				Name = @"C:\",
				Label = "System",
				Type = "Fixed",
				TotalBytes = 100_000_000_000,
				FreeBytes = 8_000_000_000,
				UsedBytes = 92_000_000_000,
				UsedPercent = 92.0,
				IsReady = true
			};
			ctx.DriveService.Drives = new List<DriveMetric> { drive };

			// 1. Initial alert
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);
			Assert.Equal("DiskAlert", ctx.Broadcasts[0].Method);

			// 2. Drive clears down to 85.0% (threshold - 5.0 = 85.0%)
			drive.UsedPercent = 85.0;
			await ctx.Service.EvaluateDrivesAsync();

			Assert.Equal(2, ctx.Broadcasts.Count);
			Assert.Equal("DiskAlertCleared", ctx.Broadcasts[1].Method);
			Assert.False(ctx.Service.DriveStates[@"C:\"].IsAlerting);

			dynamic clearedPayload = ctx.Broadcasts[1].Payload!;
			Assert.Equal(@"C:\", (string)clearedPayload.driveName);
			Assert.Equal(85.0, (double)clearedPayload.usedPercent);

			// 3. Drive fills up again to 90.5% -> fires a fresh alert
			drive.UsedPercent = 90.5;
			await ctx.Service.EvaluateDrivesAsync();

			Assert.Equal(3, ctx.Broadcasts.Count);
			Assert.Equal("DiskAlert", ctx.Broadcasts[2].Method);
			Assert.True(ctx.Service.DriveStates[@"C:\"].IsAlerting);
		}

		[Fact]
		public async Task Cooldown_DriveHoldingAtSamePercent_DoesNotReFireAfter60Minutes()
		{
			var ctx = CreateContext(diskThreshold: 90);
			var drive = new DriveMetric
			{
				Name = @"C:\",
				Label = "System",
				Type = "Fixed",
				TotalBytes = 100_000_000_000,
				FreeBytes = 10_000_000_000,
				UsedBytes = 90_000_000_000,
				UsedPercent = 90.0,
				IsReady = true
			};
			ctx.DriveService.Drives = new List<DriveMetric> { drive };

			// Initial alert
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);

			// 75 minutes pass, usage is still 90.0%
			ctx.AdvanceTime(TimeSpan.FromMinutes(75));
			await ctx.Service.EvaluateDrivesAsync();

			// Should NOT re-fire because usage did not climb +5 points
			Assert.Single(ctx.Broadcasts);
		}

		[Fact]
		public async Task Cooldown_DriveClimbingLessThan5Points_DoesNotReFireAfter60Minutes()
		{
			var ctx = CreateContext(diskThreshold: 90);
			var drive = new DriveMetric
			{
				Name = @"C:\",
				Label = "System",
				Type = "Fixed",
				TotalBytes = 100_000_000_000,
				FreeBytes = 10_000_000_000,
				UsedBytes = 90_000_000_000,
				UsedPercent = 90.0,
				IsReady = true
			};
			ctx.DriveService.Drives = new List<DriveMetric> { drive };

			// Initial alert at 90.0%
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);

			// 70 minutes pass, usage climbs to 93.5% (climb is only 3.5% < 5%)
			ctx.AdvanceTime(TimeSpan.FromMinutes(70));
			drive.UsedPercent = 93.5;
			await ctx.Service.EvaluateDrivesAsync();

			Assert.Single(ctx.Broadcasts);
		}

		[Fact]
		public async Task Cooldown_DriveClimbing5PointsOrMore_ReFiresAfter60Minutes()
		{
			var ctx = CreateContext(diskThreshold: 90);
			var drive = new DriveMetric
			{
				Name = @"C:\",
				Label = "System",
				Type = "Fixed",
				TotalBytes = 100_000_000_000,
				FreeBytes = 10_000_000_000,
				UsedBytes = 90_000_000_000,
				UsedPercent = 90.0,
				IsReady = true
			};
			ctx.DriveService.Drives = new List<DriveMetric> { drive };

			// 1. Initial alert at 90.0% at T=0
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);

			// 2. 30 minutes pass (cooldown not met), usage jumps to 95.0% -> does NOT re-fire yet
			ctx.AdvanceTime(TimeSpan.FromMinutes(30));
			drive.UsedPercent = 95.0;
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Single(ctx.Broadcasts);

			// 3. At T=65 minutes (cooldown met and usage >= 90 + 5 = 95%) -> DOES re-fire
			ctx.AdvanceTime(TimeSpan.FromMinutes(35));
			await ctx.Service.EvaluateDrivesAsync();

			Assert.Equal(2, ctx.Broadcasts.Count);
			Assert.Equal("DiskAlert", ctx.Broadcasts[1].Method);
			dynamic p = ctx.Broadcasts[1].Payload!;
			Assert.Equal(95.0, (double)p.usedPercent);

			Assert.Equal(95.0, ctx.Service.DriveStates[@"C:\"].LastPercent);
		}

		[Fact]
		public async Task CriticalSeverity_At98Percent_EmitsCriticalSeverity()
		{
			var ctx = CreateContext(diskThreshold: 90);
			ctx.DriveService.Drives = new List<DriveMetric>
			{
				new()
				{
					Name = @"C:\",
					Label = "System",
					Type = "Fixed",
					TotalBytes = 100_000_000_000,
					FreeBytes = 1_500_000_000,
					UsedBytes = 98_500_000_000,
					UsedPercent = 98.5,
					IsReady = true
				}
			};

			await ctx.Service.EvaluateDrivesAsync();

			Assert.Single(ctx.Broadcasts);
			dynamic p = ctx.Broadcasts[0].Payload!;
			Assert.Equal("critical", (string)p.severity);
		}

		[Fact]
		public async Task RemovableDrive_Uses95PercentThresholdByDefault()
		{
			var ctx = CreateContext(diskThreshold: 90);
			var usbDrive = new DriveMetric
			{
				Name = @"E:\",
				Label = "USB_STICK",
				Type = "Removable",
				TotalBytes = 32_000_000_000,
				FreeBytes = 2_500_000_000,
				UsedBytes = 29_500_000_000,
				UsedPercent = 92.2, // > 90% (fixed threshold), but < 95% (removable threshold)
				IsReady = true
			};
			ctx.DriveService.Drives = new List<DriveMetric> { usbDrive };

			// 1. At 92.2%, removable drive does NOT alert
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Empty(ctx.Broadcasts);

			// 2. At 95.5%, removable drive DOES alert
			usbDrive.UsedPercent = 95.5;
			await ctx.Service.EvaluateDrivesAsync();

			Assert.Single(ctx.Broadcasts);
			dynamic p = ctx.Broadcasts[0].Payload!;
			Assert.Equal(@"E:\", (string)p.driveName);
			Assert.Equal(95.0, (double)p.thresholdPercent);
			Assert.Equal("Removable", (string)p.driveType);
		}

		[Fact]
		public async Task PruneRemovedDrives_DropsStateFromMemory()
		{
			var ctx = CreateContext(diskThreshold: 90);
			ctx.DriveService.Drives = new List<DriveMetric>
			{
				new()
				{
					Name = @"D:\",
					Label = "External",
					Type = "Fixed",
					UsedPercent = 93.0,
					IsReady = true
				}
			};

			// Initial alert stamps D:
			await ctx.Service.EvaluateDrivesAsync();
			Assert.True(ctx.Service.DriveStates.ContainsKey(@"D:\"));

			// Drive D: unplugged
			ctx.DriveService.Drives = new List<DriveMetric>();
			await ctx.Service.EvaluateDrivesAsync();

			Assert.False(ctx.Service.DriveStates.ContainsKey(@"D:\"));
		}

		[Fact]
		public async Task SettingsChange_ImmediatelyEvaluatesAndTriggersAlert()
		{
			var ctx = CreateContext(diskThreshold: 90);
			ctx.DriveService.Drives = new List<DriveMetric>
			{
				new()
				{
					Name = @"C:\",
					Label = "System",
					Type = "Fixed",
					TotalBytes = 100_000_000_000,
					FreeBytes = 15_000_000_000,
					UsedBytes = 85_000_000_000,
					UsedPercent = 85.0,
					IsReady = true
				}
			};

			// Initially at 85% with 90% threshold -> no alert
			await ctx.Service.EvaluateDrivesAsync();
			Assert.Empty(ctx.Broadcasts);

			// User lowers threshold to 80%
			ctx.ChangeSettings(s => s.Alerts.DiskThresholdPercent = 80);

			// Evaluate drives
			await ctx.Service.EvaluateDrivesAsync();

			Assert.Single(ctx.Broadcasts);
			dynamic p = ctx.Broadcasts[0].Payload!;
			Assert.Equal(80.0, (double)p.thresholdPercent);
			Assert.Equal(85.0, (double)p.usedPercent);
		}
	}
}

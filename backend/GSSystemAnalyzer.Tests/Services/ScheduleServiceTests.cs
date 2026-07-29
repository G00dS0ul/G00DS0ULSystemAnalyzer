using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class ScheduleServiceTests : IDisposable
{
	private readonly string _tempDir;
	private readonly string _tempFile;

	public ScheduleServiceTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "GSAnalyzer_ServiceTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
		_tempFile = Path.Combine(_tempDir, "test_schedules.json");
	}

	public void Dispose()
	{
		if (Directory.Exists(_tempDir))
			Directory.Delete(_tempDir, recursive: true);
	}

	private ScheduleService CreateService(IScheduleStore? store = null)
	{
		store ??= new ScheduleStore(NullLogger<ScheduleStore>.Instance, _tempFile);

		var hubMock = new Mock<IHubContext<SystemHub>>();
		hubMock.Setup(h => h.Clients).Returns(Mock.Of<IHubClients>());
		hubMock.Setup(h => h.Clients.All).Returns(Mock.Of<IClientProxy>());

		return new ScheduleService(
			store,
			hubMock.Object,
			NullLogger<ScheduleService>.Instance);
	}

	[Fact]
	public void Create_IntervalSchedule_SetsNextRun()
	{
		var service = CreateService();

		var before = DateTimeOffset.UtcNow;
		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 30,
			Enabled = true
		});

		Assert.NotNull(schedule);
		Assert.Equal(ScheduleKind.Interval, schedule.Kind);
		Assert.Equal(30, schedule.IntervalMinutes);
		Assert.NotNull(schedule.NextRun);
		// NextRun should be ~30 minutes from now
		Assert.True(schedule.NextRun.Value >= before.AddMinutes(29));
		Assert.True(schedule.NextRun.Value <= before.AddMinutes(31));
	}

	[Fact]
	public void Create_CronSchedule_SetsNextRun()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Cron,
			Cron = "0 3 * * *", // Daily at 03:00
			Enabled = true
		});

		Assert.NotNull(schedule);
		Assert.Equal(ScheduleKind.Cron, schedule.Kind);
		Assert.Equal("0 3 * * *", schedule.Cron);
		Assert.NotNull(schedule.NextRun);
		// NextRun should be in the future
		Assert.True(schedule.NextRun.Value > DateTimeOffset.UtcNow);
	}

	[Fact]
	public void Create_InvalidCron_ThrowsArgumentException()
	{
		var service = CreateService();

		var ex = Assert.Throws<ArgumentException>(() =>
			service.Create(new CreateScheduleRequest
			{
				Type = ScanType.Directory,
				Path = "C:/",
				Kind = ScheduleKind.Cron,
				Cron = "this is not a cron expression"
			}));

		Assert.Contains("Invalid cron expression", ex.Message);
	}

	[Fact]
	public void Create_MissingCron_ThrowsArgumentException()
	{
		var service = CreateService();

		var ex = Assert.Throws<ArgumentException>(() =>
			service.Create(new CreateScheduleRequest
			{
				Type = ScanType.Directory,
				Path = "C:/",
				Kind = ScheduleKind.Cron,
				Cron = null
			}));

		Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Create_InvalidInterval_Zero_ThrowsArgumentException()
	{
		var service = CreateService();

		var ex = Assert.Throws<ArgumentException>(() =>
			service.Create(new CreateScheduleRequest
			{
				Type = ScanType.Directory,
				Path = "C:/",
				Kind = ScheduleKind.Interval,
				IntervalMinutes = 0
			}));

		Assert.Contains("between 1 and 1440", ex.Message);
	}

	[Fact]
	public void Create_InvalidInterval_TooLarge_ThrowsArgumentException()
	{
		var service = CreateService();

		var ex = Assert.Throws<ArgumentException>(() =>
			service.Create(new CreateScheduleRequest
			{
				Type = ScanType.Directory,
				Path = "C:/",
				Kind = ScheduleKind.Interval,
				IntervalMinutes = 1441
			}));

		Assert.Contains("between 1 and 1440", ex.Message);
	}

	[Fact]
	public void Create_MissingInterval_ThrowsArgumentException()
	{
		var service = CreateService();

		var ex = Assert.Throws<ArgumentException>(() =>
			service.Create(new CreateScheduleRequest
			{
				Type = ScanType.Directory,
				Path = "C:/",
				Kind = ScheduleKind.Interval,
				IntervalMinutes = null
			}));

		Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void GetDueSchedules_ReturnsOnlyDue()
	{
		var service = CreateService();

		// Create one that's due (interval = 1 minute, last run was 2 minutes ago)
		var s1 = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 1,
			Enabled = true
		});

		// Manually mark it completed 2 minutes ago so NextRun is 1 minute ago
		service.MarkCompleted(s1.Id, DateTimeOffset.UtcNow.AddMinutes(-2));

		// Create one that's NOT due (interval = 60 minutes from now)
		service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "D:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 60,
			Enabled = true
		});

		var due = service.GetDueSchedules(DateTimeOffset.UtcNow);

		Assert.Single(due);
		Assert.Equal(s1.Id, due[0].Id);
	}

	[Fact]
	public void GetDueSchedules_SkipsDisabled()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 1,
			Enabled = false
		});

		// Even if NextRun is past, disabled schedules are never due
		service.MarkCompleted(schedule.Id, DateTimeOffset.UtcNow.AddMinutes(-5));

		var due = service.GetDueSchedules(DateTimeOffset.UtcNow);
		Assert.Empty(due);
	}

	[Fact]
	public void MarkCompleted_UpdatesLastRunAndNextRun()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 15,
			Enabled = true
		});

		var completedAt = DateTimeOffset.UtcNow;
		service.MarkCompleted(schedule.Id, completedAt);

		var updated = service.GetById(schedule.Id);
		Assert.NotNull(updated);
		Assert.Equal(completedAt, updated!.LastRun);
		Assert.NotNull(updated.NextRun);
		// NextRun should be ~15 minutes from completedAt
		var expectedNext = completedAt.AddMinutes(15);
		Assert.True(Math.Abs((updated.NextRun!.Value - expectedNext).TotalSeconds) < 2);
	}

	[Fact]
	public void Delete_RemovesSchedule()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 10,
			Enabled = true
		});

		var deleted = service.Delete(schedule.Id);
		Assert.True(deleted);
		Assert.Null(service.GetById(schedule.Id));
		Assert.Empty(service.GetAll());
	}

	[Fact]
	public void Delete_NonExistentId_ReturnsFalse()
	{
		var service = CreateService();

		var deleted = service.Delete(Guid.NewGuid());
		Assert.False(deleted);
	}

	[Fact]
	public void PersistenceRoundtrip_SurvivesNewServiceInstance()
	{
		var store = new ScheduleStore(NullLogger<ScheduleStore>.Instance, _tempFile);
		var service1 = CreateService(store);

		var schedule = service1.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 45,
			Enabled = true
		});

		// Create a new service instance pointing to the same store
		var service2 = CreateService(store);
		var loaded = service2.GetAll();

		Assert.Single(loaded);
		Assert.Equal(schedule.Id, loaded[0].Id);
		Assert.Equal(45, loaded[0].IntervalMinutes);
	}

	[Fact]
	public void Update_ChangesInterval_RecomputesNextRun()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 15,
			Enabled = true
		});

		var originalNextRun = schedule.NextRun;

		var updated = service.Update(schedule.Id, new UpdateScheduleRequest
		{
			IntervalMinutes = 60
		});

		Assert.NotNull(updated);
		Assert.Equal(60, updated!.IntervalMinutes);
		// NextRun should have changed (now 60 minutes out instead of 15)
		Assert.NotEqual(originalNextRun, updated.NextRun);
	}

	[Fact]
	public void Update_DisablesSchedule()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = "C:/",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 15,
			Enabled = true
		});

		var updated = service.Update(schedule.Id, new UpdateScheduleRequest
		{
			Enabled = false
		});

		Assert.NotNull(updated);
		Assert.False(updated!.Enabled);
	}

	[Fact]
	public void Update_NonExistentId_ReturnsNull()
	{
		var service = CreateService();

		var result = service.Update(Guid.NewGuid(), new UpdateScheduleRequest { Enabled = false });
		Assert.Null(result);
	}

	[Fact]
	public void Create_NormalizesBackslashPaths()
	{
		var service = CreateService();

		var schedule = service.Create(new CreateScheduleRequest
		{
			Type = ScanType.Directory,
			Path = @"C:\Users\test",
			Kind = ScheduleKind.Interval,
			IntervalMinutes = 10,
			Enabled = true
		});

		Assert.Equal("C:/Users/test", schedule.Path);
	}
}

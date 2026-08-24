using System;
using System.Linq;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Models.SettingDtos;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services
{
    public class WatcherEventLogServiceTests
    {
        private readonly Mock<ISettingService> _settingsMock;
        private readonly Mock<IHubContext<SystemHub>> _hubContextMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly Mock<IHubClients> _hubClientsMock;

        public WatcherEventLogServiceTests()
        {
            _settingsMock = new Mock<ISettingService>();
            
            var settings = new AppSettingDto();
            settings.Scan.ExcludedPaths.Add(@"C:\ExcludedFolder");
            
            _settingsMock.Setup(s => s.Current).Returns(settings);
            _settingsMock.Setup(s => s.AppDataFolder).Returns(@"C:\AppData\GSAnalyzer");

            _clientProxyMock = new Mock<IClientProxy>();
            _hubClientsMock = new Mock<IHubClients>();
            _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
            
            _hubContextMock = new Mock<IHubContext<SystemHub>>();
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        }

        private WatcherEventLogService CreateService()
        {
            return new WatcherEventLogService(_settingsMock.Object, _hubContextMock.Object, NullLogger<WatcherEventLogService>.Instance);
        }

        [Fact]
        public void LogEvent_AddsEventToQueue()
        {
            var service = CreateService();
            service.LogEvent(DateTimeOffset.UtcNow, WatcherChangeKind.Created, @"C:\Test\file.txt", null, false);
            
            var events = service.GetEvents(10).ToList();
            Assert.Single(events);
            Assert.Equal("C:/Test/file.txt", events[0].Path);
            Assert.Equal(WatcherChangeKind.Created, events[0].Kind);
        }

        [Fact]
        public void LogEvent_DeduplicatesRepeatedEventsWithinWindow()
        {
            var service = CreateService();
            var time = DateTimeOffset.UtcNow;
            
            service.LogEvent(time, WatcherChangeKind.Modified, @"C:\Test\file.txt", null, false);
            service.LogEvent(time.AddMilliseconds(100), WatcherChangeKind.Modified, @"C:\Test\file.txt", null, false);
            service.LogEvent(time.AddMilliseconds(200), WatcherChangeKind.Modified, @"C:\Test\file.txt", null, false);
            
            var events = service.GetEvents(10).ToList();
            Assert.Single(events);
            Assert.Equal(3, events[0].Occurrences);
        }

        [Fact]
        public void LogEvent_DoesNotDeduplicateIfOutsideWindow()
        {
            var service = CreateService();
            var time = DateTimeOffset.UtcNow;
            
            service.LogEvent(time, WatcherChangeKind.Modified, @"C:\Test\file.txt", null, false);
            service.LogEvent(time.AddMilliseconds(600), WatcherChangeKind.Modified, @"C:\Test\file.txt", null, false);
            
            var events = service.GetEvents(10).ToList();
            Assert.Equal(2, events.Count);
            Assert.Equal(1, events[0].Occurrences);
            Assert.Equal(1, events[1].Occurrences);
        }

        [Fact]
        public void LogEvent_FiltersAppDataFolder()
        {
            var service = CreateService();
            service.LogEvent(DateTimeOffset.UtcNow, WatcherChangeKind.Created, @"C:\AppData\GSAnalyzer\appsettings.user.json", null, false);
            
            var events = service.GetEvents(10).ToList();
            Assert.Empty(events);
        }

        [Fact]
        public void LogEvent_FiltersExcludedPaths()
        {
            var service = CreateService();
            service.LogEvent(DateTimeOffset.UtcNow, WatcherChangeKind.Created, @"C:\ExcludedFolder\somefile.txt", null, false);
            
            var events = service.GetEvents(10).ToList();
            Assert.Empty(events);
        }

        [Fact]
        public void LogOverflow_AddsOverflowEvent()
        {
            var service = CreateService();
            service.LogOverflow(@"C:\TestFolder");
            
            var events = service.GetEvents(10).ToList();
            Assert.Single(events);
            Assert.Equal(WatcherChangeKind.Overflow, events[0].Kind);
            Assert.Equal("C:/TestFolder", events[0].Path);
        }

        [Fact]
        public void LogEvent_MaxEntries_ThrowsOutOldEvents()
        {
            var service = CreateService();
            var baseTime = DateTimeOffset.UtcNow;
            
            for (int i = 0; i < 600; i++)
            {
                service.LogEvent(baseTime.AddSeconds(i), WatcherChangeKind.Created, $@"C:\Test\file{i}.txt", null, false);
            }
            
            var events = service.GetEvents(1000).ToList();
            Assert.Equal(500, events.Count);
            // Since newer events are last (and reverse is applied in GetEvents usually to get most recent first)
            // But we didn't specify order requirement. We just want 500 max.
        }
    }
}

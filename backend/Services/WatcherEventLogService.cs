using System.Collections.Concurrent;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services
{
    public class WatcherEventLogService : IWatcherEventLogService
    {
        private readonly ConcurrentQueue<WatcherEvent> _log = new();
        private const int MaxEntries = 500;
        private readonly TimeSpan _dedupeWindow = TimeSpan.FromMilliseconds(500);

        private readonly ISettingService _settings;
        private readonly IHubContext<SystemHub> _hub;
        private readonly ILogger<WatcherEventLogService> _logger;
        
        // State for deduplication
        private readonly object _lock = new();
        private WatcherEvent? _lastEvent;

        public WatcherEventLogService(
            ISettingService settings,
            IHubContext<SystemHub> hub,
            ILogger<WatcherEventLogService> logger)
        {
            _settings = settings;
            _hub = hub;
            _logger = logger;
        }

        public void LogEvent(DateTimeOffset timestamp, WatcherChangeKind kind, string path, string? oldPath, bool isDirectory)
        {
            var normalizedPath = Path.GetFullPath(path).Replace("\\", "/");
            var normalizedOldPath = oldPath != null ? Path.GetFullPath(oldPath).Replace("\\", "/") : null;

            if (ShouldExclude(normalizedPath)) return;
            if (normalizedOldPath != null && ShouldExclude(normalizedOldPath)) return;

            WatcherEvent? eventToEmit = null;
            bool isNewEvent = false;

            lock (_lock)
            {
                if (_lastEvent != null && 
                    _lastEvent.Kind == kind && 
                    _lastEvent.Path.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) && 
                    (timestamp - _lastEvent.Timestamp) <= _dedupeWindow)
                {
                    // Deduplicate in-place
                    _lastEvent.Occurrences++;
                    eventToEmit = _lastEvent;
                }
                else
                {
                    var newEvent = new WatcherEvent(timestamp, kind, normalizedPath, normalizedOldPath, isDirectory, 1);
                    _lastEvent = newEvent;
                    eventToEmit = newEvent;
                    isNewEvent = true;
                }
            }

            if (eventToEmit != null)
            {
                if (isNewEvent)
                {
                    Append(eventToEmit);
                }
                
                // Fire and forget SignalR
                _ = _hub.Clients.All.SendAsync("WatcherEventLogged", eventToEmit);
            }
        }

        public void LogOverflow(string path)
        {
            var normalizedPath = Path.GetFullPath(path).Replace("\\", "/");
            var overflowEvent = new WatcherEvent(DateTimeOffset.UtcNow, WatcherChangeKind.Overflow, normalizedPath, null, true, 1);
            
            lock (_lock)
            {
                _lastEvent = overflowEvent;
            }

            Append(overflowEvent);
            _ = _hub.Clients.All.SendAsync("WatcherEventLogged", overflowEvent);
        }

        private void Append(WatcherEvent e)
        {
            _log.Enqueue(e);
            while (_log.Count > MaxEntries) _log.TryDequeue(out _);
        }

        public IEnumerable<WatcherEvent> GetEvents(int limit, WatcherChangeKind? kindFilter = null)
        {
            var query = _log.AsEnumerable();
            
            if (kindFilter.HasValue)
            {
                query = query.Where(e => e.Kind == kindFilter.Value);
            }

            return query.Reverse().Take(limit).Reverse();
        }

        public void Clear()
        {
            _log.Clear();
            lock (_lock)
            {
                _lastEvent = null;
            }
        }

        private bool ShouldExclude(string path)
        {
            var appData = _settings.AppDataFolder.Replace("\\", "/");
            if (path.StartsWith(appData, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var excludedPaths = _settings.Current.Scan.ExcludedPaths;
            foreach (var excludedPath in excludedPaths)
            {
                var normalizedExcluded = excludedPath.Replace("\\", "/");
                if (path.StartsWith(normalizedExcluded, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

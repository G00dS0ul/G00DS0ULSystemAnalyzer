using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
    public interface IWatcherEventLogService
    {
        void LogEvent(DateTimeOffset timestamp, WatcherChangeKind kind, string path, string? oldPath, bool isDirectory);
        void LogOverflow(string path);
        IEnumerable<WatcherEvent> GetEvents(int limit, WatcherChangeKind? kindFilter = null);
        void Clear();
    }
}

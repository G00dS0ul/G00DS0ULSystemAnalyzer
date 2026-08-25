namespace GSSystemAnalyzer.Models
{
    public enum WatcherChangeKind
    {
        Created,
        Modified,
        Deleted,
        Renamed,
        Overflow
    }

    public record WatcherEvent
    {
        public WatcherEvent(DateTimeOffset timestamp, WatcherChangeKind kind, string path, string? oldPath, bool isDirectory, int occurrences)
        {
            Timestamp = timestamp;
            Kind = kind;
            Path = path;
            OldPath = oldPath;
            IsDirectory = isDirectory;
            Occurrences = occurrences;
        }

        public DateTimeOffset Timestamp { get; init; }
        public WatcherChangeKind Kind { get; init; }
        public string Path { get; init; }
        public string? OldPath { get; init; }
        public bool IsDirectory { get; init; }
        public int Occurrences { get; set; }
    }
}

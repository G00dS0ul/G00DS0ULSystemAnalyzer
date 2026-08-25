enum WatcherChangeKind {
  created,
  modified,
  deleted,
  renamed,
  overflow;

  static WatcherChangeKind fromInt(int value) {
    if (value >= 0 && value < values.length) {
      return values[value];
    }
    return modified;
  }
}

class WatcherEvent {
  final DateTime timestamp;
  final WatcherChangeKind kind;
  final String path;
  final String? oldPath;
  final bool isDirectory;
  final int occurrences;

  WatcherEvent({
    required this.timestamp,
    required this.kind,
    required this.path,
    this.oldPath,
    required this.isDirectory,
    required this.occurrences,
  });

  factory WatcherEvent.fromJson(Map<String, dynamic> json) {
    // If backend serializes as int or string, we should handle both securely.
    WatcherChangeKind kind;
    if (json['kind'] is int) {
      kind = WatcherChangeKind.fromInt(json['kind']);
    } else {
      kind = WatcherChangeKind.values.firstWhere(
        (e) => e.name.toLowerCase() == json['kind'].toString().toLowerCase(),
        orElse: () => WatcherChangeKind.modified,
      );
    }

    return WatcherEvent(
      timestamp: DateTime.parse(json['timestamp']),
      kind: kind,
      path: json['path'],
      oldPath: json['oldPath'],
      isDirectory: json['isDirectory'] ?? false,
      occurrences: json['occurrences'] ?? 1,
    );
  }
}

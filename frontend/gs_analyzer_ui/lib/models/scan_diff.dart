/// Directory-level diff of the latest scan of a root against its stored baseline.
class ScanDiff {
  final String root;
  final bool hasBaseline;
  final DateTime? baselineScannedAt;
  final DateTime currentScannedAt;
  final List<ScanDiffEntry> added;
  final List<ScanDiffEntry> removed;
  final List<ScanDiffEntry> grown;
  final List<ScanDiffEntry> shrunk;
  final ScanDiffSummary summary;

  const ScanDiff({
    required this.root,
    required this.hasBaseline,
    required this.baselineScannedAt,
    required this.currentScannedAt,
    required this.added,
    required this.removed,
    required this.grown,
    required this.shrunk,
    required this.summary,
  });

  /// True when the last scan produced no add/remove/grow/shrink changes.
  bool get isEmpty =>
      added.isEmpty && removed.isEmpty && grown.isEmpty && shrunk.isEmpty;

  static List<ScanDiffEntry> _entries(dynamic raw) => raw == null
      ? const []
      : (raw as List)
            .map((e) => ScanDiffEntry.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();

  static DateTime? _dateOrNull(dynamic v) =>
      v == null ? null : DateTime.parse(v as String);

  factory ScanDiff.fromJson(Map<String, dynamic> j) => ScanDiff(
    root: (j['root'] ?? j['Root'] ?? '') as String,
    hasBaseline: (j['hasBaseline'] ?? j['HasBaseline'] ?? false) as bool,
    baselineScannedAt: _dateOrNull(j['baselineScannedAt'] ?? j['BaselineScannedAt']),
    currentScannedAt:
        _dateOrNull(j['currentScannedAt'] ?? j['CurrentScannedAt']) ??
        DateTime.now(),
    added: _entries(j['added'] ?? j['Added']),
    removed: _entries(j['removed'] ?? j['Removed']),
    grown: _entries(j['grown'] ?? j['Grown']),
    shrunk: _entries(j['shrunk'] ?? j['Shrunk']),
    summary: ScanDiffSummary.fromJson(
      Map<String, dynamic>.from((j['summary'] ?? j['Summary']) as Map),
    ),
  );
}

/// One changed path in a scan diff. `deltaBytes` is signed (current - previous).
class ScanDiffEntry {
  final String path;
  final bool isDirectory;
  final int currentBytes;
  final int previousBytes;
  final int deltaBytes;

  /// Number of collapsed descendants for a wholly added/removed directory; null otherwise.
  final int? childCount;
  final DateTime? lastModified;

  const ScanDiffEntry({
    required this.path,
    required this.isDirectory,
    required this.currentBytes,
    required this.previousBytes,
    required this.deltaBytes,
    required this.childCount,
    required this.lastModified,
  });

  /// The parent directory of this entry, used to jump into the explorer.
  String get parentPath {
    final normalized = path.replaceAll('\\', '/');
    final idx = normalized.lastIndexOf('/');
    return idx <= 0 ? path : normalized.substring(0, idx);
  }

  /// The trailing segment (file/folder name) for display.
  String get displayName {
    final normalized = path.replaceAll('\\', '/');
    final trimmed = normalized.endsWith('/')
        ? normalized.substring(0, normalized.length - 1)
        : normalized;
    final idx = trimmed.lastIndexOf('/');
    return idx < 0 ? trimmed : trimmed.substring(idx + 1);
  }

  static int _int(dynamic v) => (v as num?)?.toInt() ?? 0;

  factory ScanDiffEntry.fromJson(Map<String, dynamic> j) => ScanDiffEntry(
    path: (j['path'] ?? j['Path'] ?? '') as String,
    isDirectory: (j['isDirectory'] ?? j['IsDirectory'] ?? false) as bool,
    currentBytes: _int(j['currentBytes'] ?? j['CurrentBytes']),
    previousBytes: _int(j['previousBytes'] ?? j['PreviousBytes']),
    deltaBytes: _int(j['deltaBytes'] ?? j['DeltaBytes']),
    childCount: (j['childCount'] ?? j['ChildCount']) == null
        ? null
        : _int(j['childCount'] ?? j['ChildCount']),
    lastModified: (j['lastModified'] ?? j['LastModified']) == null
        ? null
        : DateTime.parse((j['lastModified'] ?? j['LastModified']) as String),
  );
}

/// Aggregate counts and byte totals across all four change buckets.
class ScanDiffSummary {
  final int addedCount;
  final int addedBytes;
  final int removedCount;
  final int removedBytes;
  final int grownCount;
  final int grownDeltaBytes;
  final int shrunkCount;
  final int shrunkDeltaBytes;
  final int netDeltaBytes;

  const ScanDiffSummary({
    required this.addedCount,
    required this.addedBytes,
    required this.removedCount,
    required this.removedBytes,
    required this.grownCount,
    required this.grownDeltaBytes,
    required this.shrunkCount,
    required this.shrunkDeltaBytes,
    required this.netDeltaBytes,
  });

  int get totalChangeCount =>
      addedCount + removedCount + grownCount + shrunkCount;

  static int _int(dynamic v) => (v as num?)?.toInt() ?? 0;

  factory ScanDiffSummary.fromJson(Map<String, dynamic> j) => ScanDiffSummary(
    addedCount: _int(j['addedCount'] ?? j['AddedCount']),
    addedBytes: _int(j['addedBytes'] ?? j['AddedBytes']),
    removedCount: _int(j['removedCount'] ?? j['RemovedCount']),
    removedBytes: _int(j['removedBytes'] ?? j['RemovedBytes']),
    grownCount: _int(j['grownCount'] ?? j['GrownCount']),
    grownDeltaBytes: _int(j['grownDeltaBytes'] ?? j['GrownDeltaBytes']),
    shrunkCount: _int(j['shrunkCount'] ?? j['ShrunkCount']),
    shrunkDeltaBytes: _int(j['shrunkDeltaBytes'] ?? j['ShrunkDeltaBytes']),
    netDeltaBytes: _int(j['netDeltaBytes'] ?? j['NetDeltaBytes']),
  );
}

enum DiffKind { added, removed, grown, shrunk }

class ScanDiffEntry {
  final String path;
  final bool isDirectory;
  final DiffKind kind;
  final int currentBytes;
  final int previousBytes;
  final int deltaBytes;
  final int? childCount;
  final DateTime? lastModified;

  ScanDiffEntry({
    required this.path,
    required this.isDirectory,
    required this.kind,
    required this.currentBytes,
    required this.previousBytes,
    required this.deltaBytes,
    this.childCount,
    this.lastModified,
  });

  factory ScanDiffEntry.fromJson(Map<String, dynamic> json) {
    DiffKind parseKind(String? kindStr) {
      switch (kindStr?.toLowerCase()) {
        case 'added':
          return DiffKind.added;
        case 'removed':
          return DiffKind.removed;
        case 'grown':
          return DiffKind.grown;
        case 'shrunk':
          return DiffKind.shrunk;
        default:
          return DiffKind.added;
      }
    }

    return ScanDiffEntry(
      path: json['path'] ?? '',
      isDirectory: json['isDirectory'] ?? false,
      kind: parseKind(json['kind']),
      currentBytes: json['currentBytes'] ?? 0,
      previousBytes: json['previousBytes'] ?? 0,
      deltaBytes: json['deltaBytes'] ?? 0,
      childCount: json['childCount'],
      lastModified: json['lastModified'] != null
          ? DateTime.tryParse(json['lastModified'])
          : null,
    );
  }
}

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

  ScanDiffSummary({
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

  factory ScanDiffSummary.fromJson(Map<String, dynamic> json) {
    return ScanDiffSummary(
      addedCount: json['addedCount'] ?? 0,
      addedBytes: json['addedBytes'] ?? 0,
      removedCount: json['removedCount'] ?? 0,
      removedBytes: json['removedBytes'] ?? 0,
      grownCount: json['grownCount'] ?? 0,
      grownDeltaBytes: json['grownDeltaBytes'] ?? 0,
      shrunkCount: json['shrunkCount'] ?? 0,
      shrunkDeltaBytes: json['shrunkDeltaBytes'] ?? 0,
      netDeltaBytes: json['netDeltaBytes'] ?? 0,
    );
  }
}

class ScanDiff {
  final String root;
  final bool hasBaseline;
  final DateTime? baselineScannedAt;
  final DateTime? currentScannedAt;
  final List<ScanDiffEntry> added;
  final List<ScanDiffEntry> removed;
  final List<ScanDiffEntry> grown;
  final List<ScanDiffEntry> shrunk;
  final ScanDiffSummary summary;

  ScanDiff({
    required this.root,
    required this.hasBaseline,
    this.baselineScannedAt,
    this.currentScannedAt,
    required this.added,
    required this.removed,
    required this.grown,
    required this.shrunk,
    required this.summary,
  });

  factory ScanDiff.fromJson(Map<String, dynamic> json) {
    List<ScanDiffEntry> parseEntries(dynamic entriesList) {
      if (entriesList is! List) return [];
      return entriesList
          .map((e) => ScanDiffEntry.fromJson(e as Map<String, dynamic>))
          .toList();
    }

    return ScanDiff(
      root: json['root'] ?? '',
      hasBaseline: json['hasBaseline'] ?? false,
      baselineScannedAt: json['baselineScannedAt'] != null
          ? DateTime.tryParse(json['baselineScannedAt'])
          : null,
      currentScannedAt: json['currentScannedAt'] != null
          ? DateTime.tryParse(json['currentScannedAt'])
          : null,
      added: parseEntries(json['added']),
      removed: parseEntries(json['removed']),
      grown: parseEntries(json['grown']),
      shrunk: parseEntries(json['shrunk']),
      summary: json['summary'] != null
          ? ScanDiffSummary.fromJson(json['summary'])
          : ScanDiffSummary(
              addedCount: 0,
              addedBytes: 0,
              removedCount: 0,
              removedBytes: 0,
              grownCount: 0,
              grownDeltaBytes: 0,
              shrunkCount: 0,
              shrunkDeltaBytes: 0,
              netDeltaBytes: 0,
            ),
    );
  }
}

class ScanDiffNoScanException implements Exception {
  const ScanDiffNoScanException();
  @override
  String toString() => 'No scan data cached. Run a scan first.';
}

class ExtensionBreakdownResult {
  final String root;
  final List<ExtensionBreakdownItem> extensions;

  ExtensionBreakdownResult({required this.root, required this.extensions});

  factory ExtensionBreakdownResult.fromJson(Map<String, dynamic> json) {
    return ExtensionBreakdownResult(
      root: json['root'] ?? '',
      extensions:
          (json['extensions'] as List<dynamic>?)
              ?.map((e) => ExtensionBreakdownItem.fromJson(e))
              .toList() ??
          [],
    );
  }
}

class ExtensionBreakdownItem {
  final String ext;
  final String category;
  final int fileCount;
  final int totalBytes;
  final String sizeFormatted;
  final double percentOfDisk;
  final int averageFileSizeBytes;
  final String averageSizeFormatted;
  final String largestFilePath;
  final int largestFileBytes;
  final String largestSizeFormatted;

  ExtensionBreakdownItem({
    required this.ext,
    required this.category,
    required this.fileCount,
    required this.totalBytes,
    required this.sizeFormatted,
    required this.percentOfDisk,
    required this.averageFileSizeBytes,
    required this.averageSizeFormatted,
    required this.largestFilePath,
    required this.largestFileBytes,
    required this.largestSizeFormatted,
  });

  factory ExtensionBreakdownItem.fromJson(Map<String, dynamic> json) {
    return ExtensionBreakdownItem(
      ext: json['ext'] ?? '',
      category: json['category'] ?? '',
      fileCount: json['fileCount'] ?? 0,
      totalBytes: json['totalBytes'] ?? 0,
      sizeFormatted: json['sizeFormatted'] ?? '',
      percentOfDisk: (json['percentOfDisk'] as num?)?.toDouble() ?? 0.0,
      averageFileSizeBytes: json['averageFileSizeBytes'] ?? 0,
      averageSizeFormatted: json['averageSizeFormatted'] ?? '',
      largestFilePath: json['largestFilePath'] ?? '',
      largestFileBytes: json['largestFileBytes'] ?? 0,
      largestSizeFormatted: json['largestSizeFormatted'] ?? '',
    );
  }
}

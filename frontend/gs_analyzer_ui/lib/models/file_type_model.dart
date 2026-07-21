class FileTypeExtensionEntry {
  final String ext;
  final int fileCount;
  final double percentOfDisk;
  final String sizeFormatted;
  final int totalBytes;

  const FileTypeExtensionEntry({
    required this.ext,
    required this.fileCount,
    required this.percentOfDisk,
    required this.sizeFormatted,
    required this.totalBytes,
  });

  factory FileTypeExtensionEntry.fromJson(Map<String, dynamic> j) =>
      FileTypeExtensionEntry(
        ext: j['ext'] as String,
        fileCount: j['fileCount'] as int,
        percentOfDisk: (j['percentOfDisk'] as num).toDouble(),
        sizeFormatted: j['sizeFormatted'] as String,
        totalBytes: j['totalBytes'] as int,
      );
}

class FileTypeCategory {
  final String name;
  final int fileCount;
  final double percentOfDisk;
  final String sizeFormatted;
  final int totalBytes;
  final List<FileTypeExtensionEntry> extensions;

  const FileTypeCategory({
    required this.name,
    required this.fileCount,
    required this.percentOfDisk,
    required this.sizeFormatted,
    required this.totalBytes,
    required this.extensions,
  });

  factory FileTypeCategory.fromJson(Map<String, dynamic> j) => FileTypeCategory(
    name: j['name'] as String,
    fileCount: j['fileCount'] as int,
    percentOfDisk: (j['percentOfDisk'] as num).toDouble(),
    sizeFormatted: j['sizeFormatted'] as String,
    totalBytes: j['totalBytes'] as int,
    extensions: (j['extensions'] as List)
        .map((e) => FileTypeExtensionEntry.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
}

class FileTypeResult {
  final String root;
  final List<FileTypeCategory> categories;
  final String totalScannedFormatted;

  const FileTypeResult({
    required this.root,
    required this.categories,
    required this.totalScannedFormatted,
  });

  factory FileTypeResult.fromJson(Map<String, dynamic> j) => FileTypeResult(
    root: j['root'] as String,
    totalScannedFormatted: j['totalScannedFormatted'] as String,
    categories: (j['categories'] as List)
        .map((e) => FileTypeCategory.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
}

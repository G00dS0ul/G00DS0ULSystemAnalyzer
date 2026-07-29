/// Dart models mirroring the backend TempCleanerModels.cs DTOs.
///
/// Supports both camelCase (default JSON serializer) and PascalCase
/// (ASP.NET default) property names for resilience.

class TempLocationPreview {
  final String path;
  final String label;
  final String category;
  final int sizeBytes;
  final String sizeFormatted;
  final int fileCount;

  TempLocationPreview({
    required this.path,
    required this.label,
    required this.category,
    required this.sizeBytes,
    required this.sizeFormatted,
    required this.fileCount,
  });

  factory TempLocationPreview.fromJson(Map<String, dynamic> json) {
    return TempLocationPreview(
      path: json['path'] ?? json['Path'] ?? '',
      label: json['label'] ?? json['Label'] ?? '',
      category: json['category'] ?? json['Category'] ?? 'Temp',
      sizeBytes: json['sizeBytes'] ?? json['SizeBytes'] ?? 0,
      sizeFormatted: json['sizeFormatted'] ?? json['SizeFormatted'] ?? '',
      fileCount: json['fileCount'] ?? json['FileCount'] ?? 0,
    );
  }
}

class TempPreviewResponse {
  final int totalBytes;
  final String totalFormatted;
  final List<TempLocationPreview> locations;

  TempPreviewResponse({
    required this.totalBytes,
    required this.totalFormatted,
    required this.locations,
  });

  factory TempPreviewResponse.fromJson(Map<String, dynamic> json) {
    final rawLocations = json['locations'] ?? json['Locations'] ?? [];
    return TempPreviewResponse(
      totalBytes: json['totalBytes'] ?? json['TotalBytes'] ?? 0,
      totalFormatted: json['totalFormatted'] ?? json['TotalFormatted'] ?? '',
      locations: (rawLocations as List<dynamic>)
          .map((e) => TempLocationPreview.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class TempCleanResult {
  final int deletedFiles;
  final int freedBytes;
  final String freedFormatted;
  final int skippedFiles;

  TempCleanResult({
    required this.deletedFiles,
    required this.freedBytes,
    required this.freedFormatted,
    required this.skippedFiles,
  });

  factory TempCleanResult.fromJson(Map<String, dynamic> json) {
    return TempCleanResult(
      deletedFiles: json['deletedFiles'] ?? json['DeletedFiles'] ?? 0,
      freedBytes: json['freedBytes'] ?? json['FreedBytes'] ?? 0,
      freedFormatted: json['freedFormatted'] ?? json['FreedFormatted'] ?? '',
      skippedFiles: json['skippedFiles'] ?? json['SkippedFiles'] ?? 0,
    );
  }
}

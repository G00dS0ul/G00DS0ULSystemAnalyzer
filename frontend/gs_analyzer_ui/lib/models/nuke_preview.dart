class NukePathBreakdown {
  final String path;
  final int sizeBytes;
  final int fileCount;

  NukePathBreakdown({
    required this.path,
    required this.sizeBytes,
    required this.fileCount,
  });

  factory NukePathBreakdown.fromJson(Map<String, dynamic> json) {
    return NukePathBreakdown(
      path: json['path'] ?? '',
      sizeBytes: json['sizeBytes'] ?? 0,
      fileCount: json['fileCount'] ?? 0,
    );
  }
}

class NukePreviewResponse {
  final int totalFiles;
  final int totalBytes;
  final String totalFormatted;
  final String planToken;
  final List<NukePathBreakdown> breakdown;

  NukePreviewResponse({
    required this.totalFiles,
    required this.totalBytes,
    required this.totalFormatted,
    required this.planToken,
    required this.breakdown,
  });

  factory NukePreviewResponse.fromJson(Map<String, dynamic> json) {
    var list = json['breakdown'] as List? ?? [];
    List<NukePathBreakdown> breakdownList = list
        .map((e) => NukePathBreakdown.fromJson(e))
        .toList();

    return NukePreviewResponse(
      totalFiles: json['totalFiles'] ?? 0,
      totalBytes: json['totalBytes'] ?? 0,
      totalFormatted: json['totalFormatted'] ?? '',
      planToken: json['planToken'] ?? '',
      breakdown: breakdownList,
    );
  }
}

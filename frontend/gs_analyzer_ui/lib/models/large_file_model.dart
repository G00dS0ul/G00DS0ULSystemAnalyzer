class LargeFileModel {
  final String path;
  final int sizeBytes;
  final String sizeFormatted;

  LargeFileModel({
    required this.path,
    required this.sizeBytes,
    required this.sizeFormatted,
  });

  factory LargeFileModel.fromJson(Map<String, dynamic> json) {
    return LargeFileModel(
      path: json['path'] ?? json['Path'] ?? '',
      sizeBytes: json['sizeBytes'] ?? json['SizeBytes'] ?? 0,
      sizeFormatted: json['sizeFormatted'] ?? json['SizeFormatted'] ?? '',
    );
  }
}

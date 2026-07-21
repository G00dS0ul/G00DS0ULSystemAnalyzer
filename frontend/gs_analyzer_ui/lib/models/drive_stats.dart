class DriveStats {
  final int totalBytes;
  final int freeBytes;
  final int usedBytes;
  final double percentageFree;

  DriveStats({
    required this.totalBytes,
    required this.freeBytes,
    required this.usedBytes,
    required this.percentageFree,
  });

  factory DriveStats.fromJson(Map<String, dynamic> json) {
    return DriveStats(
      totalBytes: json['totalBytes'] ?? 0,
      freeBytes: json['freeBytes'] ?? 0,
      usedBytes: json['usedBytes'] ?? 0,
      percentageFree: (json['percentageFree'] ?? 0).toDouble(),
    );
  }
}

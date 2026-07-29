import 'dart:core';

class DriveInfo {
  final String name;
  final String label;
  final String type;
  final String format;
  final int totalBytes;
  final int freeBytes;
  final int usedBytes;
  final double percentageFree;
  final double percentageUsed;

  DriveInfo({
    required this.name,
    required this.label,
    required this.type,
    required this.format,
    required this.totalBytes,
    required this.freeBytes,
    required this.usedBytes,
    required this.percentageFree,
    required this.percentageUsed,
  });

  factory DriveInfo.fromJson(Map<String, dynamic> json) {
    final int total = json['totalBytes'] ?? 0;
    final int free = json['freeBytes'] ?? 0;
    final int used = json['usedBytes'] ?? 0;

    final double calcUsed = total == 0 ? 0.0 : (used / total) * 100;
    final double calcFree = total == 0 ? 0.0 : (free / total) * 100;

    return DriveInfo(
      name: json['name'] ?? '',
      label: json['label'] ?? 'Local Disk',
      type: json['type'] ?? 'Fixed',
      format: json['format'] ?? 'NTFS',
      totalBytes: json['totalBytes'] ?? 0,
      freeBytes: json['freeBytes'] ?? 0,
      usedBytes: json['usedBytes'] ?? 0,
      percentageFree: calcFree,
      percentageUsed: calcUsed,
    );
  }
}

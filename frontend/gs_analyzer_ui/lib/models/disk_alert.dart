class DiskAlert {
  final String driveName;
  final String label;
  final String driveType;
  final double usedPercent;
  final int freeBytes;
  final String freeFormatted;
  final double thresholdPercent;
  final String severity; // "warning" or "critical"
  final DateTime? firstDetectedAt;

  const DiskAlert({
    required this.driveName,
    required this.label,
    required this.driveType,
    required this.usedPercent,
    required this.freeBytes,
    required this.freeFormatted,
    required this.thresholdPercent,
    required this.severity,
    this.firstDetectedAt,
  });

  bool get isCritical => severity.toLowerCase() == 'critical' || usedPercent >= 98.0;

  /// Returns a clean formatted display name avoiding duplicates like 'C:\ (C:\)'.
  String get displayName {
    final l = label.trim();
    final d = driveName.trim();
    if (l.isNotEmpty && l.toLowerCase() != d.toLowerCase() && l != 'Local Disk') {
      return '$l ($d)';
    }
    return d.isNotEmpty ? d : (l.isNotEmpty ? l : 'Drive');
  }

  factory DiskAlert.fromJson(Map<String, dynamic> json) {
    return DiskAlert(
      driveName: (json['driveName'] as String?)?.trim() ?? '',
      label: (json['label'] as String?)?.trim() ?? '',
      driveType: (json['driveType'] as String?)?.trim() ?? 'Fixed',
      usedPercent: (json['usedPercent'] as num?)?.toDouble() ?? 0.0,
      freeBytes: (json['freeBytes'] as num?)?.toInt() ?? 0,
      freeFormatted: (json['freeFormatted'] as String?)?.trim() ?? '',
      thresholdPercent: (json['thresholdPercent'] as num?)?.toDouble() ?? 90.0,
      severity: (json['severity'] as String?)?.toLowerCase() ?? 'warning',
      firstDetectedAt: json['firstDetectedAt'] != null
          ? DateTime.tryParse(json['firstDetectedAt'].toString())
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'driveName': driveName,
      'label': label,
      'driveType': driveType,
      'usedPercent': usedPercent,
      'freeBytes': freeBytes,
      'freeFormatted': freeFormatted,
      'thresholdPercent': thresholdPercent,
      'severity': severity,
      'firstDetectedAt': firstDetectedAt?.toIso8601String(),
    };
  }

  @override
  String toString() =>
      'DiskAlert($driveName, label: $label, used: ${usedPercent.toStringAsFixed(1)}%, severity: $severity)';
}

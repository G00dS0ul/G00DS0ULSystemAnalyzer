class ScheduledScan {
  final String id;
  final String type;
  final String path;
  final String kind;
  final String? cron;
  final int? intervalMinutes;
  final bool enabled;
  final DateTime? lastRun;
  final DateTime? nextRun;

  ScheduledScan({
    required this.id,
    required this.type,
    required this.path,
    required this.kind,
    this.cron,
    this.intervalMinutes,
    required this.enabled,
    this.lastRun,
    this.nextRun,
  });

  factory ScheduledScan.fromJson(Map<String, dynamic> json) {
    return ScheduledScan(
      id: json['id'],
      type: json['type'],
      path: json['path'],
      kind: json['kind'],
      cron: json['cron'],
      intervalMinutes: json['intervalMinutes'],
      enabled: json['enabled'],
      lastRun: json['lastRun'] != null ? DateTime.parse(json['lastRun']) : null,
      nextRun: json['nextRun'] != null ? DateTime.parse(json['nextRun']) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'type': type,
      'path': path,
      'kind': kind,
      'cron': cron,
      'intervalMinutes': intervalMinutes,
      'enabled': enabled,
      'lastRun': lastRun?.toIso8601String(),
      'nextRun': nextRun?.toIso8601String(),
    };
  }

  ScheduledScan copyWith({
    String? id,
    String? type,
    String? path,
    String? kind,
    String? cron,
    int? intervalMinutes,
    bool? enabled,
    DateTime? lastRun,
    DateTime? nextRun,
  }) {
    return ScheduledScan(
      id: id ?? this.id,
      type: type ?? this.type,
      path: path ?? this.path,
      kind: kind ?? this.kind,
      cron: cron ?? this.cron,
      intervalMinutes: intervalMinutes ?? this.intervalMinutes,
      enabled: enabled ?? this.enabled,
      lastRun: lastRun ?? this.lastRun,
      nextRun: nextRun ?? this.nextRun,
    );
  }
}

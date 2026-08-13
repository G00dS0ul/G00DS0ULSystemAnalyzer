class TopConsumerEntry {
  final int pid;
  final String name;
  final double ramMb;

  TopConsumerEntry({
    required this.pid,
    required this.name,
    required this.ramMb,
  });

  factory TopConsumerEntry.fromJson(Map<String, dynamic> json) {
    return TopConsumerEntry(
      pid: json['pid'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      ramMb: (json['ramMb'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class RamAlert {
  final double usedPercent;
  final int availablePhysicalBytes;
  final String availableFormatted;
  final double thresholdPercent;
  final int minimumFreeMb;
  final String severity;
  final int sustainedForSeconds;
  final List<TopConsumerEntry> topConsumers;

  RamAlert({
    required this.usedPercent,
    required this.availablePhysicalBytes,
    required this.availableFormatted,
    required this.thresholdPercent,
    required this.minimumFreeMb,
    required this.severity,
    required this.sustainedForSeconds,
    required this.topConsumers,
  });

  factory RamAlert.fromJson(Map<String, dynamic> json) {
    var list = json['topConsumers'] as List? ?? [];
    List<TopConsumerEntry> consumersList =
        list.map((i) => TopConsumerEntry.fromJson(i as Map<String, dynamic>)).toList();

    return RamAlert(
      usedPercent: (json['usedPercent'] as num?)?.toDouble() ?? 0.0,
      availablePhysicalBytes: (json['availablePhysicalBytes'] as num?)?.toInt() ?? 0,
      availableFormatted: json['availableFormatted'] as String? ?? '',
      thresholdPercent: (json['thresholdPercent'] as num?)?.toDouble() ?? 0.0,
      minimumFreeMb: (json['minimumFreeMb'] as num?)?.toInt() ?? 0,
      severity: json['severity'] as String? ?? 'warning',
      sustainedForSeconds: (json['sustainedForSeconds'] as num?)?.toInt() ?? 0,
      topConsumers: consumersList,
    );
  }
}

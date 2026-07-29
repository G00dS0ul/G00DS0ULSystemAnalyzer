class CpuSnapshot {
  final double averageLoad;
  final double delta;
  final double currentFrequencyGhz;
  final int totalProcesses;
  final int totalThreads;
  final int totalHandles;
  final String l1Cache;
  final String l2Cache;
  final String l3Cache;
  final Map<String, List<double>> coreGroups;

  CpuSnapshot({
    required this.averageLoad,
    required this.delta,
    required this.currentFrequencyGhz,
    required this.totalProcesses,
    required this.totalThreads,
    required this.totalHandles,
    required this.l1Cache,
    required this.l2Cache,
    required this.l3Cache,
    required this.coreGroups,
  });

  factory CpuSnapshot.fromJson(Map<String, dynamic> json) {
    final Map<String, List<double>> safeCoreGroups = {};
    final rawGroups = json['coreGroups'] ?? json['CoreGroups'];

    if (rawGroups is Map) {
      rawGroups.forEach((key, value) {
        if (value is List) {
          safeCoreGroups[key.toString()] = value
              .map((e) => (e as num).toDouble())
              .toList();
        }
      });
    }

    return CpuSnapshot(
      averageLoad:
          (json['averageLoad'] ?? json['AverageLoad'] as num?)?.toDouble() ??
          0.0,
      delta: (json['delta'] ?? json['Delta'] as num?)?.toDouble() ?? 0.0,
      currentFrequencyGhz:
          (json['currentFrequencyGhz'] ?? json['CurrentFrequencyGhz'] as num?)
              ?.toDouble() ??
          0.0,
      totalProcesses:
          (json['totalProcesses'] ?? json['TotalProcesses'] as int? ?? 0),
      totalThreads: (json['totalThreads'] ?? json['TotalThreads'] as int? ?? 0),
      totalHandles: (json['totalHandles'] ?? json['TotalHandles'] as int? ?? 0),
      l1Cache: (json['l1Cache'] ?? json['L1Cache'] as String?) ?? 'N/A',
      l2Cache: (json['l2Cache'] ?? json['L2Cache'] as String?) ?? 'N/A',
      l3Cache: (json['l3Cache'] ?? json['L3Cache'] as String?) ?? 'N/A',
      coreGroups: safeCoreGroups,
    );
  }
}

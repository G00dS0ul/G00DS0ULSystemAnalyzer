import 'package:intl/intl.dart';

class CacheStats {
  final int activeEntries;
  final int rootCount;
  final int approximateMemoryBytes;
  final int hitCount;
  final int missCount;
  final double hitRate;
  final DateTime? oldestCachedAt;

  CacheStats({
    this.activeEntries = 0,
    this.rootCount = 0,
    this.approximateMemoryBytes = 0,
    this.hitCount = 0,
    this.missCount = 0,
    this.hitRate = 0.0,
    this.oldestCachedAt,
  });

  factory CacheStats.fromJson(Map<String, dynamic> json) {
    final raw = json.containsKey('data') && json['data'] is Map<String, dynamic>
        ? json['data'] as Map<String, dynamic>
        : json;

    return CacheStats(
      activeEntries: raw['nodeCount'] ??
          raw['activeEntries'] ??
          raw['entryCount'] ??
          raw['NodeCount'] ??
          raw['ActiveEntries'] ??
          0,
      rootCount: raw['rootCount'] ?? raw['RootCount'] ?? 0,
      approximateMemoryBytes: raw['approximateBytes'] ??
          raw['approximateMemoryBytes'] ??
          raw['ApproximateBytes'] ??
          raw['ApproximateMemoryBytes'] ??
          0,
      hitCount: raw['hitCount'] ?? raw['HitCount'] ?? 0,
      missCount: raw['missCount'] ?? raw['MissCount'] ?? 0,
      hitRate: ((raw['hitMissRatio'] ??
              raw['hitRate'] ??
              raw['HitMissRatio'] ??
              raw['HitRate'] ??
              0.0) as num)
          .toDouble(),
      oldestCachedAt: raw['oldestCachedAt'] != null
          ? DateTime.tryParse(raw['oldestCachedAt'].toString())
          : null,
    );
  }

  Map<String, dynamic> toJson() => {
        'activeEntries': activeEntries,
        'rootCount': rootCount,
        'approximateMemoryBytes': approximateMemoryBytes,
        'hitCount': hitCount,
        'missCount': missCount,
        'hitRate': hitRate,
        'oldestCachedAt': oldestCachedAt?.toIso8601String(),
      };

  String get formattedNodes => NumberFormat('#,###').format(activeEntries);

  String get formattedMemory {
    final gb = approximateMemoryBytes / (1024 * 1024 * 1024);
    if (gb >= 1.0) {
      return '${gb.toStringAsFixed(1)} GB';
    }
    final mb = approximateMemoryBytes / (1024 * 1024);
    if (mb >= 1.0) {
      return '${mb.toStringAsFixed(0)} MB';
    }
    final kb = approximateMemoryBytes / 1024;
    return '${kb.toStringAsFixed(0)} KB';
  }

  String get formattedHitRate {
    final pct = (hitRate * 100).round();
    return '$pct%';
  }
}

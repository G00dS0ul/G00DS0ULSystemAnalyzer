class TelemetryPoint {
  final DateTime timestamp;
  final double value;

  TelemetryPoint({required this.timestamp, required this.value});

  factory TelemetryPoint.fromJson(Map<String, dynamic> json) {
    return TelemetryPoint(
      timestamp: DateTime.parse(json['timestamp']).toLocal(),
      value: (json['value'] as num).toDouble(),
    );
  }
}

class TelemetryStats {
  final double min;
  final double max;
  final double avg;
  final double current;

  TelemetryStats({
    required this.min,
    required this.max,
    required this.avg,
    required this.current,
  });

  factory TelemetryStats.fromJson(Map<String, dynamic> json) {
    return TelemetryStats(
      min: (json['min'] as num).toDouble(),
      max: (json['max'] as num).toDouble(),
      avg: (json['avg'] as num).toDouble(),
      current: (json['current'] as num).toDouble(),
    );
  }
}

class TelemetryHistoryResponse {
  final String metric;
  final int minutes;
  final String unit;
  final List<TelemetryPoint> points;
  final TelemetryStats stats;

  TelemetryHistoryResponse({
    required this.metric,
    required this.minutes,
    required this.unit,
    required this.points,
    required this.stats,
  });

  factory TelemetryHistoryResponse.fromJson(Map<String, dynamic> json) {
    return TelemetryHistoryResponse(
      metric: json['metric'] ?? '',
      minutes: json['minutes'] ?? 5,
      unit: json['unit'] ?? '',
      points:
          (json['points'] as List<dynamic>?)
              ?.map((p) => TelemetryPoint.fromJson(p))
              .toList() ??
          [],
      stats: json['stats'] != null
          ? TelemetryStats.fromJson(json['stats'])
          : TelemetryStats(min: 0, max: 0, avg: 0, current: 0),
    );
  }
}

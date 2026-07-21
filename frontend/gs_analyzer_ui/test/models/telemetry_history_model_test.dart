import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/telemetry_history_model.dart';

void main() {
  group('TelemetryHistoryModel', () {
    test('TelemetryPoint fromJson parses correctly', () {
      final json = {'timestamp': '2023-10-01T12:00:00Z', 'value': 42.5};

      final point = TelemetryPoint.fromJson(json);

      expect(point.timestamp, DateTime.parse('2023-10-01T12:00:00Z').toLocal());
      expect(point.value, 42.5);
    });

    test('TelemetryStats fromJson parses correctly', () {
      final json = {'min': 10.0, 'max': 90.0, 'avg': 50.0, 'current': 60.0};

      final stats = TelemetryStats.fromJson(json);

      expect(stats.min, 10.0);
      expect(stats.max, 90.0);
      expect(stats.avg, 50.0);
      expect(stats.current, 60.0);
    });

    test('TelemetryHistoryResponse fromJson parses correctly', () {
      final json = {
        'metric': 'cpu',
        'minutes': 15,
        'unit': '%',
        'points': [
          {'timestamp': '2023-10-01T12:00:00Z', 'value': 20.0},
          {'timestamp': '2023-10-01T12:01:00Z', 'value': 30.0},
        ],
        'stats': {'min': 20.0, 'max': 30.0, 'avg': 25.0, 'current': 30.0},
      };

      final response = TelemetryHistoryResponse.fromJson(json);

      expect(response.metric, 'cpu');
      expect(response.minutes, 15);
      expect(response.unit, '%');
      expect(response.points.length, 2);
      expect(response.points.first.value, 20.0);
      expect(response.stats.min, 20.0);
      expect(response.stats.max, 30.0);
    });
  });
}

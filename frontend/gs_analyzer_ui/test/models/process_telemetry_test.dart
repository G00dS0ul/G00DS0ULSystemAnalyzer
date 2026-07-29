import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';

void main() {
  group('ProcessTelemetry.fromJson', () {
    test('parses all fields correctly', () {
      final json = {
        'processId': 1234,
        'name': 'devenv',
        'ramMb': 512.5,
        'cpuPercent': 18.3,
        'status': 'RUNNING',
        'user': 'G00dS0ul',
      };

      final p = ProcessTelemetry.fromJson(json, 16384.0);

      expect(p.pid, 1234);
      expect(p.name, 'devenv');
      expect(p.ramMb, 512.5);
      expect(p.cpuPercent, 18.3);
      expect(p.status, 'RUNNING');
      expect(p.user, 'G00dS0ul');
    });

    test('percentMem calculates from ramMb and totalSystemRamMb', () {
      final json = {
        'processId': 1,
        'name': 'x',
        'ramMb': 1638.4,
        'cpuPercent': 0.0,
        'status': 'RUNNING',
        'user': 'SYSTEM',
      };
      final p = ProcessTelemetry.fromJson(json, 16384.0);
      // 1638.4 / 16384.0 * 100 = 10.0
      expect(p.percentMem, closeTo(10.0, 0.01));
    });

    test('percentMem is zero when totalSystemRamMb is zero', () {
      final json = {
        'processId': 1,
        'name': 'x',
        'ramMb': 500.0,
        'cpuPercent': 0.0,
        'status': 'RUNNING',
        'user': 'SYSTEM',
      };
      final p = ProcessTelemetry.fromJson(json, 0.0);
      expect(p.percentMem, 0.0);
    });

    test('missing fields fall back to defaults', () {
      final p = ProcessTelemetry.fromJson({}, 16384.0);
      expect(p.pid, 0);
      expect(p.name, 'UNKNOWN');
      expect(p.ramMb, 0.0);
      expect(p.cpuPercent, 0.0);
      expect(p.status, 'UNKNOWN'); // was 'RUNNING' — fixed
      expect(p.user, 'UNKNOWN'); // was 'SYSTEM'  — fixed
    });

    test('null json values use defaults', () {
      final json = {
        'processId': null,
        'name': null,
        'ramMb': null,
        'cpuPercent': null,
        'status': null,
        'user': null,
      };
      final p = ProcessTelemetry.fromJson(json, 16384.0);
      expect(p.pid, 0);
      expect(p.name, 'UNKNOWN');
      expect(p.status, 'UNKNOWN'); // was 'RUNNING' — fixed
      expect(p.user, 'UNKNOWN'); // was 'SYSTEM'  — fixed
    });

    test('integer ramMb is coerced to double', () {
      final json = {
        'processId': 1,
        'name': 'x',
        'ramMb': 256,
        'cpuPercent': 0,
        'status': 'RUNNING',
        'user': 'SYSTEM',
      };
      final p = ProcessTelemetry.fromJson(json, 16384.0);
      expect(p.ramMb, 256.0);
    });
  });
}

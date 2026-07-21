import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/scheduled_scan_model.dart';

void main() {
  group('ScheduledScan Model', () {
    test('fromJson creates a valid object (Interval)', () {
      final json = {
        'id': 'scan-1',
        'type': 'Directory',
        'path': 'C:\\',
        'kind': 'Interval',
        'intervalMinutes': 60,
        'enabled': true,
        'lastRun': '2024-01-01T10:00:00Z',
        'nextRun': '2024-01-01T11:00:00Z',
      };

      final scan = ScheduledScan.fromJson(json);

      expect(scan.id, 'scan-1');
      expect(scan.type, 'Directory');
      expect(scan.path, 'C:\\');
      expect(scan.kind, 'Interval');
      expect(scan.intervalMinutes, 60);
      expect(scan.cron, isNull);
      expect(scan.enabled, isTrue);
      expect(scan.lastRun?.year, 2024);
      expect(scan.nextRun?.hour, 11);
    });

    test('fromJson creates a valid object (Cron)', () {
      final json = {
        'id': 'scan-2',
        'type': 'Duplicates',
        'path': 'D:\\',
        'kind': 'Cron',
        'cron': '0 0 * * *',
        'enabled': false,
      };

      final scan = ScheduledScan.fromJson(json);

      expect(scan.id, 'scan-2');
      expect(scan.type, 'Duplicates');
      expect(scan.path, 'D:\\');
      expect(scan.kind, 'Cron');
      expect(scan.intervalMinutes, isNull);
      expect(scan.cron, '0 0 * * *');
      expect(scan.enabled, isFalse);
      expect(scan.lastRun, isNull);
      expect(scan.nextRun, isNull);
    });

    test('toJson serializes correctly', () {
      final scan = ScheduledScan(
        id: 'scan-3',
        type: 'LargeFiles',
        path: 'E:\\',
        kind: 'Interval',
        intervalMinutes: 15,
        enabled: true,
        lastRun: DateTime.utc(2024, 5, 1, 12, 0, 0),
      );

      final json = scan.toJson();

      expect(json['id'], 'scan-3');
      expect(json['type'], 'LargeFiles');
      expect(json['intervalMinutes'], 15);
      expect(json['enabled'], isTrue);
      expect(json['lastRun'], '2024-05-01T12:00:00.000Z');
      expect(json['nextRun'], isNull);
    });

    test('copyWith updates fields', () {
      final scan = ScheduledScan(
        id: 'scan-1',
        type: 'Directory',
        path: 'C:\\',
        kind: 'Interval',
        enabled: true,
      );

      final updated = scan.copyWith(enabled: false, cron: '0 3 * * *');

      expect(updated.id, 'scan-1');
      expect(updated.enabled, isFalse);
      expect(updated.cron, '0 3 * * *');
      expect(updated.type, 'Directory');
    });
  });
}

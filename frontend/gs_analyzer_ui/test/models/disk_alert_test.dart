import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/disk_alert.dart';

void main() {
  group('DiskAlert Model Tests', () {
    test('fromJson parses standard warning alert correctly', () {
      final json = {
        'driveName': 'C:\\',
        'label': 'Windows (C:)',
        'driveType': 'Fixed',
        'usedPercent': 92.4,
        'freeBytes': 8000000000,
        'freeFormatted': '7.45 GB',
        'thresholdPercent': 90.0,
        'severity': 'warning',
        'firstDetectedAt': '2026-08-07T12:00:00.000Z',
      };

      final alert = DiskAlert.fromJson(json);

      expect(alert.driveName, 'C:\\');
      expect(alert.label, 'Windows (C:)');
      expect(alert.driveType, 'Fixed');
      expect(alert.usedPercent, 92.4);
      expect(alert.freeBytes, 8000000000);
      expect(alert.freeFormatted, '7.45 GB');
      expect(alert.thresholdPercent, 90.0);
      expect(alert.severity, 'warning');
      expect(alert.isCritical, false);
      expect(alert.firstDetectedAt, isNotNull);
    });

    test('isCritical returns true when severity is critical or usedPercent >= 98%', () {
      final criticalAlert = DiskAlert.fromJson({
        'driveName': 'D:\\',
        'label': 'Data',
        'severity': 'critical',
        'usedPercent': 94.0,
      });
      expect(criticalAlert.isCritical, true);

      final highUsageAlert = DiskAlert.fromJson({
        'driveName': 'E:\\',
        'label': 'Backup',
        'severity': 'warning',
        'usedPercent': 98.5,
      });
      expect(highUsageAlert.isCritical, true);

      final warningAlert = DiskAlert.fromJson({
        'driveName': 'C:\\',
        'label': 'System',
        'severity': 'warning',
        'usedPercent': 92.0,
      });
      expect(warningAlert.isCritical, false);
    });

    test('displayName formats cleanly avoiding duplicate drive names', () {
      final duplicateAlert = DiskAlert(
        driveName: 'C:\\',
        label: 'C:\\',
        driveType: 'Fixed',
        usedPercent: 50.0,
        freeBytes: 1000,
        freeFormatted: '1 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );
      expect(duplicateAlert.displayName, 'C:\\');

      final emptyLabelAlert = DiskAlert(
        driveName: 'C:\\',
        label: '',
        driveType: 'Fixed',
        usedPercent: 50.0,
        freeBytes: 1000,
        freeFormatted: '1 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );
      expect(emptyLabelAlert.displayName, 'C:\\');

      final localDiskAlert = DiskAlert(
        driveName: 'C:\\',
        label: 'Local Disk',
        driveType: 'Fixed',
        usedPercent: 50.0,
        freeBytes: 1000,
        freeFormatted: '1 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );
      expect(localDiskAlert.displayName, 'C:\\');

      final labeledAlert = DiskAlert(
        driveName: 'C:\\',
        label: 'Windows',
        driveType: 'Fixed',
        usedPercent: 50.0,
        freeBytes: 1000,
        freeFormatted: '1 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );
      expect(labeledAlert.displayName, 'Windows (C:\\)');
    });
  });
}

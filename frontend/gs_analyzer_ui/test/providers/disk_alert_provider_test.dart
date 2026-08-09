import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/disk_alert.dart';
import 'package:gs_analyzer_ui/providers/disk_alert_provider.dart';

void main() {
  group('DiskAlertsNotifier Tests', () {
    test('handleDiskAlert adds or updates alert in state', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final notifier = container.read(diskAlertsProvider.notifier);

      expect(container.read(diskAlertsProvider), isEmpty);

      final alertC = DiskAlert(
        driveName: 'C:\\',
        label: 'Windows (C:)',
        driveType: 'Fixed',
        usedPercent: 92.0,
        freeBytes: 8000000000,
        freeFormatted: '8.0 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );

      notifier.handleDiskAlert(alertC);
      expect(container.read(diskAlertsProvider).length, 1);
      expect(container.read(diskAlertsProvider)['C:\\']?.usedPercent, 92.0);

      // Update same drive
      final alertCUpdated = DiskAlert(
        driveName: 'C:\\',
        label: 'Windows (C:)',
        driveType: 'Fixed',
        usedPercent: 95.0,
        freeBytes: 5000000000,
        freeFormatted: '5.0 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );

      notifier.handleDiskAlert(alertCUpdated);
      expect(container.read(diskAlertsProvider).length, 1);
      expect(container.read(diskAlertsProvider)['C:\\']?.usedPercent, 95.0);
    });

    test('handleDiskAlertCleared removes cleared drive from state', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final notifier = container.read(diskAlertsProvider.notifier);

      final alertC = DiskAlert(
        driveName: 'C:\\',
        label: 'System',
        driveType: 'Fixed',
        usedPercent: 92.0,
        freeBytes: 8000000000,
        freeFormatted: '8.0 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );
      final alertD = DiskAlert(
        driveName: 'D:\\',
        label: 'Games',
        driveType: 'Fixed',
        usedPercent: 94.0,
        freeBytes: 6000000000,
        freeFormatted: '6.0 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      );

      notifier.handleDiskAlert(alertC);
      notifier.handleDiskAlert(alertD);
      expect(container.read(diskAlertsProvider).length, 2);

      notifier.handleDiskAlertCleared('C:\\');
      expect(container.read(diskAlertsProvider).length, 1);
      expect(container.read(diskAlertsProvider).containsKey('C:\\'), false);
      expect(container.read(diskAlertsProvider).containsKey('D:\\'), true);
    });

    test('pruneDrives removes active alerts for disconnected drives', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final notifier = container.read(diskAlertsProvider.notifier);

      notifier.handleDiskAlert(DiskAlert(
        driveName: 'C:\\',
        label: 'System',
        driveType: 'Fixed',
        usedPercent: 92.0,
        freeBytes: 8000000000,
        freeFormatted: '8.0 GB',
        thresholdPercent: 90.0,
        severity: 'warning',
      ));
      notifier.handleDiskAlert(DiskAlert(
        driveName: 'E:\\',
        label: 'USB_DISK',
        driveType: 'Removable',
        usedPercent: 96.0,
        freeBytes: 1000000000,
        freeFormatted: '1.0 GB',
        thresholdPercent: 95.0,
        severity: 'warning',
      ));

      expect(container.read(diskAlertsProvider).length, 2);

      // USB drive E:\ unplugged, only C:\ ready
      notifier.pruneDrives(['C:\\']);
      expect(container.read(diskAlertsProvider).length, 1);
      expect(container.read(diskAlertsProvider).containsKey('E:\\'), false);
      expect(container.read(diskAlertsProvider).containsKey('C:\\'), true);
    });
  });
}

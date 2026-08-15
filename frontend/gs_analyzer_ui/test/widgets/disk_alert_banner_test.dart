import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/disk_alert.dart';
import 'package:gs_analyzer_ui/providers/disk_alert_provider.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_mode_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_view_provider.dart';
import 'package:gs_analyzer_ui/widgets/disk_alert_banner.dart';

void main() {
  testWidgets('DiskAlertBannerList renders nothing when no alerts active',
      (tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(
          home: Scaffold(
            body: DiskAlertBannerList(),
          ),
        ),
      ),
    );

    expect(find.byType(DiskAlertBannerCard), findsNothing);
  });

  testWidgets('DiskAlertBannerList renders stacked banners ordered by usedPercent descending',
      (tester) async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    // Add 92% warning and 98.5% critical alerts
    container.read(diskAlertsProvider.notifier).handleDiskAlert(DiskAlert(
          driveName: 'C:\\',
          label: 'Windows (C:)',
          driveType: 'Fixed',
          usedPercent: 92.0,
          freeBytes: 8000000000,
          freeFormatted: '8.0 GB',
          thresholdPercent: 90.0,
          severity: 'warning',
        ));

    container.read(diskAlertsProvider.notifier).handleDiskAlert(DiskAlert(
          driveName: 'D:\\',
          label: 'Data (D:)',
          driveType: 'Fixed',
          usedPercent: 98.5,
          freeBytes: 1500000000,
          freeFormatted: '1.5 GB',
          thresholdPercent: 90.0,
          severity: 'critical',
        ));

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(
            body: DiskAlertBannerList(),
          ),
        ),
      ),
    );

    expect(find.byType(DiskAlertBannerCard), findsNWidgets(2));
    expect(find.text('CRITICAL DISK SPACE'), findsOneWidget);
    expect(find.text('LOW DISK SPACE WARNING'), findsOneWidget);

    // Verify ordering: D:\ (98.5%) should appear above C:\ (92.0%)
    final dPos = tester.getTopLeft(find.byKey(const Key('cleanup_btn_D:\\')));
    final cPos = tester.getTopLeft(find.byKey(const Key('cleanup_btn_C:\\')));
    expect(dPos.dy, lessThan(cPos.dy));

    // Tap CLEAN UP on C:\
    await tester.tap(find.byKey(const Key('cleanup_btn_C:\\')));
    await tester.pumpAndSettle();

    expect(container.read(selectedDriveNameProvider), 'C:\\');
    expect(container.read(storageModeProvider), StorageMode.tempFileCleaner);
    expect(container.read(storageViewProvider), StorageView.analyzer);
  });
}

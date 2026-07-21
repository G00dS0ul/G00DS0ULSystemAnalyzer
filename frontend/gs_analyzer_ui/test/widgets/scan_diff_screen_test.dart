import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/drive_info.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/providers/scan_diff_provider.dart';
import 'package:gs_analyzer_ui/screen/scan_diff_screen.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

DriveInfo _drive() => DriveInfo.fromJson({
  'name': r'C:\',
  'label': 'System',
  'type': 'fixed',
  'format': 'NTFS',
  'totalBytes': 1000,
  'freeBytes': 500,
  'usedBytes': 500,
});

Map<String, dynamic> _diffJson({
  bool hasBaseline = true,
  int added = 1,
  int grown = 1,
}) => {
  'root': 'C:/',
  'hasBaseline': hasBaseline,
  'baselineScannedAt': hasBaseline ? '2026-07-14T09:12:00Z' : null,
  'currentScannedAt': '2026-07-15T14:03:00Z',
  'added': List.generate(
    added,
    (i) => {
      'path': 'C:/added/file$i.iso',
      'isDirectory': false,
      'currentBytes': 5100273664,
      'previousBytes': 0,
      'deltaBytes': 5100273664,
      'childCount': null,
      'lastModified': null,
    },
  ),
  'removed': const [],
  'grown': List.generate(
    grown,
    (i) => {
      'path': 'C:/grown/dir$i',
      'isDirectory': true,
      'currentBytes': 3221225472,
      'previousBytes': 1073741824,
      'deltaBytes': 2147483648,
      'childCount': null,
      'lastModified': null,
    },
  ),
  'shrunk': const [],
  'summary': {
    'addedCount': added,
    'addedBytes': added * 5100273664,
    'removedCount': 0,
    'removedBytes': 0,
    'grownCount': grown,
    'grownDeltaBytes': grown * 2147483648,
    'shrunkCount': 0,
    'shrunkDeltaBytes': 0,
    'netDeltaBytes': added * 5100273664 + grown * 2147483648,
  },
};

Future<void> _pump(WidgetTester tester, MockClient client) {
  return tester.pumpWidget(
    ProviderScope(
      overrides: [
        apiServiceProvider.overrideWithValue(ApiService(client)),
        // Seed a single fixed drive so currentDriveProvider resolves to C:\.
        drivesProvider.overrideWith(() => _StubDrivesNotifier([_drive()])),
      ],
      child: const MaterialApp(home: ScanDiffScreen()),
    ),
  );
}

class _StubDrivesNotifier extends DrivesNotifier {
  _StubDrivesNotifier(this._drives);
  final List<DriveInfo> _drives;
  @override
  List<DriveInfo> build() => _drives;
}

void main() {
  testWidgets('renders the summary strip and change sections on success', (
    tester,
  ) async {
    final client = MockClient(
      (_) async => http.Response(jsonEncode(_diffJson()), 200),
    );
    await _pump(tester, client);
    await tester.pumpAndSettle();

    expect(find.text('WHAT_CHANGED'), findsOneWidget);
    // Section headers include their counts.
    expect(find.textContaining('ADDED'), findsWidgets);
    expect(find.textContaining('GROWN'), findsWidgets);
    // The added file surfaces by name.
    expect(find.textContaining('file0.iso'), findsOneWidget);
  });

  testWidgets('first-scan (no baseline) shows the BASELINE SAVED state', (
    tester,
  ) async {
    final client = MockClient(
      (_) async => http.Response(
        jsonEncode(_diffJson(hasBaseline: false, added: 0, grown: 0)),
        200,
      ),
    );
    await _pump(tester, client);
    await tester.pumpAndSettle();

    expect(find.textContaining('FIRST SCAN'), findsOneWidget);
  });

  testWidgets('409 (no scan cached) shows the RUN A SCAN state', (tester) async {
    final client = MockClient(
      (_) async => http.Response(
        jsonEncode({'error': 'NO_SCAN_CACHED', 'message': 'x'}),
        409,
      ),
    );
    await _pump(tester, client);
    await tester.pumpAndSettle();

    expect(find.textContaining('NO SCAN'), findsOneWidget);
  });
}

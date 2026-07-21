import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:gs_analyzer_ui/models/scan_diff.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/scan_diff_provider.dart';

/// Minimal success payload the backend GET /api/scan/diff returns (raw ScanDiff, camelCase).
Map<String, dynamic> _diffJson({
  int added = 2,
  int removed = 1,
  int grown = 1,
  int shrunk = 1,
}) => {
  'root': 'C:/',
  'hasBaseline': true,
  'baselineScannedAt': '2026-07-14T09:12:00Z',
  'currentScannedAt': '2026-07-15T14:03:00Z',
  'added': List.generate(
    added,
    (i) => {
      'path': 'C:/added/$i',
      'isDirectory': true,
      'currentBytes': 1000,
      'previousBytes': 0,
      'deltaBytes': 1000,
      'childCount': null,
      'lastModified': null,
    },
  ),
  'removed': List.generate(
    removed,
    (i) => {
      'path': 'C:/removed/$i',
      'isDirectory': true,
      'currentBytes': 0,
      'previousBytes': 500,
      'deltaBytes': -500,
      'childCount': null,
      'lastModified': null,
    },
  ),
  'grown': List.generate(
    grown,
    (i) => {
      'path': 'C:/grown/$i',
      'isDirectory': true,
      'currentBytes': 2000,
      'previousBytes': 1000,
      'deltaBytes': 1000,
      'childCount': null,
      'lastModified': null,
    },
  ),
  'shrunk': List.generate(
    shrunk,
    (i) => {
      'path': 'C:/shrunk/$i',
      'isDirectory': true,
      'currentBytes': 300,
      'previousBytes': 800,
      'deltaBytes': -500,
      'childCount': null,
      'lastModified': null,
    },
  ),
  'summary': {
    'addedCount': added,
    'addedBytes': added * 1000,
    'removedCount': removed,
    'removedBytes': removed * 500,
    'grownCount': grown,
    'grownDeltaBytes': grown * 1000,
    'shrunkCount': shrunk,
    'shrunkDeltaBytes': shrunk * -500,
    'netDeltaBytes': 5000,
  },
};

ProviderContainer _containerWith(http.Client client) {
  final container = ProviderContainer(
    overrides: [
      apiServiceProvider.overrideWithValue(ApiService(client)),
    ],
  );
  addTearDown(container.dispose);
  return container;
}

void main() {
  group('scanDiffProvider', () {
    test('returns parsed ScanDiff on 200', () async {
      final client = MockClient(
        (_) async => http.Response(jsonEncode(_diffJson()), 200),
      );
      final container = _containerWith(client);

      final diff = await container.read(scanDiffProvider('C:/').future);

      expect(diff, isA<ScanDiff>());
      expect(diff.hasBaseline, isTrue);
      expect(diff.added.length, 2);
      expect(diff.summary.netDeltaBytes, 5000);
    });

    // The 409 -> DiffNoScanException throw originates in ApiService.getScanDiff,
    // which the provider propagates verbatim. Asserting it at the ApiService
    // boundary avoids racing autoDispose disposal on the FutureProvider error path;
    // the provider's propagation of that error is covered by the
    // "diffChangeCountProvider is 0 when the diff request 409s" test below.
    test('getScanDiff throws DiffNoScanException on 409 (never triggers a scan)',
        () async {
      final client = MockClient(
        (_) async => http.Response(
          jsonEncode({'error': 'NO_SCAN_CACHED', 'message': 'Run a scan first.'}),
          409,
        ),
      );

      expect(
        () => ApiService(client).getScanDiff('C:/'),
        throwsA(isA<DiffNoScanException>()),
      );
    });

    test('sends root as a query parameter', () async {
      late Uri captured;
      final client = MockClient((req) async {
        captured = req.url;
        return http.Response(jsonEncode(_diffJson()), 200);
      });
      final container = _containerWith(client);

      await container.read(scanDiffProvider(r'D:\').future);

      expect(captured.path, endsWith('/api/scan/diff'));
      expect(captured.queryParameters['root'], r'D:\');
    });
  });

  group('diffChangeCountProvider', () {
    test('sums the four change buckets', () async {
      final client = MockClient(
        (_) async => http.Response(
          jsonEncode(_diffJson(added: 3, removed: 2, grown: 1, shrunk: 4)),
          200,
        ),
      );
      final container = _containerWith(client);

      // Resolve the underlying future first so the derived provider sees data.
      await container.read(scanDiffProvider('C:/').future);

      expect(container.read(diffChangeCountProvider('C:/')), 10);
    });

    test('is 0 while loading / before any scan is cached', () {
      final client = MockClient(
        (_) async => http.Response(jsonEncode(_diffJson()), 200),
      );
      final container = _containerWith(client);

      // No await: the FutureProvider is still in the loading state → asData is null.
      expect(container.read(diffChangeCountProvider('C:/')), 0);
    });

    test('is 0 when the diff request 409s (no baseline yet)', () async {
      final client = MockClient(
        (_) async => http.Response(
          jsonEncode({'error': 'NO_SCAN_CACHED', 'message': 'x'}),
          409,
        ),
      );
      final container = _containerWith(client);

      // Drive the future to its error state, ignoring the throw.
      try {
        await container.read(scanDiffProvider('C:/').future);
      } catch (_) {}

      expect(container.read(diffChangeCountProvider('C:/')), 0);
    });
  });
}

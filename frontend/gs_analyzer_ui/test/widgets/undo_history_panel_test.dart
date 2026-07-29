import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/nuke_result.dart';
import 'package:gs_analyzer_ui/widgets/undo_history_panel.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'dart:convert';

NukeOperation _op({
  required String id,
  required bool recoverable,
  int files = 1,
}) => NukeOperation(
  operationId: id,
  executedAt: DateTime(2026, 6, 24, 10, 0, 0),
  originalPaths: const [],
  deletedPaths: const [],
  usedRecycleBin: recoverable,
  deletedFiles: files,
);

Future<void> _pump(
  WidgetTester tester,
  List<NukeOperation> ops, {
  ApiService? apiService,
}) {
  return tester.pumpWidget(
    ProviderScope(
      overrides: [undoHistoryProvider.overrideWith((ref) async => ops)],
      child: MaterialApp(
        home: Scaffold(body: UndoHistoryPanel(apiService: apiService)),
      ),
    ),
  );
}

void main() {
  testWidgets(
    'collapsed by default; expanding reveals a recoverable row with UNDO',
    (tester) async {
      await _pump(tester, [_op(id: 'op-1', recoverable: true, files: 3)]);
      await tester.pumpAndSettle();

      expect(find.text('UNDO HISTORY'), findsOneWidget);
      expect(find.text('3 ITEMS'), findsNothing); // hidden while collapsed

      await tester.tap(find.text('UNDO HISTORY'));
      await tester.pumpAndSettle();

      expect(find.text('3 ITEMS'), findsOneWidget);
      expect(find.text('UNDO'), findsOneWidget);
    },
  );

  testWidgets('permanent op shows a lock, not an UNDO button', (tester) async {
    await _pump(tester, [_op(id: 'op-2', recoverable: false, files: 5)]);
    await tester.pumpAndSettle();
    await tester.tap(find.text('UNDO HISTORY'));
    await tester.pumpAndSettle();

    expect(find.text('5 ITEMS'), findsOneWidget);
    expect(find.textContaining('PERMANENT'), findsOneWidget);
    expect(find.text('UNDO'), findsNothing);
  });

  testWidgets('empty history shows the placeholder', (tester) async {
    await _pump(tester, const []);
    await tester.pumpAndSettle();
    await tester.tap(find.text('UNDO HISTORY'));
    await tester.pumpAndSettle();

    expect(find.text('NO OPERATIONS RECORDED'), findsOneWidget);
  });

  testWidgets('tapping UNDO row sends op.operationId to apiService', (
    tester,
  ) async {
    late Uri capturedUrl;
    final client = MockClient((req) async {
      capturedUrl = req.url;
      return http.Response(
        jsonEncode({
          'success': true,
          'data': {
            'deletedFiles': 3,
            'freedBytes': 0,
            'freedFormatted': '0 B',
            'stagedBytes': 100,
            'stagedFormatted': '100 B',
            'skippedFiles': 0,
            'recycleBinUsed': true,
            'recoverable': false,
            'operationId': 'op-999',
          },
        }),
        200,
      );
    });

    final apiService = ApiService(client);
    await _pump(tester, [
      _op(id: 'op-999', recoverable: true, files: 3),
    ], apiService: apiService);
    await tester.pumpAndSettle();

    await tester.tap(find.text('UNDO HISTORY'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('UNDO'));
    await tester.pumpAndSettle();

    expect(capturedUrl.path, endsWith('/api/nuke/undo/op-999'));
  });
}

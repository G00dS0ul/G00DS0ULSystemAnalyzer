import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/widgets/nuke_preview_dialog.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'dart:convert';

void main() {
  testWidgets('recycle-bin toggle updates NukePreviewResult correctly', (
    tester,
  ) async {
    final client = MockClient((req) async {
      return http.Response(
        jsonEncode({
          'success': true,
          'data': {
            'totalBytes': 1000,
            'totalFormatted': '1 KB',
            'totalFiles': 2,
            'totalDirectories': 1,
            'planToken': 'plan-abc',
            'stagedPaths': ['C:\\temp\\x'],
            'errors': [],
          },
        }),
        200,
      );
    });

    final apiService = ApiService(client);

    dynamic dialogResult;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () async {
                dialogResult = await showDialog(
                  context: context,
                  builder: (context) => NukePreviewDialog(
                    targetPaths: const ['C:\\temp\\x'],
                    apiService: apiService,
                  ),
                );
              },
              child: const Text('Show Dialog'),
            ),
          ),
        ),
      ),
    );

    // Open dialog
    await tester.tap(find.text('Show Dialog'));
    await tester.pumpAndSettle();

    // The switch for recycle bin should be initially off (default)
    expect(find.byType(Switch), findsOneWidget);
    Switch switchWidget = tester.widget(find.byType(Switch));
    expect(switchWidget.value, isFalse);

    // Toggle the switch
    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    // The button should now be MOVE TO RECYCLE BIN, tap it
    await tester.tap(find.text('MOVE TO RECYCLE BIN').last);
    await tester.pumpAndSettle();

    expect(dialogResult, isNotNull);
    // Since dialog returns a Map or a typed record, I'll access fields
    // Wait, NukePreviewDialog pops a map: {'execute': true, 'useRecycleBin': _useRecycleBin, 'planToken': _preview!.planToken}
    expect(dialogResult.confirmed, isTrue);
    expect(dialogResult.useRecycleBin, isTrue);
    expect(dialogResult.planToken, 'plan-abc');
  });
}

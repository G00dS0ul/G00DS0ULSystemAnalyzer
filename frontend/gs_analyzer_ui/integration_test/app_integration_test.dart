import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/main.dart' as app;
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('End-to-End System Integration', () {
    testWidgets('App boots, connects to C# Backend, and loads C:/ drive', (
      WidgetTester tester,
    ) async {
      app.main();

      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 4));
      await tester.pumpAndSettle();

      final windowsFolderFinder = find.text('Windows');
      final userFolderFinder = find.text('Users');

      expect(
        windowsFolderFinder,
        findsWidgets,
        reason:
            'CRITICAL: The UI did not render the Windows folder. Is the C# Backend running?',
      );

      expect(
        userFolderFinder,
        findsWidgets,
        reason: 'CRITICAL: The UI did not render the Users folder.',
      );
    });
  });
}

import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/temp_cleaner_model.dart';
import 'package:gs_analyzer_ui/providers/temp_cleaner_provider.dart';

void main() {
  group('TempCleanerNotifier state manipulations', () {
    late ProviderContainer container;

    setUp(() {
      container = ProviderContainer();
    });

    tearDown(() {
      container.dispose();
    });

    test('initial state has no preview and isLoading false', () {
      final state = container.read(tempCleanerProvider);
      expect(state.isLoading, isFalse);
      expect(state.preview, isNull);
      expect(state.selectedPaths, isEmpty);
      expect(state.cleanResult, isNull);
      expect(state.errorMessage, isNull);
    });

    test('togglePath adds and removes a path', () {
      final notifier = container.read(tempCleanerProvider.notifier);

      notifier.togglePath('C:\\temp');
      expect(
        container.read(tempCleanerProvider).selectedPaths,
        contains('C:\\temp'),
      );

      notifier.togglePath('C:\\temp');
      expect(
        container.read(tempCleanerProvider).selectedPaths,
        isNot(contains('C:\\temp')),
      );
    });

    test('reset clears all state', () {
      final notifier = container.read(tempCleanerProvider.notifier);

      notifier.togglePath('C:\\temp');
      expect(container.read(tempCleanerProvider).selectedPaths, isNotEmpty);

      notifier.reset();

      final state = container.read(tempCleanerProvider);
      expect(state.isLoading, isFalse);
      expect(state.preview, isNull);
      expect(state.selectedPaths, isEmpty);
      expect(state.cleanResult, isNull);
      expect(state.errorMessage, isNull);
    });
  });
}

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'mock_factory.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  SharedPreferences.setMockInitialValues({});

  group('RAM Telemetry Engine Test', () {
    test(
      'Test 1: Math & Formating (Translate raw bytes to readable strings',
      () {
        final container = ProviderContainer();
        final ramNotifier = container.read(ramProvider.notifier);

        final rawTelemetry = MockFactory.generateRamTelemetry(
          activeGb: 8.0,
          totalGb: 16.0,
        );

        ramNotifier.updateProcesses(rawTelemetry);

        final state = ramNotifier.state;

        expect(state.activeGb, 8.0, reason: 'Percentage math is incorrect!');

        expect(
          state.displayedString,
          '8.00 GB / 16.00 GB',
          reason:
              'CRITICAL: The UI string formatter failed to convert bytes to GB!',
        );
      },
    );

    test('Test 2: The Overload Shield (Triggers warning state at 90% usage)', () {
      final container = ProviderContainer();
      final notifier = container.read(ramProvider.notifier);

      final criticalTelemetry = MockFactory.generateRamTelemetry(
        activeGb: 15.2,
        totalGb: 16.0,
      );

      notifier.updateProcesses(criticalTelemetry);

      expect(
        notifier.state.isCritical,
        true,
        reason:
            'CRITICAL FAILURE: The engine did not warn the UI of a RAM overload!',
      );
    });

    test('Test 3: The live Graph Engine (Maintain exactly ticks of history)', () {
      final container = ProviderContainer();
      final notifier = container.read(ramProvider.notifier);

      for (int i = 0; i < 25; i++) {
        final tickData = MockFactory.generateRamTelemetry(
          activeGb: 0.1 * (i * 0.01),
        );
        notifier.updateProcesses(tickData);
      }

      expect(
        notifier.state.usageHistory.length,
        20,
        reason:
            'CRITICAL: The history array is growing infinitely! It must be capped at 20 (or your max graph size)',
      );
    });
  });
}

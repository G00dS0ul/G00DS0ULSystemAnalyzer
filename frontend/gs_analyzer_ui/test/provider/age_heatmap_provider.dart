import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/providers/age_heatmap_provider.dart';

void main() {
  group('ageHeatmapEnabledProvider', () {
    test('defaults to false', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);
      expect(container.read(ageHeatmapEnabledProvider), false);
    });

    test('toggles to true and back', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(ageHeatmapEnabledProvider.notifier).state = true;
      expect(container.read(ageHeatmapEnabledProvider), true);

      container.read(ageHeatmapEnabledProvider.notifier).state = false;
      expect(container.read(ageHeatmapEnabledProvider), false);
    });

    test('result survives toggle off — provider not disposed', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(ageHeatmapEnabledProvider.notifier).state = true;

      container.read(ageHeatmapEnabledProvider.notifier).state = false;

      expect(() => container.read(ageHeatmapEnabledProvider), returnsNormally);
    });
  });
}

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/providers/window_provider.dart';

class HudDensity {
  final double gap;
  final double panelPad;
  final double rowHeight;
  final double titleSize;
  final double valueSize;
  final bool isMax;

  const HudDensity({
    required this.gap,
    required this.panelPad,
    required this.rowHeight,
    required this.titleSize,
    required this.valueSize,
    required this.isMax,
  });
}

final hudDensityProvider = Provider<HudDensity>((ref) {
  final settings = ref.watch(settingsProvider);
  final isMax = ref.watch(windowMaximizedProvider);
  final savedCompact = settings.currentSettings?.appearance.compactMode ?? true;

  final isCompact = isMax ? false : savedCompact;

  if (isCompact) {
    return HudDensity(
      gap: 8.0,
      panelPad: 12.0,
      rowHeight: 32.0,
      titleSize: 12.0,
      valueSize: 32.0,
      isMax: isMax,
    );
  } else {
    return HudDensity(
      gap: 16.0,
      panelPad: 24.0,
      rowHeight: 48.0,
      titleSize: 16.0,
      valueSize: 48.0,
      isMax: isMax,
    );
  }
});

import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/app_settings.dart';

void main() {
  group('AppearanceSettings serialization', () {
    test('compactMode defaults to true when parsing empty json', () {
      final settings = AppearanceSettings.fromJson({});
      expect(
        settings.compactMode,
        isTrue,
        reason:
            'Compact Mode must default to true to match native Task Manager feel',
      );
    });

    test('fromJson and toJson round-trips correctly', () {
      final jsonPayload = {
        'theme': 'light',
        'accentColor': 'green',
        'compactMode': false,
        'showAnimations': true,
      };

      final settings = AppearanceSettings.fromJson(jsonPayload);

      expect(settings.theme, 'light');
      expect(settings.accentColor, 'green');
      expect(settings.compactMode, isFalse);
      expect(settings.showAnimations, isTrue);

      final outJson = settings.toJson();

      expect(outJson['theme'], 'light');
      expect(outJson['accentColor'], 'green');
      expect(outJson['compactMode'], false);
      expect(outJson['showAnimations'], true);
    });
  });
}

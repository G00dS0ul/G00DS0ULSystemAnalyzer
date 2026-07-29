import 'dart:async';

import 'package:leak_tracker_flutter_testing/leak_tracker_flutter_testing.dart';

/// Auto-discovered by `flutter test` (it MUST live at test/flutter_test_config.dart).
/// Enables leak tracking for every testWidgets() in the suite, so undisposed
/// StreamSubscription / AnimationController / TextEditingController and
/// un-onDispose'd Riverpod subscriptions fail the test.
Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  LeakTesting.enable();
  LeakTracking.warnForUnsupportedPlatforms = false;
  LeakTesting.settings = LeakTesting.settings.withIgnored(
    classes: ['PanGestureRecognizer', 'TapGestureRecognizer', 'LongPressGestureRecognizer'],
  );
  await testMain();
}

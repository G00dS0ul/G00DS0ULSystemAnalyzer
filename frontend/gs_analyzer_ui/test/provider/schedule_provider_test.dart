import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/schedule_provider.dart';
import 'package:gs_analyzer_ui/models/scheduled_scan_model.dart';

class TestScheduleNotifier extends ScheduleNotifier {
  @override
  Future<List<ScheduledScan>> fetchSchedules() async {
    return []; // Instantly return empty list
  }
}

void main() {
  group('ScheduleNotifier', () {
    test('updateFromSignalR sets state correctly', () async {
      final container = ProviderContainer(
        overrides: [
          scheduleProvider.overrideWith(() => TestScheduleNotifier()),
        ],
      );
      addTearDown(container.dispose);

      final notifier = container.read(scheduleProvider.notifier);
      
      try {
        await container.read(scheduleProvider.future);
      } catch (_) {}

      final jsonList = [
        {
          'id': '1',
          'type': 'Directory',
          'path': 'C:\\',
          'kind': 'Interval',
          'intervalMinutes': 15,
          'enabled': true,
        },
        {
          'id': '2',
          'type': 'LargeFiles',
          'path': 'D:\\',
          'kind': 'Cron',
          'cron': '* * * * *',
          'enabled': false,
        }
      ];

      notifier.updateFromSignalR(jsonList);

      final state = container.read(scheduleProvider);
      
      expect(state.isLoading, isFalse);
      expect(state.hasError, isFalse);
      expect(state.hasValue, isTrue);

      final schedules = state.value!;
      expect(schedules.length, 2);
      
      expect(schedules[0].id, '1');
      expect(schedules[0].type, 'Directory');
      expect(schedules[0].path, 'C:\\');
      expect(schedules[0].enabled, isTrue);
      
      expect(schedules[1].id, '2');
      expect(schedules[1].type, 'LargeFiles');
      expect(schedules[1].path, 'D:\\');
      expect(schedules[1].enabled, isFalse);
    });
  });
}

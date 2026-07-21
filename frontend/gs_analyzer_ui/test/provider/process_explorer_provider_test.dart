import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';
import 'package:gs_analyzer_ui/providers/process_explorer_provider.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';

// Seeds the ramProvider with known groups, bypassing SignalR
ProviderContainer _containerWith(List<ProcessGroup> groups) {
  final container = ProviderContainer();
  addTearDown(container.dispose);
  // Force state before any reads so providers see the seeded data
  container.read(ramProvider.notifier).debugSetGroupsForTest(groups);
  return container;
}

// Shorthand group builder
ProcessGroup _g({
  required String name,
  double ramMb = 100.0,
  double cpu = 5.0,
  String status = 'RUNNING',
  String user = 'SYSTEM',
  int pid = 1,
}) => ProcessGroup(
  name: name,
  processes: [
    ProcessTelemetry(
      pid: pid,
      name: name,
      ramMb: ramMb,
      percentMem: ramMb / 163.84,
      cpuPercent: cpu,
      status: status,
      user: user,
    ),
  ],
);

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  SharedPreferences.setMockInitialValues({});

  group('Name filter', () {
    test('empty filter returns all groups', () {
      final c = _containerWith([
        _g(name: 'chrome'),
        _g(name: 'devenv'),
        _g(name: 'system'),
      ]);
      expect(c.read(filteredProcessesProvider).length, 3);
    });

    test('filter is case-insensitive', () {
      final c = _containerWith([
        _g(name: 'Chrome'),
        _g(name: 'devenv'),
        _g(name: 'chrome_elf'),
      ]);
      c.read(processFilterProvider.notifier).state = 'chrome';
      final result = c.read(filteredProcessesProvider);
      expect(result.length, 2);
      expect(
        result.every((g) => g.name.toLowerCase().contains('chrome')),
        isTrue,
      );
    });

    test('filter by PID string', () {
      final c = _containerWith([
        _g(name: 'chrome', pid: 1234),
        _g(name: 'devenv', pid: 5678),
      ]);
      c.read(processFilterProvider.notifier).state = '1234';
      final result = c.read(filteredProcessesProvider);
      expect(result.length, 1);
      expect(result.first.name, 'chrome');
    });

    test('no match returns empty list', () {
      final c = _containerWith([_g(name: 'chrome'), _g(name: 'devenv')]);
      c.read(processFilterProvider.notifier).state = 'zzznomatch';
      expect(c.read(filteredProcessesProvider), isEmpty);
    });

    test('whitespace-only filter returns all groups', () {
      final c = _containerWith([_g(name: 'chrome'), _g(name: 'devenv')]);
      c.read(processFilterProvider.notifier).state = '   ';
      expect(c.read(filteredProcessesProvider).length, 2);
    });
  });

  group('Sort by CPU% (default)', () {
    test('sorted descending by CPU', () {
      final c = _containerWith([
        _g(name: 'alpha', cpu: 5.0),
        _g(name: 'beta', cpu: 25.0),
        _g(name: 'gamma', cpu: 10.0),
      ]);
      final result = c.read(filteredProcessesProvider);
      expect(result[0].name, 'beta');
      expect(result[1].name, 'gamma');
      expect(result[2].name, 'alpha');
    });

    test('CPU tie broken by RAM descending', () {
      final c = _containerWith([
        _g(name: 'a', cpu: 10.0, ramMb: 100.0),
        _g(name: 'b', cpu: 10.0, ramMb: 400.0),
        _g(name: 'c', cpu: 10.0, ramMb: 200.0),
      ]);
      final result = c.read(filteredProcessesProvider);
      expect(result[0].name, 'b'); // highest RAM wins tie
      expect(result[1].name, 'c');
      expect(result[2].name, 'a');
    });
  });

  group('Sort by RAM', () {
    test('sorted descending by RAM', () {
      final c = _containerWith([
        _g(name: 'alpha', ramMb: 300.0),
        _g(name: 'beta', ramMb: 100.0),
        _g(name: 'gamma', ramMb: 200.0),
      ]);
      c.read(processSortModeProvider.notifier).state = ProcessSortMode.ram;
      final result = c.read(filteredProcessesProvider);
      expect(result[0].name, 'alpha');
      expect(result[1].name, 'gamma');
      expect(result[2].name, 'beta');
    });
  });

  group('Sort by PID', () {
    test('sorted ascending by PID', () {
      final c = _containerWith([
        _g(name: 'alpha', pid: 30),
        _g(name: 'beta', pid: 10),
        _g(name: 'gamma', pid: 20),
      ]);
      c.read(processSortModeProvider.notifier).state = ProcessSortMode.pid;
      final result = c.read(filteredProcessesProvider);
      expect(result[0].primaryPid, 10);
      expect(result[1].primaryPid, 20);
      expect(result[2].primaryPid, 30);
    });
  });

  group('Sort by name', () {
    test('sorted ascending alphabetically', () {
      final c = _containerWith([
        _g(name: 'gamma'),
        _g(name: 'alpha'),
        _g(name: 'beta'),
      ]);
      c.read(processSortModeProvider.notifier).state = ProcessSortMode.name;
      final result = c.read(filteredProcessesProvider);
      expect(result[0].name, 'alpha');
      expect(result[1].name, 'beta');
      expect(result[2].name, 'gamma');
    });

    test('name sort is case-insensitive', () {
      final c = _containerWith([_g(name: 'Zebra'), _g(name: 'apple')]);
      c.read(processSortModeProvider.notifier).state = ProcessSortMode.name;
      final result = c.read(filteredProcessesProvider);
      expect(result[0].name, 'apple');
      expect(result[1].name, 'Zebra');
    });
  });

  group('Status filter', () {
    final groups = [
      _g(name: 'running_a', status: 'RUNNING'),
      _g(name: 'sleeping_a', status: 'SLEEPING'),
      _g(name: 'running_b', status: 'RUNNING'),
    ];

    test('ALL shows everything', () {
      final c = _containerWith(groups);
      c.read(processStatusFilterProvider.notifier).state =
          ProcessStatusFilter.all;
      expect(c.read(filteredProcessesProvider).length, 3);
    });

    test('RUNNING shows only running', () {
      final c = _containerWith(groups);
      c.read(processStatusFilterProvider.notifier).state =
          ProcessStatusFilter.running;
      final result = c.read(filteredProcessesProvider);
      expect(result.length, 2);
      expect(result.every((g) => g.dominantStatus == 'RUNNING'), isTrue);
    });

    test('SLEEPING shows only sleeping', () {
      final c = _containerWith(groups);
      c.read(processStatusFilterProvider.notifier).state =
          ProcessStatusFilter.sleeping;
      final result = c.read(filteredProcessesProvider);
      expect(result.length, 1);
      expect(result.first.name, 'sleeping_a');
    });
  });

  group('showAllProcessesProvider cap', () {
    test('false caps at 100 results', () {
      final c = _containerWith(
        List.generate(150, (i) => _g(name: 'proc_$i', pid: i + 1)),
      );
      c.read(showAllProcessesProvider.notifier).state = false;
      expect(c.read(filteredProcessesProvider).length, 100);
    });

    test('true returns all results past 100', () {
      final c = _containerWith(
        List.generate(150, (i) => _g(name: 'proc_$i', pid: i + 1)),
      );
      c.read(showAllProcessesProvider.notifier).state = true;
      expect(c.read(filteredProcessesProvider).length, 150);
    });
  });

  group('Combined filter + sort', () {
    test('filter then sort works together', () {
      final c = _containerWith([
        _g(name: 'chrome', cpu: 5.0, pid: 1),
        _g(name: 'chrome_elf', cpu: 20.0, pid: 2),
        _g(name: 'devenv', cpu: 15.0, pid: 3),
      ]);
      c.read(processFilterProvider.notifier).state = 'chrome';
      c.read(processSortModeProvider.notifier).state = ProcessSortMode.cpu;
      final result = c.read(filteredProcessesProvider);
      expect(result.length, 2);
      expect(result[0].name, 'chrome_elf'); // 20% CPU first
      expect(result[1].name, 'chrome');
    });
  });
}

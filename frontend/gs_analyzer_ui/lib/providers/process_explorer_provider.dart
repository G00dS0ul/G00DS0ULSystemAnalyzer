import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';
import 'package:flutter_riverpod/legacy.dart';

enum ProcessSortMode { cpu, ram, pid, name }

enum ProcessStatusFilter { all, running, sleeping }

final processFilterProvider = StateProvider<String>((ref) => '');
final processSortModeProvider = StateProvider<ProcessSortMode>(
  (ref) => ProcessSortMode.cpu,
);
final processStatusFilterProvider = StateProvider<ProcessStatusFilter>(
  (ref) => ProcessStatusFilter.all,
);
final selectedProcessPidProvider = StateProvider<int?>((ref) => null);
final showAllProcessesProvider = StateProvider<bool>((ref) => false);

final filteredProcessesProvider = Provider<List<ProcessGroup>>((ref) {
  final groups = ref.watch(ramProvider).groupedProcesses;
  final filter = ref.watch(processFilterProvider).toLowerCase().trim();
  final sort = ref.watch(processSortModeProvider);
  final status = ref.watch(processStatusFilterProvider);
  final showAll = ref.watch(showAllProcessesProvider);

  var result = groups.where((g) {
    final matchesName =
        filter.isEmpty ||
        g.name.toLowerCase().contains(filter) ||
        g.primaryPid.toString().contains(filter);

    final matchesStatus =
        status == ProcessStatusFilter.all ||
        (status == ProcessStatusFilter.running &&
            g.dominantStatus == 'RUNNING') ||
        (status == ProcessStatusFilter.sleeping &&
            g.dominantStatus == 'SLEEPING');

    return matchesName && matchesStatus;
  }).toList();

  result.sort(
    (a, b) => switch (sort) {
      ProcessSortMode.cpu => () {
        final c = b.totalCpuPercent.compareTo(a.totalCpuPercent);
        return c != 0 ? c : b.totalRamMb.compareTo(a.totalRamMb);
      }(),
      ProcessSortMode.ram => b.totalRamMb.compareTo(a.totalRamMb),
      ProcessSortMode.pid => a.primaryPid.compareTo(b.primaryPid),
      ProcessSortMode.name => a.name.toLowerCase().compareTo(
        b.name.toLowerCase(),
      ),
    },
  );

  return showAll ? result : result.take(100).toList();
});

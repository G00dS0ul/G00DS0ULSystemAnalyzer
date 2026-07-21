import 'package:gs_analyzer_ui/utils/logger.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';

import '../services/api_service.dart';

class RamState {
  final List<ProcessGroup> groupedProcesses;
  final bool isLoading;
  final double activeGb;
  final double cacheGb;
  final double swapGb;
  final double totalSwapGb;
  final double totalGb;
  final List<double> usageHistory;
  final int ramThresholdPercent;

  const RamState({
    this.groupedProcesses = const [],
    this.isLoading = true,
    this.activeGb = 0.0,
    this.cacheGb = 0.0,
    this.swapGb = 0.0,
    this.totalSwapGb = 0.0,
    this.totalGb = 16.0, // fix: hard coded total ram size
    this.usageHistory = const [],
    this.ramThresholdPercent = 85,
  });

  double get usedPercentage => totalGb > 0 ? activeGb / totalGb : 0.0;
  String get displayedString =>
      '${activeGb.toStringAsFixed(2)} GB / ${totalGb.toStringAsFixed(2)} GB';
  bool get isCritical =>
      totalGb > 0 && (activeGb / totalGb) >= (ramThresholdPercent / 100.0);

  RamState copyWith({
    List<ProcessGroup>? groupedProcesses,
    bool? isLoading,
    double? activeGb,
    double? cacheGb,
    double? swapGb,
    double? totalSwapGb,
    double? totalGb,
    List<double>? usageHistory,
    int? ramThresholdPercent,
  }) {
    return RamState(
      groupedProcesses: groupedProcesses ?? this.groupedProcesses,
      isLoading: isLoading ?? this.isLoading,
      activeGb: activeGb ?? this.activeGb,
      cacheGb: cacheGb ?? this.cacheGb,
      swapGb: swapGb ?? this.swapGb,
      totalSwapGb: totalSwapGb ?? this.totalSwapGb,
      totalGb: totalGb ?? this.totalGb,
      usageHistory: usageHistory ?? this.usageHistory,
      ramThresholdPercent: ramThresholdPercent ?? this.ramThresholdPercent,
    );
  }
}

class RamNotifier extends StateNotifier<RamState> {
  final ApiService _apiService = ApiService();
  final Ref ref;

  RamNotifier(this.ref) : super(const RamState()) {
    _apiService.startRamRadar();
    _listenToSettings();
  }

  void _listenToSettings() {
    ref.listen(settingsProvider, (previous, next) {
      final newThreshold = next.currentSettings?.alerts.ramThresholdPercent;
      if (newThreshold != null && newThreshold != state.ramThresholdPercent) {
        state = state.copyWith(ramThresholdPercent: newThreshold);
      }
    }, fireImmediately: true);
  }

  void updateProcesses(Map<String, dynamic> payload) {
    final global = payload['global'] ?? payload['Global'] ?? {};

    // appLogger.i('🦅 MATRIX RADAR - Global Memory Payload: $global');

    double parseGb(String lowerKey, String upperKey, double fallback) {
      final val = global[lowerKey] ?? global[upperKey] ?? fallback;
      return (val as num).toDouble();
    }

    final totalGb = parseGb('totalGb', 'TotalGb', 16.0);
    final totalMb = totalGb * 1024.0;

    final rawProcesses = payload['processes'] as List<dynamic>? ?? [];
    final parsed = rawProcesses
        .map((p) => ProcessTelemetry.fromJson(p, totalMb))
        .toList();

    final Map<String, List<ProcessTelemetry>> groupsMap = {};
    for (var p in parsed) {
      if (!groupsMap.containsKey(p.name)) {
        groupsMap[p.name] = [];
      }
      groupsMap[p.name]!.add(p);
    }

    final groupedList = groupsMap.entries
        .map((e) => ProcessGroup(name: e.key, processes: e.value))
        .toList();

    groupedList.sort((a, b) {
      return b.totalRamMb.compareTo(a.totalRamMb);
    });

    final parsedActiveGb = (global['activeGb'] ?? 0.0).toDouble();

    final currentPercentage = totalGb > 0 ? parsedActiveGb / totalGb : 0.0;
    final updatedHistory = List<double>.from(state.usageHistory)
      ..add(currentPercentage);

    if (updatedHistory.length > 20) {
      updatedHistory.removeAt(0);
    }

    state = state.copyWith(
      groupedProcesses: groupedList,
      isLoading: false,
      activeGb: parsedActiveGb,
      cacheGb: (global['cacheGb'] ?? global['CacheGb'] ?? 0.0).toDouble(),
      swapGb: (global['swapGb'] ?? global['SwapGb'] ?? 0.0).toDouble(),
      totalSwapGb:
          (global['totalSwapGb'] ?? global['TotalSwapGb'] ?? (totalGb * 2))
              .toDouble(),
      totalGb: totalGb,
      usageHistory: updatedHistory,
    );
  }

  Future<void> killProcess(int pid) async {
    try {
      state = state.copyWith(
        groupedProcesses: state.groupedProcesses
            .map((group) {
              return ProcessGroup(
                name: group.name,
                processes: group.processes
                    .where((process) => process.pid != pid)
                    .toList(),
              );
            })
            .where((group) => group.processes.isNotEmpty)
            .toList(),
      );

      await _apiService.killRamProcesses([pid]);

      appLogger.i('EXECUTE ORDER 66 ON PID: $pid - SUCCESS');
    } catch (e) {
      appLogger.i('Failed to kill process: $e');
    }
  }

  Future<void> killProcessGroup(String groupName) async {
    try {
      final targetGroup = state.groupedProcesses.firstWhere(
        (g) => g.name == groupName,
      );
      final pidsToKill = targetGroup.processes.map((p) => p.pid).toList();

      state = state.copyWith(
        groupedProcesses: state.groupedProcesses
            .where((g) => g.name != groupName)
            .toList(),
      );

      await _apiService.killRamProcesses(pidsToKill);
    } catch (e) {
      appLogger.i('Failed to kill process group: $e');
    }
  }

  void debugSetGroupsForTest(List<ProcessGroup> groups) {
    assert(() {
      return true;
    }(), 'debug only');
    state = state.copyWith(groupedProcesses: groups);
  }
}

final ramProvider = StateNotifierProvider<RamNotifier, RamState>((ref) {
  return RamNotifier(ref);
});

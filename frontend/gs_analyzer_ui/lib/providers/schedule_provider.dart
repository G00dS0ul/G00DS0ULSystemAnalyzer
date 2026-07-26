import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/scheduled_scan_model.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/utils/logger.dart';

class ScheduleNotifier extends AsyncNotifier<List<ScheduledScan>> {
  final ApiService _api = ApiService();

  @override
  FutureOr<List<ScheduledScan>> build() async {
    return fetchSchedules();
  }

  Future<List<ScheduledScan>> fetchSchedules() async {
    try {
      return await _api.getSchedules();
    } catch (e) {
      appLogger.e('Failed to load schedules: $e');
      throw Exception('Failed to load schedules: $e');
    }
  }

  Future<void> reload() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(() => fetchSchedules());
  }

  void updateFromSignalR(List<dynamic> jsonList) {
    try {
      final schedules = jsonList.map((j) => ScheduledScan.fromJson(j)).toList();
      state = AsyncValue.data(schedules);
    } catch (e) {
      appLogger.e('Failed to parse schedules from SignalR: $e');
    }
  }

  Future<void> createSchedule(Map<String, dynamic> data) async {
    try {
      await _api.createSchedule(data);
      // The backend will broadcast ScheduleUpdate, which will update the state via SignalR.
      // But we can also proactively reload just in case.
      await reload();
    } catch (e) {
      appLogger.e('Failed to create schedule: $e');
      rethrow;
    }
  }

  Future<void> updateSchedule(String id, Map<String, dynamic> data) async {
    try {
      await _api.updateSchedule(id, data);
      await reload();
    } catch (e) {
      appLogger.e('Failed to update schedule: $e');
      rethrow;
    }
  }

  Future<void> toggleEnabled(String id, bool enabled) async {
    return updateSchedule(id, {'enabled': enabled});
  }

  Future<void> deleteSchedule(String id) async {
    try {
      await _api.deleteSchedule(id);
      await reload();
    } catch (e) {
      appLogger.e('Failed to delete schedule: $e');
      rethrow;
    }
  }

  Future<void> runNow(String id) async {
    try {
      await _api.runScheduleNow(id);
    } catch (e) {
      appLogger.e('Failed to run schedule now: $e');
      rethrow;
    }
  }
}

final scheduleProvider = AsyncNotifierProvider<ScheduleNotifier, List<ScheduledScan>>(
  () => ScheduleNotifier(),
);

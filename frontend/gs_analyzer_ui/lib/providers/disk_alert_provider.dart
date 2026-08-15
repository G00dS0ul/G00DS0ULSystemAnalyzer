import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/disk_alert.dart';

final diskAlertsProvider =
    NotifierProvider<DiskAlertsNotifier, Map<String, DiskAlert>>(() {
  return DiskAlertsNotifier();
});

class DiskAlertsNotifier extends Notifier<Map<String, DiskAlert>> {
  @override
  Map<String, DiskAlert> build() => {};

  void handleDiskAlert(DiskAlert alert) {
    if (alert.driveName.isEmpty) return;
    state = {...state, alert.driveName: alert};
  }

  void handleDiskAlertCleared(String driveName) {
    if (state.containsKey(driveName)) {
      final updated = Map<String, DiskAlert>.from(state);
      updated.remove(driveName);
      state = updated;
    }
  }

  void pruneDrives(List<String> activeDriveNames) {
    final activeSet = activeDriveNames.toSet();
    final updated = Map<String, DiskAlert>.from(state)
      ..removeWhere((k, v) => !activeSet.contains(k));
    if (updated.length != state.length) {
      state = updated;
    }
  }

  void clearAll() {
    state = {};
  }
}

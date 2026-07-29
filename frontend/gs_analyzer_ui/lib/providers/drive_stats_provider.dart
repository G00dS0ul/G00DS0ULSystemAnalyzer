import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/drive_info.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final drivesProvider = NotifierProvider<DrivesNotifier, List<DriveInfo>>(() {
  return DrivesNotifier();
});

class DrivesNotifier extends Notifier<List<DriveInfo>> {
  @override
  List<DriveInfo> build() {
    Future.microtask(() => refresh());
    return [];
  }

  Future<void> refresh() async {
    final api = ApiService();
    final initialData = await api.getDrives();
    if (initialData != null) {
      state = initialData.map((item) {
        return DriveInfo.fromJson(Map<String, dynamic>.from(item as Map));
      }).toList();
    }
  }

  void updateFromTelemetry(List<dynamic> data) {
    state = data.map((item) {
      return DriveInfo.fromJson(Map<String, dynamic>.from(item as Map));
    }).toList();
  }
}

final selectedDriveNameProvider = StateProvider<String?>((ref) => null);

final currentDriveProvider = Provider<DriveInfo?>((ref) {
  final drives = ref.watch(drivesProvider);
  if (drives.isEmpty) return null;

  final selectedName = ref.watch(selectedDriveNameProvider);

  if (selectedName == null) {
    return drives.firstWhere(
      (d) => d.type == 'fixed',
      orElse: () => drives.first,
    );
  }

  return drives.firstWhere(
    (d) => d.name == selectedName,
    orElse: () => drives.first,
  );
});

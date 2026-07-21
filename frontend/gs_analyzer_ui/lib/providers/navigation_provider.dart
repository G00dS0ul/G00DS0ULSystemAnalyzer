import 'package:flutter_riverpod/legacy.dart';

enum AppRoute {
  dashboard,
  process,
  cpuMetics,
  memory,
  storage,
  startup,
  network,
  thermal,
  telemetryHistory,
  settings,
}

final navigationProvider = StateProvider<AppRoute>((ref) => AppRoute.process);

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/scan_diff_model.dart';
import 'package:gs_analyzer_ui/providers/telemetry_history_provider.dart';

final scanDiffProvider =
    FutureProvider.autoDispose.family<ScanDiff, String>((ref, root) async {
  final api = ref.read(apiServiceProvider);
  return api.getScanDiff(root);
});

// Whether the WHAT_CHANGED panel is expanded (persisted per session)
final diffPanelExpandedProvider = StateProvider<bool>((ref) => true);

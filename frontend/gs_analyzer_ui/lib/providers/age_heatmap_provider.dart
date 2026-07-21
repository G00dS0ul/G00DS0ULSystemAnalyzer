import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/age_heatmap_model.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

/// Exception thrown when the backend returns 409 (no scan cached).
class AgeHeatmapNoScanException implements Exception {
  const AgeHeatmapNoScanException();
}

/// Toggle: whether the age heatmap overlay is active.
final ageHeatmapEnabledProvider = StateProvider<bool>((ref) => false);

/// Fetches age heatmap data for [root].
/// Throws [AgeHeatmapNoScanException] when no Directory scan has run yet.
/// Not auto-disposed — data persists across toggle ON/OFF cycles for the session.
final ageHeatmapProvider = FutureProvider.family<AgeHeatmapResult, String>((
  ref,
  root,
) async {
  return ApiService().getAgeHeatmap(root);
});

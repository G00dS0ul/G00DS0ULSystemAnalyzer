import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/scan_diff.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final apiServiceProvider = Provider<ApiService>((ref) => ApiService());
final scanDiffProvider = FutureProvider.autoDispose.family<ScanDiff, String>((
  ref,
  root,
) async {
  final api = ref.read(apiServiceProvider);
  return api.getScanDiff(root);
});

final diffChangeCountProvider = Provider.autoDispose.family<int, String>((
  ref,
  root,
) {
  final summary = ref.watch(scanDiffProvider(root)).asData?.value.summary;
  return summary?.totalChangeCount ?? 0;
});

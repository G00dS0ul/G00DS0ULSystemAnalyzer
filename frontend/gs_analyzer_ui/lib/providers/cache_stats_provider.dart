import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/cache_stats.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final cacheStatsApiProvider = Provider<ApiService>((ref) => ApiService());

final cacheStatsProvider = FutureProvider.autoDispose<CacheStats?>((ref) async {
  final api = ref.watch(cacheStatsApiProvider);
  return await api.getCacheStats();
});

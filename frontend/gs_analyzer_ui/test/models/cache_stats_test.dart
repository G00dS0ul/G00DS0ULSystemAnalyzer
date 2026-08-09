import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/cache_stats.dart';

void main() {
  group('CacheStats Model', () {
    test('parses from backend json with camelCase and PascalCase', () {
      final json = {
        'activeEntries': 12481,
        'rootCount': 3,
        'approximateMemoryBytes': 88080384, // ~84 MB
        'hitCount': 910,
        'missCount': 90,
        'hitRate': 0.91,
        'oldestCachedAt': '2026-08-07T12:00:00Z',
      };

      final stats = CacheStats.fromJson(json);

      expect(stats.activeEntries, 12481);
      expect(stats.rootCount, 3);
      expect(stats.approximateMemoryBytes, 88080384);
      expect(stats.hitCount, 910);
      expect(stats.missCount, 90);
      expect(stats.hitRate, 0.91);
      expect(stats.formattedNodes, '12,481');
      expect(stats.formattedMemory, '84 MB');
      expect(stats.formattedHitRate, '91%');
      expect(stats.oldestCachedAt, isNotNull);
    });

    test('formats small memory correctly as KB', () {
      final json = {
        'activeEntries': 5,
        'rootCount': 1,
        'approximateMemoryBytes': 512000,
        'hitCount': 0,
        'missCount': 1,
        'hitRate': 0.0,
      };

      final stats = CacheStats.fromJson(json);
      expect(stats.formattedNodes, '5');
      expect(stats.formattedMemory, '500 KB');
      expect(stats.formattedHitRate, '0%');
    });

    test('parses real backend data envelope correctly', () {
      final json = {
        'success': true,
        'data': {
          'entryCount': 26085,
          'nodeCount': 26083,
          'rootCount': 2,
          'approximateBytes': 137213053154,
          'hitCount': 104545,
          'missCount': 0,
          'hitMissRatio': 1.0,
          'oldestCachedAt': '2026-08-07T20:41:42.3288544+00:00'
        }
      };

      final stats = CacheStats.fromJson(json);
      expect(stats.activeEntries, 26083);
      expect(stats.rootCount, 2);
      expect(stats.approximateMemoryBytes, 137213053154);
      expect(stats.hitCount, 104545);
      expect(stats.missCount, 0);
      expect(stats.hitRate, 1.0);
      expect(stats.formattedNodes, '26,083');
      expect(stats.formattedMemory, '127.8 GB');
      expect(stats.formattedHitRate, '100%');
    });
  });
}

import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/age_heatmap_model.dart';

void main() {
  AgeHeatmapNode makeNode(String path, String bucket) => AgeHeatmapNode(
    path: path,
    sizeBytes: 1024,
    ageBucket: bucket,
    lastModified: DateTime.now(),
  );

  AgeHeatmapResult makeResult(List<AgeHeatmapNode> nodes) =>
      AgeHeatmapResult(root: 'C:/root', nodes: nodes, summary: {});

  group('AgeHeatmapResult.lookupByPath', () {
    test('finds node with forward-slash path', () {
      final result = makeResult([makeNode('C:/root/docs', 'FRESH')]);
      expect(result.lookupByPath['C:/root/docs'], isNotNull);
    });

    test('finds node when lookup key uses backslash', () {
      final result = makeResult([makeNode('C:/root/docs', 'FRESH')]);
      // Simulate Windows path coming from DirectoryNodeWidget
      final key = r'C:\root\docs'.replaceAll('\\', '/');
      expect(result.lookupByPath[key], isNotNull);
    });

    test('returns null for path not in result', () {
      final result = makeResult([makeNode('C:/root/docs', 'FRESH')]);
      expect(result.lookupByPath['C:/root/other'], isNull);
    });

    test('trailing slash does not break lookup', () {
      final result = makeResult([makeNode('C:/root/docs', 'FRESH')]);
      final key = 'C:/root/docs/'.replaceAll(RegExp(r'/$'), '');
      expect(result.lookupByPath[key], isNotNull);
    });
  });

  group('AgeHeatmapResult.fromJson', () {
    test('parses full response correctly', () {
      final json = {
        'root': 'C:/root',
        'nodes': [
          {
            'path': 'C:/root/file.txt',
            'sizeBytes': 2048,
            'ageBucket': 'AGING',
            'lastModified': '2024-01-01T00:00:00Z',
          },
        ],
        'summary': {
          'AGING': {'count': 1, 'totalBytes': 2048},
        },
      };

      final result = AgeHeatmapResult.fromJson(json);

      expect(result.root, 'C:/root');
      expect(result.nodes.length, 1);
      expect(result.nodes[0].ageBucket, 'AGING');
      expect(result.summary['AGING']!.totalBytes, 2048);
    });

    test('parses empty nodes list without error', () {
      final json = {
        'root': 'C:/root',
        'nodes': <dynamic>[],
        'summary': <String, dynamic>{},
      };
      final result = AgeHeatmapResult.fromJson(json);
      expect(result.nodes, isEmpty);
    });
  });
}

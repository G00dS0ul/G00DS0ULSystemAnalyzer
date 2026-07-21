class AgeHeatmapNode {
  final String path;
  final int sizeBytes;
  final String ageBucket;
  final DateTime lastModified;

  const AgeHeatmapNode({
    required this.path,
    required this.sizeBytes,
    required this.ageBucket,
    required this.lastModified,
  });

  factory AgeHeatmapNode.fromJson(Map<String, dynamic> j) => AgeHeatmapNode(
    path: j['path'] as String,
    sizeBytes: j['sizeBytes'] as int,
    ageBucket: j['ageBucket'] as String,
    lastModified: DateTime.parse(j['lastModified'] as String),
  );
}

class AgeBucketSummary {
  final int count;
  final int totalBytes;

  const AgeBucketSummary({required this.count, required this.totalBytes});

  factory AgeBucketSummary.fromJson(Map<String, dynamic> j) => AgeBucketSummary(
    count: j['count'] as int,
    totalBytes: j['totalBytes'] as int,
  );
}

class AgeHeatmapResult {
  final String root;
  final List<AgeHeatmapNode> nodes;
  final Map<String, AgeBucketSummary> summary;

  AgeHeatmapResult({
    required this.root,
    required this.nodes,
    required this.summary,
  });

  factory AgeHeatmapResult.fromJson(Map<String, dynamic> j) => AgeHeatmapResult(
    root: j['root'] as String,
    nodes: (j['nodes'] as List)
        .map((e) => AgeHeatmapNode.fromJson(e as Map<String, dynamic>))
        .toList(),
    summary: (j['summary'] as Map<String, dynamic>).map(
      (key, value) => MapEntry(
        key,
        AgeBucketSummary.fromJson(value as Map<String, dynamic>),
      ),
    ),
  );

  /// O(1) lookup map keyed by normalized path (forward slashes, lowercase).
  late final Map<String, AgeHeatmapNode> lookupByPath = {
    for (final node in nodes)
      node.path.replaceAll('\\', '/').toLowerCase(): node,
  };
}

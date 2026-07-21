class MockFactory {
  static Map<String, dynamic> createFakeNodeMap({
    required String name,
    required String path,
    bool isDirectory = false,
    int size = 1024,
  }) {
    return {
      'name': name,
      'path': path,
      'type': isDirectory ? 'Directory' : 'File',
      'sizeBytes': size,
      'lastModified': DateTime.now().toIso8601String(),
    };
  }

  static List<Map<String, dynamic>> generateFakeChunk(
    int count,
    String basePath,
  ) {
    return List.generate(count, (index) {
      return createFakeNodeMap(
        name: 'item_$index.bin',
        path: '$basePath/item_$index.bin',
        size: (index + 1) * 1000,
      );
    });
  }

  static List<Map<String, dynamic>> generateNukeTarget(
    int count,
    String targetPath,
  ) {
    return List.generate(count, (index) {
      return {
        'name': 'target_item_$index.tmp',
        'path': '$targetPath/target_item_$index.tmp',
        'type': index % 2 == 0 ? 'File' : 'Directory',
        'sizeBytes': 1024 * index,
        'lastModified': DateTime.now().toIso8601String(),
        'status': 'PendingDeletion',
      };
    });
  }

  static Map<String, dynamic> generateRamTelemetry({
    required double activeGb,
    double totalGb = 16.0,
  }) {
    return {
      'global': {
        'totalGb': totalGb,
        'activeGb': activeGb,
        'cacheGb': 1.5,
        'swapGb': 0.0,
      },
      'processes': [],
    };
  }
}

import 'dart:io';

class DuplicateItem {
  final String path;
  final DateTime lastModified;
  final int sizeBytes;
  bool isSelected;

  DuplicateItem({
    required this.path,
    required this.lastModified,
    required this.sizeBytes,
    this.isSelected = false,
  });

  factory DuplicateItem.fromPath(String filePath) {
    final file = File(filePath);
    final stat = file.statSync();
    return DuplicateItem(
      path: filePath,
      lastModified: stat.modified,
      sizeBytes: stat.size,
    );
  }
}

class DuplicateGroup {
  final String fileHash;
  final List<DuplicateItem> files;

  DuplicateGroup({required this.fileHash, required this.files});

  int get wastedSizeBytes {
    if (files.isEmpty) return 0;
    return files[0].sizeBytes * (files.length - 1);
  }
}

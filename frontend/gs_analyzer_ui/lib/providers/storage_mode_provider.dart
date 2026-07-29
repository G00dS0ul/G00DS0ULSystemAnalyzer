import 'package:flutter_riverpod/legacy.dart';

enum StorageMode {
  diskAnalyzer,
  duplicateScanner,
  largeFileScanner,
  tempFileCleaner,
  permissionAudit,
}

final storageModeProvider = StateProvider<StorageMode>(
  (ref) => StorageMode.diskAnalyzer,
);

import 'package:gs_analyzer_ui/utils/logger.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/duplicate_model.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';

class DuplicateState {
  final bool isLoading;
  final List<DuplicateGroup> duplicateGroups;
  final bool skipHiddenFiles;
  final bool skipSystemFiles;

  DuplicateState({
    this.isLoading = false,
    this.duplicateGroups = const [],
    this.skipHiddenFiles = true,
    this.skipSystemFiles = true,
  });

  String get totalWastedSpaceFormatted {
    int totalBytes = duplicateGroups.fold(
      0,
      (sum, group) => sum + group.wastedSizeBytes,
    );
    if (totalBytes >= 1024 * 1024 * 1024) {
      return '${(totalBytes / (1024 * 1024 * 1024)).toStringAsFixed(2)} GB';
    }
    return '${(totalBytes / (1024 * 1024)).toStringAsFixed(2)} MB';
  }

  List<String> get pathsToNuke {
    return duplicateGroups
        .expand((group) => group.files)
        .where((file) => file.isSelected)
        .map((file) => file.path)
        .toList();
  }

  DuplicateState copyWith({
    bool? isLoading,
    List<DuplicateGroup>? duplicateGroups,
    bool? skipHiddenFiles,
    bool? skipSystemFiles,
  }) {
    return DuplicateState(
      isLoading: isLoading ?? this.isLoading,
      duplicateGroups: duplicateGroups ?? this.duplicateGroups,
      skipHiddenFiles: skipHiddenFiles ?? this.skipHiddenFiles,
      skipSystemFiles: skipSystemFiles ?? this.skipSystemFiles,
    );
  }
}

class DuplicateNotifier extends StateNotifier<DuplicateState> {
  final Ref ref;

  DuplicateNotifier(this.ref) : super(DuplicateState()) {
    _listenToSettings();
  }

  void _listenToSettings() {
    ref.listen(settingsProvider, (previous, next) {
      final scanSettings = next.currentSettings?.scan;
      if (scanSettings != null) {
        state = state.copyWith(
          skipHiddenFiles: scanSettings.skipHiddenFiles,
          skipSystemFiles: scanSettings.skipSystemFiles,
        );
      }
    }, fireImmediately: true);
  }

  Future<void> startScan(String rootPath) async {
    state = state.copyWith(isLoading: true, duplicateGroups: []);

    try {
      final apiService = ApiService();
      final rawData = await apiService.scanForDuplicates(
        rootPath,
        generateUuid(),
      );

      loadFromBackend(rawData);
    } catch (e) {
      appLogger.i('DUPLICATE SCAN CRASHED: $e');
      state = state.copyWith(isLoading: false, duplicateGroups: []);
    }
  }

  void loadFromBackend(List<dynamic> jsonList) {
    List<DuplicateGroup> parsedGroups = [];

    for (var item in jsonList) {
      final hash = item['FileHash'] ?? item['fileHash'];
      final List<dynamic> rawPaths = item['FilePaths'] ?? item['filePaths'];
      final List<dynamic> paths = rawPaths;

      final items = paths
          .map((p) => DuplicateItem.fromPath(p.toString()))
          .toList();

      parsedGroups.add(DuplicateGroup(fileHash: hash, files: items));
    }

    state = state.copyWith(isLoading: false, duplicateGroups: parsedGroups);
  }

  void smartSelectAll() {
    final updateGroups = state.duplicateGroups.map((group) {
      group.files.sort((a, b) => a.lastModified.compareTo(b.lastModified));

      for (int i = 0; i < group.files.length; i++) {
        group.files[i].isSelected = (i != 0);
      }
      return group;
    }).toList();

    state = state.copyWith(duplicateGroups: updateGroups);
  }

  void clearNukedFiles() {
    final updatedGroups = state.duplicateGroups
        .map((group) {
          group.files.removeWhere((file) => file.isSelected);
          return group;
        })
        .where((group) => group.files.length > 1)
        .toList();

    state = state.copyWith(isLoading: false, duplicateGroups: updatedGroups);
  }

  void toggleFileSelection(String hash, String path) {
    final updatedGroups = state.duplicateGroups.map((group) {
      if (group.fileHash == hash) {
        for (var file in group.files) {
          if (file.path == path) {
            file.isSelected = !file.isSelected;
          }
        }
      }
      return group;
    }).toList();

    state = state.copyWith(duplicateGroups: updatedGroups);
  }

  void abortScan() {
    state = state.copyWith(isLoading: false, duplicateGroups: []);

    ApiService().abortScan();
  }
}

final duplicateProvider =
    StateNotifierProvider<DuplicateNotifier, DuplicateState>(
      (ref) => DuplicateNotifier(ref),
    );

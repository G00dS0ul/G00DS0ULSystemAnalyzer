import 'package:gs_analyzer_ui/utils/logger.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/large_file_model.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class LargeFileState {
  final bool isLoading;
  final List<LargeFileModel> largeFiles;
  final String? errorMessage;
  final bool skipHiddenFiles;
  final bool skipSystemFiles;

  LargeFileState({
    this.isLoading = false,
    this.largeFiles = const [],
    this.errorMessage,
    this.skipHiddenFiles = true,
    this.skipSystemFiles = true,
  });

  LargeFileState copyWith({
    bool? isLoading,
    List<LargeFileModel>? largeFiles,
    String? errorMessage,
    bool? skipHiddenFiles,
    bool? skipSystemFiles,
  }) {
    return LargeFileState(
      isLoading: isLoading ?? this.isLoading,
      largeFiles: largeFiles ?? this.largeFiles,
      errorMessage: errorMessage ?? this.errorMessage,
      skipHiddenFiles: skipHiddenFiles ?? this.skipHiddenFiles,
      skipSystemFiles: skipSystemFiles ?? this.skipSystemFiles,
    );
  }
}

class LargeFileNotifier extends StateNotifier<LargeFileState> {
  final Ref ref;

  LargeFileNotifier(this.ref) : super(LargeFileState()) {
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

  Future<void> startScan(String rootPath, int topN) async {
    state = state.copyWith(isLoading: true, largeFiles: []);

    try {
      final apiService = ApiService();
      final rawData = await apiService.scanForLargeFiles(rootPath, topN);

      final parsedFiles = rawData
          .map((json) => LargeFileModel.fromJson(json))
          .toList();

      state = state.copyWith(isLoading: false, largeFiles: parsedFiles);
    } catch (e) {
      appLogger.i('LARGE FILE SCAN CRACHED: $e');
      state = state.copyWith(
        isLoading: false,
        largeFiles: [],
        errorMessage: e.toString(),
      );
    }
  }

  void removeNukeFiles(List<String> nukePaths) {
    final updatedList = state.largeFiles
        .where((file) => !nukePaths.contains(file.path))
        .toList();
    state = state.copyWith(isLoading: false, largeFiles: updatedList);
  }

  void abortScan() {
    state = state.copyWith(
      isLoading: false,
      largeFiles: [],
      errorMessage: 'SCAN ABORTED BY USER',
    );

    ApiService().abortScan();
  }
}

final largeFileProvider =
    StateNotifierProvider<LargeFileNotifier, LargeFileState>(
      (ref) => LargeFileNotifier(ref),
    );

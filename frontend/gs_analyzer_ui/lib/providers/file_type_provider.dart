import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/file_type_model.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class FileTypeNoScanException implements Exception {
  const FileTypeNoScanException();
}

final selectedCategoryProvider = StateProvider<String?>((ref) => null);

/// Fetches file type breakdown for [root].
/// Throws [FileTypeNoScanException] when no Directory scan has run yet.
final fileTypesProvider = FutureProvider.autoDispose
    .family<FileTypeResult, String>((ref, root) async {
      return ApiService().getFileTypes(root);
    });

final scanRootProvider = StateProvider.family<String, String>(
  (ref, driveName) => driveName,
);

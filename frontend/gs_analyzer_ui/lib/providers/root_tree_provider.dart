import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'dart:math';

String generateUuid() {
  final random = Random();
  final chars = '0123456789abcdef';
  String randomString(int length) => String.fromCharCodes(
    Iterable.generate(length, (_) => chars.codeUnitAt(random.nextInt(16))),
  );
  return '${randomString(8)}-${randomString(4)}-4${randomString(3)}-a${randomString(3)}-${randomString(12)}';
}

final rootTreeProvider = FutureProvider<List<StorageNode>>((ref) async {
  return ApiService().scanDirectory('C:/', generateUuid());
});

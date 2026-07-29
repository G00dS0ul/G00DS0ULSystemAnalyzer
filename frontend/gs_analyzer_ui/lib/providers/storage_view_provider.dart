import 'package:flutter_riverpod/legacy.dart';

enum StorageView { drivePicker, analyzer }

final storageViewProvider = StateProvider<StorageView>(
  (ref) => StorageView.drivePicker,
);

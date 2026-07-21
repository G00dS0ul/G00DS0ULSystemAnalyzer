import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/permission_audit_models.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/telemetry_provider.dart';

final apiServiceProvider = Provider((ref) => ApiService());

final auditProgressProvider = StateProvider<Map<String, dynamic>?>(
  (ref) => null,
);

final permissionAuditProvider =
    NotifierProvider<
      PermissionAuditNotifier,
      AsyncValue<PermissionAuditResult?>
    >(() {
      return PermissionAuditNotifier();
    });

class PermissionAuditNotifier
    extends Notifier<AsyncValue<PermissionAuditResult?>> {
  @override
  AsyncValue<PermissionAuditResult?> build() {
    return const AsyncValue.data(null);
  }

  Future<void> runAudit(String rootPath) async {
    ref.read(auditProgressProvider.notifier).state = {
      'scanned': 0,
      'issues': 0,
    };
    state = const AsyncValue.loading();

    // Subscribe to live telemetry
    final telemetry = ref.read(telemetryProvider.notifier).service;
    if (telemetry != null) {
      telemetry.onAuditProgress = (progress) {
        ref.read(auditProgressProvider.notifier).state = progress;
      };
    }

    try {
      final apiService = ref.read(apiServiceProvider);
      final result = await apiService.auditPermissions(rootPath);
      state = AsyncValue.data(result);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    } finally {
      if (telemetry != null) {
        telemetry.onAuditProgress = null; // Unsubscribe
      }
    }
  }

  void cancelAudit() {
    ref.read(apiServiceProvider).cancelAudit();
  }

  void reset() {
    state = const AsyncValue.data(null);
  }
}

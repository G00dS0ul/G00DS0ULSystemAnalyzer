import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:local_notifier/local_notifier.dart';
import 'package:gs_analyzer_ui/models/disk_alert.dart';
import 'package:gs_analyzer_ui/models/ram_alert.dart';
import 'package:gs_analyzer_ui/utils/logger.dart';

/// Centralized notification service supporting desktop OS toasts for Disk, RAM, Thermal, and other alerts.
class NotificationService {
  static final NotificationService _instance = NotificationService._internal();
  factory NotificationService() => _instance;
  NotificationService._internal();

  bool _isInitialized = false;
  Future<void>? _initFuture;
  void Function(String? payload)? _onSelectPayload;

  bool get isInitialized => _isInitialized;

  void setOnSelectNotification(void Function(String? payload)? callback) {
    _onSelectPayload = callback;
  }

  /// Initializes the desktop local notification service.
  Future<void> initialize({
    void Function(String? payload)? onSelectNotification,
  }) async {
    if (onSelectNotification != null) {
      _onSelectPayload = onSelectNotification;
    }
    if (_isInitialized) return;
    if (_initFuture != null) return _initFuture;

    _initFuture = _doInitialize();
    return _initFuture;
  }

  Future<void> _doInitialize() async {
    try {
      if (!kIsWeb && (Platform.isWindows || Platform.isLinux || Platform.isMacOS)) {
        await localNotifier.setup(
          appName: 'GSAnalyzer',
          shortcutPolicy: ShortcutPolicy.requireCreate,
        );
      }
      _isInitialized = true;
      appLogger.i('NOTIFICATION SERVICE INITIALIZED');
    } catch (e, st) {
      appLogger.w('Failed to initialize NotificationService: $e\n$st');
    }
  }

  /// Dispatches a desktop DiskAlert notification gated strictly on [enableDesktopNotifications].
  Future<void> showDiskAlertNotification(
    DiskAlert alert, {
    required bool enableDesktopNotifications,
  }) async {
    // Gate every notification on alerts.enableDesktopNotifications.
    // A user who turned notifications off must get silence, including the critical tier.
    if (!enableDesktopNotifications) {
      appLogger.d(
        'Notification suppressed: enableDesktopNotifications is false for drive ${alert.driveName}',
      );
      return;
    }

    final title = 'GSAnalyzer — Disk Alert [${alert.severity.toUpperCase()}]';
    final body =
        '${alert.displayName} is ${alert.usedPercent.toStringAsFixed(0)}% full — ${alert.freeFormatted} free';
    final payload = 'storage:${alert.driveName}';

    await showNotification(
      title: title,
      body: body,
      payload: payload,
    );
  }

  /// Dispatches a desktop RamAlert notification gated strictly on [enableDesktopNotifications].
  Future<void> showRamAlertNotification(
    RamAlert alert, {
    required bool enableDesktopNotifications,
  }) async {
    if (!enableDesktopNotifications) {
      appLogger.d(
        'Notification suppressed: enableDesktopNotifications is false for RAM alert',
      );
      return;
    }

    final title = 'GSAnalyzer — Memory Alert [${alert.severity.toUpperCase()}]';
    
    // Base body: MEMORY PRESSURE — 924 MB FREE (88%)
    var body = 'MEMORY PRESSURE — ${alert.availableFormatted} FREE (${alert.usedPercent}%)';
    
    // Expanded body (Top consumers)
    if (alert.topConsumers.isNotEmpty) {
      body += '\n\nTop consumers:';
      for (var consumer in alert.topConsumers) {
        body += '\n• ${consumer.name} (${consumer.ramMb.toStringAsFixed(0)} MB)';
      }
    }
    
    final payload = 'memory';

    await showNotification(
      title: title,
      body: body,
      payload: payload,
    );
  }

  /// Generic notification dispatcher supporting future alerts (RAM, CPU, Thermal).
  Future<void> showNotification({
    int? id,
    required String title,
    required String body,
    String? payload,
    bool enableDesktopNotifications = true,
  }) async {
    if (!enableDesktopNotifications) return;

    try {
      if (!_isInitialized) {
        await initialize();
      }

      final notification = LocalNotification(
        identifier: id?.toString() ?? DateTime.now().millisecondsSinceEpoch.toString(),
        title: title,
        body: body,
        actions: [
          LocalNotificationAction(text: 'View'),
        ],
      );

      notification.onShow = () {
        appLogger.i('NOTIFICATION SHOWN: $title');
      };

      notification.onClick = () {
        appLogger.i('NOTIFICATION TAPPED: payload=$payload');
        _onSelectPayload?.call(payload);
      };

      notification.onClickAction = (actionIndex) {
        appLogger.i('NOTIFICATION ACTION TAPPED: payload=$payload');
        _onSelectPayload?.call(payload);
      };

      await notification.show();
    } catch (e, st) {
      appLogger.w('Error displaying desktop notification: $e\n$st');
    }
  }
}

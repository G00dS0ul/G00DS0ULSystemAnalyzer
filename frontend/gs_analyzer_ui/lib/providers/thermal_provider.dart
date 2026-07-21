import 'package:gs_analyzer_ui/utils/logger.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:gs_analyzer_ui/models/thermal_telemetry.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class ThermalState {
  final ThermalTelemetry? telemetry;
  final int thermalThresholdCelsius;

  const ThermalState({this.telemetry, this.thermalThresholdCelsius = 85});

  bool get isCritical {
    if (telemetry == null) return false;
    final temp = telemetry!.cpuPackageCelsius ?? 0.0;
    return temp >= thermalThresholdCelsius;
  }

  ThermalState copyWith({
    ThermalTelemetry? telemetry,
    int? thermalThresholdCelsius,
  }) {
    return ThermalState(
      telemetry: telemetry ?? this.telemetry,
      thermalThresholdCelsius:
          thermalThresholdCelsius ?? this.thermalThresholdCelsius,
    );
  }
}

class ThermalNotifier extends StateNotifier<ThermalState> {
  HubConnection? _hubConnection;
  final ApiService _apiService = ApiService();
  final Ref ref;

  ThermalNotifier(this.ref) : super(const ThermalState()) {
    _fetchInitialSnapshot();
    _initSignalR();
    _listenToSettings();
  }

  void _listenToSettings() {
    ref.listen(settingsProvider, (previous, next) {
      final newThreshold = next.currentSettings?.alerts.thermalThresholdCelsius;
      if (newThreshold != null &&
          newThreshold != state.thermalThresholdCelsius) {
        state = state.copyWith(thermalThresholdCelsius: newThreshold);
      }
    }, fireImmediately: true);
  }

  Future<void> _fetchInitialSnapshot() async {
    try {
      final snapshot = await _apiService.getCurrentThermals();
      if (snapshot != null) {
        // Instantly populate the UI while SignalR is still waking up
        state = state.copyWith(telemetry: ThermalTelemetry.fromJson(snapshot));
        appLogger.i("🦅🔥 THERMAL RADAR: Instant Snapshot Loaded!");
      }
    } catch (e) {
      appLogger.i("Failed to load initial thermal snapshot: $e");
    }
  }

  Future<void> _initSignalR() async {
    final serverUrl = 'http://localhost:5200/systemHub';

    _hubConnection = HubConnectionBuilder()
        .withUrl(serverUrl)
        .withAutomaticReconnect()
        .build();

    _hubConnection?.on("ReceiveThermalTelemetry", _handleThermalUpdate);

    try {
      await _hubConnection?.start();
      appLogger.i('THERMAL RADAR CONNECTED TO SYSTEM HUB!');
    } catch (e) {
      appLogger.i('TELEMETRY ERROR: Failed to connect thermal Radar: $e');
    }
  }

  void _handleThermalUpdate(List<dynamic>? arguments) {
    if (arguments != null && arguments.isNotEmpty) {
      try {
        final data = arguments.first as Map<String, dynamic>;
        state = state.copyWith(telemetry: ThermalTelemetry.fromJson(data));
      } catch (e) {
        appLogger.i('THERMAL PAYLOAD CRASH: $e');
      }
    }
  }

  @override
  void dispose() {
    _hubConnection?.stop();
    super.dispose();
  }
}

final thermalProvider = StateNotifierProvider<ThermalNotifier, ThermalState>((
  ref,
) {
  return ThermalNotifier(ref);
});

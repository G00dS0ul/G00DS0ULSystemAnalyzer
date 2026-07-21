import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/telemetry_history_model.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final apiServiceProvider = Provider((ref) => ApiService());

class TelemetryHistoryState {
  final TelemetryHistoryResponse? response;
  final bool isLoading;
  final int minutes;
  final String? error;

  TelemetryHistoryState({
    this.response,
    this.isLoading = false,
    this.minutes = 5,
    this.error,
  });

  TelemetryHistoryState copyWith({
    TelemetryHistoryResponse? response,
    bool? isLoading,
    int? minutes,
    String? error,
  }) {
    return TelemetryHistoryState(
      response: response ?? this.response,
      isLoading: isLoading ?? this.isLoading,
      minutes: minutes ?? this.minutes,
      error: error, // null means clear error if not specified
    );
  }
}

class TelemetryHistoryNotifier extends StateNotifier<TelemetryHistoryState> {
  final ApiService _apiService;
  final String _metric;
  Timer? _timer;

  TelemetryHistoryNotifier(this._apiService, this._metric)
    : super(TelemetryHistoryState()) {
    _fetchHistory();
    _startTimer();
  }

  void _startTimer() {
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 5), (_) {
      _fetchHistory(silent: true);
    });
  }

  Future<void> _fetchHistory({bool silent = false}) async {
    if (!silent) {
      state = state.copyWith(isLoading: true, error: null);
    }

    try {
      final response = await _apiService.fetchTelemetryHistory(
        _metric,
        state.minutes,
      );
      if (response != null) {
        state = state.copyWith(
          isLoading: false,
          response: response,
          error: null,
        );
      } else {
        state = state.copyWith(
          isLoading: false,
          error: 'Failed to fetch history for $_metric',
        );
      }
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  void setMinutes(int newMinutes) {
    if (state.minutes != newMinutes) {
      state = state.copyWith(minutes: newMinutes);
      _fetchHistory(); // fetch immediately on change
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}

// We use FamilyStateNotifier from riverpod to pass the metric as an argument
final telemetryHistoryProvider = StateNotifierProvider.autoDispose
    .family<TelemetryHistoryNotifier, TelemetryHistoryState, String>((
      ref,
      metric,
    ) {
      final apiService = ref.watch(apiServiceProvider);
      return TelemetryHistoryNotifier(apiService, metric);
    });

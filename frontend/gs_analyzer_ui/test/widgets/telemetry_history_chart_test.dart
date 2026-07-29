import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/telemetry_history_model.dart';
import 'package:gs_analyzer_ui/widgets/telemetry_history_chart.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/telemetry_history_provider.dart';
import 'package:mocktail/mocktail.dart';

class MockApiService extends Mock implements ApiService {}

void main() {
  group('TelemetryHistoryChart Widget', () {
    testWidgets('shows loading indicator initially', (
      WidgetTester tester,
    ) async {
      final mockApiService = MockApiService();

      // Return null or delay to keep it in loading state
      final completer = Completer<TelemetryHistoryResponse?>();
      when(
        () => mockApiService.fetchTelemetryHistory('cpu', 5),
      ).thenAnswer((_) => completer.future);

      await tester.pumpWidget(
        ProviderScope(
          overrides: [apiServiceProvider.overrideWithValue(mockApiService)],
          child: const MaterialApp(
            home: Scaffold(body: TelemetryHistoryChart(metricKey: 'cpu')),
          ),
        ),
      );

      // Verify loading indicator is present
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('CPU'), findsOneWidget); // title

      // Fix the leak: complete the future and let the widget settle to dispose the AnimationController
      completer.complete(null);
      await tester.pumpAndSettle();
    });

    testWidgets('renders chart and stats when data is loaded', (
      WidgetTester tester,
    ) async {
      final mockApiService = MockApiService();
      final now = DateTime.now();

      final mockResponse = TelemetryHistoryResponse(
        metric: 'cpu',
        minutes: 15,
        unit: '%',
        points: [
          TelemetryPoint(
            timestamp: now.subtract(const Duration(minutes: 5)),
            value: 25.0,
          ),
          TelemetryPoint(timestamp: now, value: 50.0),
        ],
        stats: TelemetryStats(min: 25.0, max: 50.0, avg: 37.5, current: 50.0),
      );

      when(
        () => mockApiService.fetchTelemetryHistory('cpu', 5),
      ).thenAnswer((_) async => mockResponse);

      await tester.pumpWidget(
        ProviderScope(
          overrides: [apiServiceProvider.overrideWithValue(mockApiService)],
          child: const MaterialApp(
            home: Scaffold(body: TelemetryHistoryChart(metricKey: 'cpu')),
          ),
        ),
      );

      // Wait for provider to fetch
      await tester.pumpAndSettle();

      // Should show the title
      expect(find.text('CPU'), findsOneWidget);

      // Should show the stat strips
      expect(find.text('MIN: '), findsOneWidget);
      expect(find.text('25.0 %'), findsOneWidget);
      expect(find.text('MAX: '), findsOneWidget);
      expect(find.text('50.0 %'), findsNWidgets(2)); // matches both MAX and NOW

      // Should NOT have loading indicator
      expect(find.byType(CircularProgressIndicator), findsNothing);

      // Fix the leak: unmount the chart to dispose fl_chart's GestureRecognizers
      await tester.pumpWidget(const SizedBox());
      await tester.pumpAndSettle();
    });
  });
}

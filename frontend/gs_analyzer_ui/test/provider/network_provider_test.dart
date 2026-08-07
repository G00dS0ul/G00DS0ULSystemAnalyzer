import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/network_telemetry.dart';
import 'package:gs_analyzer_ui/providers/network_provider.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('NetworkNotifier Tests', () {
    test('updateFromSnapshot appends rolling spots and computes rollingMaxY', () {
      final notifier = NetworkNotifier();

      final snap1 = NetworkSnapshot(
        timestamp: DateTime.now(),
        primaryInterfaceId: '{nic-1}',
        interfaces: [
          NetInterfaceSnapshot(
            id: '{nic-1}',
            name: 'Wi-Fi',
            description: 'Wireless Adapter',
            interfaceType: 'Wireless80211',
            isUp: true,
            linkSpeedBitsPerSec: 866000000,
            rxBytesPerSec: 500000.0,
            txBytesPerSec: 250000.0,
            sessionRxBytes: 1000000,
            sessionTxBytes: 500000,
          )
        ],
      );

      notifier.updateFromSnapshot(snap1);

      expect(notifier.state.snapshot, snap1);
      expect(notifier.state.primaryInterface?.id, '{nic-1}');
      expect(notifier.state.rxRollingSpots.length, 1);
      expect(notifier.state.rxRollingSpots.first.y, 500000.0);
      expect(notifier.state.txRollingSpots.first.y, 250000.0);
      // 500000 * 1.2 = 600000 > 131072.0
      expect(notifier.state.rollingMaxY, 600000.0);
    });

    test('rollingMaxY is floored at 128 KB/s during low throughput', () {
      final notifier = NetworkNotifier();

      final snap = NetworkSnapshot(
        timestamp: DateTime.now(),
        primaryInterfaceId: '{nic-1}',
        interfaces: [
          NetInterfaceSnapshot(
            id: '{nic-1}',
            name: 'Wi-Fi',
            description: 'Wireless Adapter',
            interfaceType: 'Wireless80211',
            isUp: true,
            linkSpeedBitsPerSec: 866000000,
            rxBytesPerSec: 100.0,
            txBytesPerSec: 50.0,
            sessionRxBytes: 100,
            sessionTxBytes: 50,
          )
        ],
      );

      notifier.updateFromSnapshot(snap);
      expect(notifier.state.rollingMaxY, 131072.0);
    });
  });
}

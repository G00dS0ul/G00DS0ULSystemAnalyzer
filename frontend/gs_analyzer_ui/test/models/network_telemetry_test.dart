import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/network_telemetry.dart';

void main() {
  group('NetworkTelemetry Model Tests', () {
    test('deserializes NetInterfaceSnapshot correctly from json', () {
      final json = {
        'id': '{1111-2222}',
        'name': 'Wi-Fi',
        'description': 'Intel AX200',
        'interfaceType': 'Wireless80211',
        'isUp': true,
        'linkSpeedBitsPerSec': 866000000,
        'rxBytesPerSec': 1048576.0,
        'txBytesPerSec': 524288.0,
        'utilisationPercent': 1.25,
        'sessionRxBytes': 50000000,
        'sessionTxBytes': 12000000,
      };

      final nic = NetInterfaceSnapshot.fromJson(json);
      expect(nic.id, '{1111-2222}');
      expect(nic.name, 'Wi-Fi');
      expect(nic.description, 'Intel AX200');
      expect(nic.interfaceType, 'Wireless80211');
      expect(nic.isUp, true);
      expect(nic.linkSpeedBitsPerSec, 866000000);
      expect(nic.rxBytesPerSec, 1048576.0);
      expect(nic.txBytesPerSec, 524288.0);
      expect(nic.utilisationPercent, 1.25);
      expect(nic.sessionRxBytes, 50000000);
      expect(nic.sessionTxBytes, 12000000);
    });

    test('deserializes NetworkSnapshot and supports pascal-case JSON from backend', () {
      final json = {
        'Timestamp': '2026-08-07T12:00:00.000Z',
        'PrimaryInterfaceId': '{1111-2222}',
        'Interfaces': [
          {
            'Id': '{1111-2222}',
            'Name': 'Ethernet',
            'Description': 'Realtek PCIe GbE',
            'InterfaceType': 'Ethernet',
            'IsUp': true,
            'LinkSpeedBitsPerSec': 1000000000,
            'RxBytesPerSec': 2048.0,
            'TxBytesPerSec': 1024.0,
            'UtilisationPercent': 0.02,
            'SessionRxBytes': 10240,
            'SessionTxBytes': 5120,
          }
        ]
      };

      final snapshot = NetworkSnapshot.fromJson(json);
      expect(snapshot.primaryInterfaceId, '{1111-2222}');
      expect(snapshot.interfaces.length, 1);
      expect(snapshot.interfaces.first.name, 'Ethernet');
      expect(snapshot.interfaces.first.linkSpeedBitsPerSec, 1000000000);
    });
  });
}

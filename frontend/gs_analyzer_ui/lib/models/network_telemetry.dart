class NetInterfaceSnapshot {
  final String id;
  final String name;
  final String description;
  final String interfaceType;
  final bool isUp;
  final int linkSpeedBitsPerSec;
  final double rxBytesPerSec;
  final double txBytesPerSec;
  final double? utilisationPercent;
  final int sessionRxBytes;
  final int sessionTxBytes;

  NetInterfaceSnapshot({
    required this.id,
    required this.name,
    required this.description,
    required this.interfaceType,
    required this.isUp,
    required this.linkSpeedBitsPerSec,
    required this.rxBytesPerSec,
    required this.txBytesPerSec,
    this.utilisationPercent,
    required this.sessionRxBytes,
    required this.sessionTxBytes,
  });

  factory NetInterfaceSnapshot.fromJson(Map<String, dynamic> json) {
    return NetInterfaceSnapshot(
      id: (json['id'] ?? json['Id'] ?? '') as String,
      name: (json['name'] ?? json['Name'] ?? '') as String,
      description: (json['description'] ?? json['Description'] ?? '') as String,
      interfaceType: (json['interfaceType'] ?? json['InterfaceType'] ?? '') as String,
      isUp: (json['isUp'] ?? json['IsUp'] ?? false) as bool,
      linkSpeedBitsPerSec: (json['linkSpeedBitsPerSec'] ?? json['LinkSpeedBitsPerSec'] as num?)?.toInt() ?? -1,
      rxBytesPerSec: (json['rxBytesPerSec'] ?? json['RxBytesPerSec'] as num?)?.toDouble() ?? 0.0,
      txBytesPerSec: (json['txBytesPerSec'] ?? json['TxBytesPerSec'] as num?)?.toDouble() ?? 0.0,
      utilisationPercent: (json['utilisationPercent'] ?? json['UtilisationPercent'] as num?)?.toDouble(),
      sessionRxBytes: (json['sessionRxBytes'] ?? json['SessionRxBytes'] as num?)?.toInt() ?? 0,
      sessionTxBytes: (json['sessionTxBytes'] ?? json['SessionTxBytes'] as num?)?.toInt() ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'description': description,
    'interfaceType': interfaceType,
    'isUp': isUp,
    'linkSpeedBitsPerSec': linkSpeedBitsPerSec,
    'rxBytesPerSec': rxBytesPerSec,
    'txBytesPerSec': txBytesPerSec,
    'utilisationPercent': utilisationPercent,
    'sessionRxBytes': sessionRxBytes,
    'sessionTxBytes': sessionTxBytes,
  };
}

class NetworkSnapshot {
  final DateTime timestamp;
  final String? primaryInterfaceId;
  final List<NetInterfaceSnapshot> interfaces;

  NetworkSnapshot({
    required this.timestamp,
    this.primaryInterfaceId,
    required this.interfaces,
  });

  factory NetworkSnapshot.fromJson(Map<String, dynamic> json) {
    final rawTimestamp = json['timestamp'] ?? json['Timestamp'];
    DateTime parsedTimestamp;
    if (rawTimestamp is String) {
      parsedTimestamp = DateTime.tryParse(rawTimestamp) ?? DateTime.now();
    } else {
      parsedTimestamp = DateTime.now();
    }

    final rawInterfaces = json['interfaces'] ?? json['Interfaces'];
    final List<NetInterfaceSnapshot> parsedInterfaces = [];
    if (rawInterfaces is List) {
      for (var item in rawInterfaces) {
        if (item is Map<String, dynamic>) {
          parsedInterfaces.add(NetInterfaceSnapshot.fromJson(item));
        } else if (item is Map) {
          parsedInterfaces.add(NetInterfaceSnapshot.fromJson(Map<String, dynamic>.from(item)));
        }
      }
    }

    return NetworkSnapshot(
      timestamp: parsedTimestamp,
      primaryInterfaceId: (json['primaryInterfaceId'] ?? json['PrimaryInterfaceId']) as String?,
      interfaces: parsedInterfaces,
    );
  }

  Map<String, dynamic> toJson() => {
    'timestamp': timestamp.toIso8601String(),
    'primaryInterfaceId': primaryInterfaceId,
    'interfaces': interfaces.map((i) => i.toJson()).toList(),
  };
}

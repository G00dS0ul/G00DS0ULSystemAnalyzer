class ThermalTelemetry {
  final double? cpuPackageCelsius;
  final List<double> coreCelsius;
  final double? motherBoardCelsius;
  final double? chipsetCelsius;
  final double? nvmeCelsius;
  final double? ramCelsius;
  final double? ambientCelsius;
  final double? cpuPowerWatts;
  final bool isThermalThrottling;
  final int? cpuFanRpm;
  final int? chassisFan1Rpm;
  final int? chassisFan2Rpm;
  final int? pumpRpm;
  final double? gpuCoreCelsius;
  final double? gpuHotspotCelsius;
  final double? gpuVramCelsius;
  final int? gpuFanRpm;

  ThermalTelemetry({
    this.cpuPackageCelsius,
    this.coreCelsius = const [],
    this.motherBoardCelsius,
    this.chipsetCelsius,
    this.nvmeCelsius,
    this.ramCelsius,
    this.ambientCelsius,
    this.cpuPowerWatts,
    this.isThermalThrottling = false,
    this.cpuFanRpm,
    this.chassisFan1Rpm,
    this.chassisFan2Rpm,
    this.pumpRpm,
    this.gpuCoreCelsius,
    this.gpuHotspotCelsius,
    this.gpuVramCelsius,
    this.gpuFanRpm,
  });

  factory ThermalTelemetry.fromJson(Map<String, dynamic> json) {
    return ThermalTelemetry(
      cpuPackageCelsius: (json['cpuPackageCelsius'] as num?)?.toDouble(),
      coreCelsius:
          (json['coreCelsius'] as List<dynamic>?)
              ?.map((e) => (e as num).toDouble())
              .toList() ??
          [],
      motherBoardCelsius: (json['motherboardCelsius'] as num?)?.toDouble(),
      chipsetCelsius: (json['chipsetCelsius'] as num?)?.toDouble(),
      nvmeCelsius: (json['nvmeCelsius'] as num?)?.toDouble(),
      ramCelsius: (json['ramCelsius'] as num?)?.toDouble(),
      ambientCelsius: (json['ambientCelsius'] as num?)?.toDouble(),
      cpuPowerWatts: (json['cpuPowerWatts'] as num?)?.toDouble(),
      isThermalThrottling: json['isThermalThrottling'] as bool? ?? false,
      cpuFanRpm: json['cpuFanRpm'] as int?,
      chassisFan1Rpm: json['chassisFan1Rpm'] as int?,
      chassisFan2Rpm: json['chassisFan2Rpm'] as int?,
      pumpRpm: json['pumpRpm'] as int?,
      gpuCoreCelsius: (json['gpuCoreCelsius'] as num?)?.toDouble(),
      gpuHotspotCelsius: (json['gpuHotspotCelsius'] as num?)?.toDouble(),
      gpuVramCelsius: (json['gpuVramCelsius'] as num?)?.toDouble(),
      gpuFanRpm: json['gpuFanRpm'] as int?,
    );
  }
}

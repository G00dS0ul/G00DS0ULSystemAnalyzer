class ProcessTelemetry {
  final int pid;
  final String name;
  final double ramMb;
  final double percentMem;
  final String status;
  final double cpuPercent;
  final String user;

  ProcessTelemetry({
    required this.pid,
    required this.name,
    required this.ramMb,
    required this.percentMem,
    required this.status,
    required this.cpuPercent,
    required this.user,
  });

  factory ProcessTelemetry.fromJson(
    Map<String, dynamic> json,
    double totalSystemRamMb,
  ) {
    double mb = (json['ramMb'] ?? 0.0).toDouble();
    return ProcessTelemetry(
      pid: json['processId'] ?? 0,
      name: json['name'] ?? 'UNKNOWN',
      ramMb: mb,
      percentMem: totalSystemRamMb > 0 ? (mb / totalSystemRamMb) * 100 : 0.0,
      status: json['status'] ?? 'UNKNOWN',
      cpuPercent: (json['cpuPercent'] ?? 0.0).toDouble(),
      user: json['user'] ?? json['username'] ?? 'UNKNOWN',
    );
  }
}

class ProcessGroup {
  final String name;
  final List<ProcessTelemetry> processes;

  ProcessGroup({required this.name, required this.processes});

  double get totalRamMb => processes.fold(0, (sum, p) => sum + p.ramMb);
  double get totalPercentMem =>
      processes.fold(0, (sum, p) => sum + p.percentMem);
  double get totalCpuPercent =>
      processes.fold(0, (sum, p) => sum + p.cpuPercent);
  int get count => processes.length;
  int get primaryPid => processes.isNotEmpty ? processes.first.pid : 0;
  String get primaryUser =>
      processes.isNotEmpty ? processes.first.user : 'UNKNOWN';

  String get dominantStatus {
    if (processes.any((p) => p.status == 'RUNNING')) return 'RUNNING';
    if (processes.any((p) => p.status == 'SLEEPING')) return 'SLEEPING';
    return 'STOPPED';
  }
}

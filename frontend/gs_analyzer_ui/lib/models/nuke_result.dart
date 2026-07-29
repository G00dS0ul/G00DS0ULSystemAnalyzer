class NukeOperation {
  final String operationId;
  final DateTime executedAt;
  final List<String> originalPaths;
  final List<String> deletedPaths;
  final bool usedRecycleBin;
  final int deletedFiles;

  NukeOperation({
    required this.operationId,
    required this.executedAt,
    required this.originalPaths,
    required this.deletedPaths,
    required this.usedRecycleBin,
    required this.deletedFiles,
  });

  factory NukeOperation.fromJson(Map<String, dynamic> json) {
    return NukeOperation(
      operationId: json['operationId'] ?? '',
      executedAt: json['executedAt'] != null
          ? DateTime.parse(json['executedAt'])
          : DateTime.now(),
      originalPaths: List<String>.from(json['originalPaths'] ?? []),
      deletedPaths: List<String>.from(json['deletedPaths'] ?? []),
      usedRecycleBin: json['usedRecycleBin'] ?? false,
      deletedFiles: json['deletedFiles'] ?? 0,
    );
  }
}

class NukeResultDto {
  final int deletedFiles;
  final int freedBytes;
  final String freedFormatted;
  final int stagedBytes;
  final String stagedFormatted;
  final int skippedFiles;
  final bool recycleBinUsed;
  final bool recoverable;
  final String operationId;

  NukeResultDto({
    required this.deletedFiles,
    required this.freedBytes,
    required this.freedFormatted,
    required this.stagedBytes,
    required this.stagedFormatted,
    required this.skippedFiles,
    required this.recycleBinUsed,
    required this.recoverable,
    required this.operationId,
  });

  factory NukeResultDto.fromJson(Map<String, dynamic> json) {
    return NukeResultDto(
      deletedFiles: json['deletedFiles'] ?? 0,
      freedBytes: json['freedBytes'] ?? 0,
      freedFormatted: json['freedFormatted'] ?? '',
      stagedBytes: json['stagedBytes'] ?? 0,
      stagedFormatted: json['stagedFormatted'] ?? '',
      skippedFiles: json['skippedFiles'] ?? 0,
      recycleBinUsed: json['recycleBinUsed'] ?? false,
      recoverable: json['recoverable'] ?? false,
      operationId: json['operationId'] ?? '',
    );
  }
}

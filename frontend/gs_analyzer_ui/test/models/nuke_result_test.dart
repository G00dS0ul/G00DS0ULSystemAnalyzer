import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/nuke_result.dart';

void main() {
  group('NukeOperation.fromJson', () {
    test('parses all fields incl. deletedFiles', () {
      final op = NukeOperation.fromJson({
        'operationId': 'op-1',
        'executedAt': '2026-06-24T10:00:00Z',
        'originalPaths': ['C:\\a', 'C:\\b'],
        'deletedPaths': ['C:\\a'],
        'usedRecycleBin': true,
        'deletedFiles': 3,
      });
      expect(op.operationId, 'op-1');
      expect(op.usedRecycleBin, isTrue);
      expect(op.deletedFiles, 3);
      expect(op.originalPaths, hasLength(2));
    });

    test('applies safe defaults when fields are missing', () {
      final op = NukeOperation.fromJson({});
      expect(op.operationId, '');
      expect(op.usedRecycleBin, isFalse);
      expect(op.deletedFiles, 0);
      expect(op.originalPaths, isEmpty);
      expect(op.deletedPaths, isEmpty);
    });
  });

  group('NukeResultDto.fromJson', () {
    test('parses a recoverable recycle-bin result', () {
      final dto = NukeResultDto.fromJson({
        'deletedFiles': 2,
        'freedBytes': 0,
        'freedFormatted': '0 B',
        'stagedBytes': 100,
        'stagedFormatted': '100 B',
        'skippedFiles': 1,
        'recycleBinUsed': true,
        'recoverable': true,
        'operationId': 'op-9',
      });
      expect(dto.recycleBinUsed, isTrue);
      expect(dto.recoverable, isTrue);
      expect(dto.stagedBytes, 100);
      expect(dto.freedBytes, 0);
      expect(dto.operationId, 'op-9');
    });

    test('defaults are safe when fields absent', () {
      final dto = NukeResultDto.fromJson({});
      expect(dto.deletedFiles, 0);
      expect(dto.recoverable, isFalse);
      expect(dto.recycleBinUsed, isFalse);
      expect(dto.operationId, '');
    });
  });
}

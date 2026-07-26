import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/scan_diff.dart';

void main() {
  group('ScanDiff.fromJson', () {
    test('parses a full diff response (camelCase)', () {
      final json = {
        'root': 'C:/',
        'hasBaseline': true,
        'baselineScannedAt': '2026-07-14T09:12:00Z',
        'currentScannedAt': '2026-07-15T14:03:00Z',
        'added': [
          {
            'path': 'C:/dev/new-project/node_modules',
            'isDirectory': true,
            'currentBytes': 261120000,
            'previousBytes': 0,
            'deltaBytes': 261120000,
            'childCount': 18422,
            'lastModified': '2026-07-15T11:02:00Z',
          },
        ],
        'removed': [
          {
            'path': 'C:/old_backups/2024',
            'isDirectory': true,
            'currentBytes': 0,
            'previousBytes': 1073741824,
            'deltaBytes': -1073741824,
            'childCount': 640,
            'lastModified': '2024-01-10T08:00:00Z',
          },
        ],
        'grown': [
          {
            'path': 'C:/Users/user/AppData/Local/Temp',
            'isDirectory': true,
            'currentBytes': 3221225472,
            'previousBytes': 1073741824,
            'deltaBytes': 2147483648,
            'childCount': null,
            'lastModified': '2026-07-15T14:01:00Z',
          },
        ],
        'shrunk': <dynamic>[],
        'summary': {
          'addedCount': 1,
          'addedBytes': 261120000,
          'removedCount': 1,
          'removedBytes': 1073741824,
          'grownCount': 1,
          'grownDeltaBytes': 2147483648,
          'shrunkCount': 0,
          'shrunkDeltaBytes': 0,
          'netDeltaBytes': 1334883648,
        },
      };

      final diff = ScanDiff.fromJson(json);

      expect(diff.root, 'C:/');
      expect(diff.hasBaseline, isTrue);
      expect(diff.added.single.childCount, 18422);
      expect(diff.removed.single.deltaBytes, -1073741824);
      expect(diff.grown.single.childCount, isNull);
      expect(diff.shrunk, isEmpty);
      expect(diff.summary.netDeltaBytes, 1334883648);
      expect(diff.summary.totalChangeCount, 3);
      expect(diff.isEmpty, isFalse);
    });

    test('first-scan response: hasBaseline false, empty lists, isEmpty true', () {
      final json = {
        'root': 'D:/',
        'hasBaseline': false,
        'baselineScannedAt': null,
        'currentScannedAt': '2026-07-15T14:03:00Z',
        'added': <dynamic>[],
        'removed': <dynamic>[],
        'grown': <dynamic>[],
        'shrunk': <dynamic>[],
        'summary': {
          'addedCount': 0,
          'addedBytes': 0,
          'removedCount': 0,
          'removedBytes': 0,
          'grownCount': 0,
          'grownDeltaBytes': 0,
          'shrunkCount': 0,
          'shrunkDeltaBytes': 0,
          'netDeltaBytes': 0,
        },
      };

      final diff = ScanDiff.fromJson(json);

      expect(diff.hasBaseline, isFalse);
      expect(diff.baselineScannedAt, isNull);
      expect(diff.isEmpty, isTrue);
      expect(diff.summary.totalChangeCount, 0);
    });

    test('parses PascalCase keys (raw backend record casing)', () {
      final json = {
        'Root': 'C:/',
        'HasBaseline': true,
        'BaselineScannedAt': '2026-07-14T09:12:00Z',
        'CurrentScannedAt': '2026-07-15T14:03:00Z',
        'Added': [
          {
            'Path': 'C:/x',
            'IsDirectory': true,
            'CurrentBytes': 100,
            'PreviousBytes': 0,
            'DeltaBytes': 100,
            'ChildCount': null,
            'LastModified': null,
          },
        ],
        'Removed': <dynamic>[],
        'Grown': <dynamic>[],
        'Shrunk': <dynamic>[],
        'Summary': {
          'AddedCount': 1,
          'AddedBytes': 100,
          'RemovedCount': 0,
          'RemovedBytes': 0,
          'GrownCount': 0,
          'GrownDeltaBytes': 0,
          'ShrunkCount': 0,
          'ShrunkDeltaBytes': 0,
          'NetDeltaBytes': 100,
        },
      };

      final diff = ScanDiff.fromJson(json);

      expect(diff.root, 'C:/');
      expect(diff.added.single.path, 'C:/x');
      expect(diff.added.single.lastModified, isNull);
      expect(diff.summary.addedCount, 1);
    });
  });

  group('ScanDiffEntry helpers', () {
    test('parentPath returns the containing directory', () {
      const entry = ScanDiffEntry(
        path: 'C:/Users/user/Downloads/ubuntu.iso',
        isDirectory: false,
        currentBytes: 100,
        previousBytes: 0,
        deltaBytes: 100,
        childCount: null,
        lastModified: null,
      );
      expect(entry.parentPath, 'C:/Users/user/Downloads');
    });

    test('parentPath handles backslash paths', () {
      const entry = ScanDiffEntry(
        path: r'C:\dev\project\node_modules',
        isDirectory: true,
        currentBytes: 100,
        previousBytes: 0,
        deltaBytes: 100,
        childCount: 10,
        lastModified: null,
      );
      expect(entry.parentPath, 'C:/dev/project');
    });

    test('displayName returns the trailing segment', () {
      const entry = ScanDiffEntry(
        path: 'C:/dev/project/node_modules',
        isDirectory: true,
        currentBytes: 100,
        previousBytes: 0,
        deltaBytes: 100,
        childCount: 10,
        lastModified: null,
      );
      expect(entry.displayName, 'node_modules');
    });

    test('displayName strips a trailing slash', () {
      const entry = ScanDiffEntry(
        path: 'C:/dev/project/',
        isDirectory: true,
        currentBytes: 100,
        previousBytes: 0,
        deltaBytes: 100,
        childCount: 10,
        lastModified: null,
      );
      expect(entry.displayName, 'project');
    });

    test('handles missing/null numeric fields as zero', () {
      final entry = ScanDiffEntry.fromJson({'path': 'C:/x', 'isDirectory': true});
      expect(entry.currentBytes, 0);
      expect(entry.previousBytes, 0);
      expect(entry.deltaBytes, 0);
      expect(entry.childCount, isNull);
    });
  });
}

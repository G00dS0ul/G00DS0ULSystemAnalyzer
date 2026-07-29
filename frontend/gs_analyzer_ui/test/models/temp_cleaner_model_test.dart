import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/temp_cleaner_model.dart';

void main() {
  group('TempLocationPreview.fromJson', () {
    test('parses all fields from camelCase and PascalCase keys', () {
      final loc1 = TempLocationPreview.fromJson({
        'path': 'C:\\temp',
        'label': 'User temp',
        'category': 'Temp',
        'sizeBytes': 1024,
        'sizeFormatted': '1 KB',
        'fileCount': 5,
      });

      expect(loc1.path, 'C:\\temp');
      expect(loc1.label, 'User temp');
      expect(loc1.category, 'Temp');
      expect(loc1.sizeBytes, 1024);
      expect(loc1.sizeFormatted, '1 KB');
      expect(loc1.fileCount, 5);

      final loc2 = TempLocationPreview.fromJson({
        'Path': 'C:\\cache',
        'Label': 'npm cache',
        'Category': 'Cache',
        'SizeBytes': 2048,
        'SizeFormatted': '2 KB',
        'FileCount': 10,
      });

      expect(loc2.path, 'C:\\cache');
      expect(loc2.label, 'npm cache');
      expect(loc2.category, 'Cache');
      expect(loc2.sizeBytes, 2048);
      expect(loc2.sizeFormatted, '2 KB');
      expect(loc2.fileCount, 10);
    });

    test('applies safe defaults', () {
      final loc = TempLocationPreview.fromJson({});

      expect(loc.path, '');
      expect(loc.label, '');
      expect(loc.category, 'Temp');
      expect(loc.sizeBytes, 0);
      expect(loc.sizeFormatted, '');
      expect(loc.fileCount, 0);
    });

    test(
      'label defaults to empty and category defaults to Temp when missing',
      () {
        final loc = TempLocationPreview.fromJson({
          'path': 'C:\\Windows\\Temp',
          'sizeBytes': 512,
          'sizeFormatted': '512 B',
          'fileCount': 1,
        });

        expect(loc.label, '');
        expect(loc.category, 'Temp');
      },
    );

    test('category field accepts Cache value', () {
      final loc = TempLocationPreview.fromJson({
        'path': 'C:\\Users\\test\\AppData\\Local\\npm-cache',
        'label': 'npm cache',
        'category': 'Cache',
        'sizeBytes': 1024,
        'sizeFormatted': '1 KB',
        'fileCount': 3,
      });

      expect(loc.category, 'Cache');
      expect(loc.label, 'npm cache');
    });
  });

  group('TempPreviewResponse.fromJson', () {
    test('parses full response', () {
      final response = TempPreviewResponse.fromJson({
        'totalBytes': 3072,
        'totalFormatted': '3 KB',
        'locations': [
          {
            'path': 'C:\\temp',
            'sizeBytes': 1024,
            'sizeFormatted': '1 KB',
            'fileCount': 5,
          },
          {
            'path': 'C:\\cache',
            'sizeBytes': 2048,
            'sizeFormatted': '2 KB',
            'fileCount': 10,
          },
        ],
      });

      expect(response.totalBytes, 3072);
      expect(response.totalFormatted, '3 KB');
      expect(response.locations.length, 2);
      expect(response.locations[0].path, 'C:\\temp');
      expect(response.locations[1].path, 'C:\\cache');
    });

    test('handles empty locations', () {
      final response = TempPreviewResponse.fromJson({
        'totalBytes': 0,
        'totalFormatted': '0 B',
        'locations': [],
      });

      expect(response.totalBytes, 0);
      expect(response.totalFormatted, '0 B');
      expect(response.locations, isEmpty);
    });
  });

  group('TempCleanResult.fromJson', () {
    test('parses all fields', () {
      final result = TempCleanResult.fromJson({
        'deletedFiles': 15,
        'freedBytes': 3072,
        'freedFormatted': '3 KB',
        'skippedFiles': 2,
      });

      expect(result.deletedFiles, 15);
      expect(result.freedBytes, 3072);
      expect(result.freedFormatted, '3 KB');
      expect(result.skippedFiles, 2);
    });

    test('applies safe defaults', () {
      final result = TempCleanResult.fromJson({});

      expect(result.deletedFiles, 0);
      expect(result.freedBytes, 0);
      expect(result.freedFormatted, '');
      expect(result.skippedFiles, 0);
    });
  });
}

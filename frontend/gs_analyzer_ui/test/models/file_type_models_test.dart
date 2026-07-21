import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/file_type_model.dart';

void main() {
  group('FileTypeExtensionEntry', () {
    test('FromJson_ParsesAllFields_Correctly', () {
      final entry = FileTypeExtensionEntry.fromJson({
        'ext': '.exe',
        'fileCount': 619,
        'percentOfDisk': 21.2,
        'sizeFormatted': '123.3 MB',
        'totalBytes': 129236992,
      });

      expect(entry.ext, '.exe');
      expect(entry.fileCount, 619);
      expect(entry.percentOfDisk, 21.2);
      expect(entry.sizeFormatted, '123.3 MB');
      expect(entry.totalBytes, 129236992);
    });

    test('FromJson_CoercesIntPercentToDouble', () {
      final entry = FileTypeExtensionEntry.fromJson({
        'ext': '.cs',
        'fileCount': 5,
        'percentOfDisk': 1, // comes as int from JSON
        'sizeFormatted': '50 KB',
        'totalBytes': 51200,
      });

      expect(entry.percentOfDisk, isA<double>());
      expect(entry.percentOfDisk, 1.0);
    });
  });

  group('FileTypeCategory', () {
    final sampleJson = {
      'name': 'executables',
      'fileCount': 619,
      'percentOfDisk': 21.2,
      'sizeFormatted': '123.3 MB',
      'totalBytes': 129236992,
      'extensions': [
        {
          'ext': '.exe',
          'fileCount': 200,
          'percentOfDisk': 10.0,
          'sizeFormatted': '50.0 MB',
          'totalBytes': 52428800,
        },
        {
          'ext': '.dll',
          'fileCount': 419,
          'percentOfDisk': 11.2,
          'sizeFormatted': '73.3 MB',
          'totalBytes': 76808192,
        },
      ],
    };

    test('FromJson_ParsesTopLevelFields', () {
      final cat = FileTypeCategory.fromJson(sampleJson);

      expect(cat.name, 'executables');
      expect(cat.fileCount, 619);
      expect(cat.percentOfDisk, 21.2);
      expect(cat.sizeFormatted, '123.3 MB');
      expect(cat.totalBytes, 129236992);
    });

    test('FromJson_ParsesNestedExtensionsList', () {
      final cat = FileTypeCategory.fromJson(sampleJson);

      expect(cat.extensions.length, 2);
      expect(cat.extensions[0].ext, '.exe');
      expect(cat.extensions[1].ext, '.dll');
    });

    test('FromJson_EmptyExtensionsList_IsValid', () {
      final cat = FileTypeCategory.fromJson({...sampleJson, 'extensions': []});
      expect(cat.extensions, isEmpty);
    });
  });

  group('FileTypeResult', () {
    final sampleJson = {
      'root': r'C:\',
      'totalScannedFormatted': '582.1 MB',
      'categories': [
        {
          'name': 'code',
          'fileCount': 196,
          'percentOfDisk': 71.3,
          'sizeFormatted': '415.0 MB',
          'totalBytes': 435159040,
          'extensions': [
            {
              'ext': '.cs',
              'fileCount': 196,
              'percentOfDisk': 71.3,
              'sizeFormatted': '415.0 MB',
              'totalBytes': 435159040,
            },
          ],
        },
      ],
    };

    test('FromJson_ParsesRootAndTotalFormatted', () {
      final result = FileTypeResult.fromJson(sampleJson);

      expect(result.root, r'C:\');
      expect(result.totalScannedFormatted, '582.1 MB');
    });

    test('FromJson_ParsesCategoriesList', () {
      final result = FileTypeResult.fromJson(sampleJson);

      expect(result.categories.length, 1);
      expect(result.categories[0].name, 'code');
      expect(result.categories[0].fileCount, 196);
    });

    test('FromJson_NestedExtensionsParsedThroughCategories', () {
      final result = FileTypeResult.fromJson(sampleJson);
      final exts = result.categories[0].extensions;

      expect(exts.length, 1);
      expect(exts[0].ext, '.cs');
      expect(exts[0].fileCount, 196);
    });

    test('FromJson_EmptyCategoriesList_IsValid', () {
      final result = FileTypeResult.fromJson({
        'root': r'C:\',
        'totalScannedFormatted': '0 B',
        'categories': [],
      });
      expect(result.categories, isEmpty);
    });
  });
}

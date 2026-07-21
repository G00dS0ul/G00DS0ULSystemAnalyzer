import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/providers/file_type_provider.dart';

void main() {
  group('FileTypeNoScanException', () {
    test('IsAnException', () {
      expect(const FileTypeNoScanException(), isA<Exception>());
    });

    test('DoesNotThrowOnConstruction', () {
      // In Dart — just constructing should not throw
      expect(() => const FileTypeNoScanException(), returnsNormally);
      expect(const FileTypeNoScanException(), isNotNull);
    });
  });

  group('selectedCategoryProvider', () {
    test('DefaultsToNull', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      expect(container.read(selectedCategoryProvider), isNull);
    });

    test('UpdatesToCategoryName', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(selectedCategoryProvider.notifier).state = 'code';

      expect(container.read(selectedCategoryProvider), 'code');
    });

    test('CanBeClearedBackToNull', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(selectedCategoryProvider.notifier).state = 'media';
      container.read(selectedCategoryProvider.notifier).state = null;

      expect(container.read(selectedCategoryProvider), isNull);
    });

    test('TappingSameCategoryTwice_StaysSelected', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(selectedCategoryProvider.notifier).state = 'executables';
      container.read(selectedCategoryProvider.notifier).state = 'executables';

      expect(container.read(selectedCategoryProvider), 'executables');
    });
  });

  group('scanRootProvider', () {
    test('DefaultsToTheDriveNameKey', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      expect(container.read(scanRootProvider(r'C:\')), r'C:\');
    });

    test('DifferentDriveKeysAreIndependent', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      expect(container.read(scanRootProvider(r'C:\')), r'C:\');
      expect(container.read(scanRootProvider(r'D:\')), r'D:\');
    });

    test('UpdatesToSubfolderPath', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(scanRootProvider(r'C:\').notifier).state =
          r'C:\Users\G00dS0ul\Projects';

      expect(
        container.read(scanRootProvider(r'C:\')),
        r'C:\Users\G00dS0ul\Projects',
      );
    });

    test('UpdatingOneDrive_DoesNotAffectAnother', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(scanRootProvider(r'C:\').notifier).state = r'C:\Windows';

      expect(container.read(scanRootProvider(r'D:\')), r'D:\');
    });

    test('CanResetBackToDriveRoot', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(scanRootProvider(r'C:\').notifier).state = r'C:\Users';
      container.read(scanRootProvider(r'C:\').notifier).state = r'C:\';

      expect(container.read(scanRootProvider(r'C:\')), r'C:\');
    });
  });
}

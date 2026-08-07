import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/utils/formatters.dart';

void main() {
  group('formatRate Tests', () {
    test('formats B/s correctly', () {
      expect(formatRate(0), '0 B/s');
      expect(formatRate(512), '512 B/s');
      expect(formatRate(1023), '1023 B/s');
    });

    test('formats KB/s correctly', () {
      expect(formatRate(1024), '1.0 KB/s');
      expect(formatRate(1536), '1.5 KB/s');
      expect(formatRate(1024 * 100), '100.0 KB/s');
    });

    test('formats MB/s correctly', () {
      expect(formatRate(1048576), '1.00 MB/s');
      expect(formatRate(1048576 * 12.5), '12.50 MB/s');
    });

    test('formats GB/s correctly', () {
      expect(formatRate(1073741824), '1.00 GB/s');
      expect(formatRate(1073741824 * 2.5), '2.50 GB/s');
    });
  });

  group('formatBytes Tests', () {
    test('formats raw bytes, KB, MB, and GB', () {
      expect(formatBytes(500), '500 B');
      expect(formatBytes(2048), '2.0 KB');
      expect(formatBytes(1048576 * 5), '5.00 MB');
      expect(formatBytes(1073741824 * 3), '3.00 GB');
    });
  });

  group('formatLinkSpeed Tests', () {
    test('handles negative/unsupported speeds as N/A', () {
      expect(formatLinkSpeed(-1), 'N/A');
      expect(formatLinkSpeed(0), 'N/A');
    });

    test('formats Mbps and Gbps accurately', () {
      expect(formatLinkSpeed(100 * 1000 * 1000), '100 Mbps');
      expect(formatLinkSpeed(866 * 1000 * 1000), '866 Mbps');
      expect(formatLinkSpeed(1000 * 1000 * 1000), '1.0 Gbps');
      expect(formatLinkSpeed(10 * 1000 * 1000 * 1000), '10.0 Gbps');
    });
  });
}

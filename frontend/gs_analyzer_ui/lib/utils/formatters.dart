/// Shared formatting utilities for data rates, sizes, and hardware link speeds.

String formatRate(double bytesPerSec) {
  if (bytesPerSec < 1024) {
    return '${bytesPerSec.toStringAsFixed(0)} B/s';
  }
  if (bytesPerSec < 1024 * 1024) {
    return '${(bytesPerSec / 1024).toStringAsFixed(1)} KB/s';
  }
  if (bytesPerSec < 1024 * 1024 * 1024) {
    return '${(bytesPerSec / 1048576).toStringAsFixed(2)} MB/s';
  }
  return '${(bytesPerSec / 1073741824).toStringAsFixed(2)} GB/s';
}

String formatBytes(int bytes) {
  if (bytes < 1024) {
    return '$bytes B';
  }
  if (bytes < 1024 * 1024) {
    return '${(bytes / 1024).toStringAsFixed(1)} KB';
  }
  if (bytes < 1024 * 1024 * 1024) {
    return '${(bytes / 1048576).toStringAsFixed(2)} MB';
  }
  return '${(bytes / 1073741824).toStringAsFixed(2)} GB';
}

String formatLinkSpeed(int speedBitsPerSec) {
  if (speedBitsPerSec <= 0) {
    return 'N/A';
  }
  if (speedBitsPerSec < 1000 * 1000) {
    return '${(speedBitsPerSec / 1000).toStringAsFixed(0)} Kbps';
  }
  if (speedBitsPerSec < 1000 * 1000 * 1000) {
    return '${(speedBitsPerSec / 1000000).toStringAsFixed(0)} Mbps';
  }
  return '${(speedBitsPerSec / 1000000000).toStringAsFixed(1)} Gbps';
}

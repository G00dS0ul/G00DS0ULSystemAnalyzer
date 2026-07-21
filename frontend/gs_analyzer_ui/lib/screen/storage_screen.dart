import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/drive_info.dart';
import '../providers/drive_stats_provider.dart';
import '../providers/settings_provider.dart';
import '../utils/hud_theme.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_view_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_mode_provider.dart';
import 'package:gs_analyzer_ui/widgets/file_type_analyzer_panel.dart';
import 'package:gs_analyzer_ui/widgets/undo_history_panel.dart';
import 'package:gs_analyzer_ui/providers/hud_density_provider.dart';

class StorageScreen extends ConsumerWidget {
  const StorageScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final currentDrive = ref.watch(currentDriveProvider);
    final drives = ref.watch(drivesProvider);
    final d = ref.watch(hudDensityProvider);

    Widget buildBody() {
      if (drives.isEmpty) {
        return Center(
          child: CircularProgressIndicator(color: HudTheme.accentCyan),
        );
      }

      if (currentDrive == null) {
        return const SizedBox.shrink();
      }

      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _DriveSelectorBar(drives: drives, selectedDrive: currentDrive),
          const Divider(color: Colors.white10, height: 1),

          Expanded(
            child: ListView(
              children: [
                Padding(
                  padding: EdgeInsets.all(d.panelPad),
                  child: _DriveDetailCard(drive: currentDrive, d: d),
                ),
                FileTypeAnalyzerPanel(driveName: currentDrive.name),
                Padding(
                  padding: EdgeInsets.symmetric(horizontal: d.panelPad),
                  child: Column(
                    children: [
                      _ScanLaunchTile(
                        title: 'DIRECTORY SCANNER',
                        subtitle: 'Index every sector on ${currentDrive.name}',
                        icon: Icons.account_tree_outlined,
                        d: d,
                        onLaunch: () => _enterAnalyzer(
                          ref,
                          currentDrive,
                          StorageMode.diskAnalyzer,
                        ),
                      ),
                      _ScanLaunchTile(
                        title: 'DUPLICATE HUNTER',
                        subtitle:
                            'Scan for duplicate files on ${currentDrive.name}',
                        icon: Icons.copy_all_outlined,
                        d: d,
                        onLaunch: () => _enterAnalyzer(
                          ref,
                          currentDrive,
                          StorageMode.duplicateScanner,
                        ),
                      ),
                      _ScanLaunchTile(
                        title: 'TEMP CLEANER',
                        subtitle:
                            'Purge temporary files across all system cache locations',
                        icon: Icons.cleaning_services_outlined,
                        d: d,
                        onLaunch: () => _enterAnalyzer(
                          ref,
                          currentDrive,
                          StorageMode.tempFileCleaner,
                        ),
                      ),
                      _ScanLaunchTile(
                        title: 'PERMISSION AUDIT',
                        subtitle:
                            'Detect world-writable paths and orphaned files',
                        icon: Icons.security_outlined,
                        d: d,
                        onLaunch: () => _enterAnalyzer(
                          ref,
                          currentDrive,
                          StorageMode.permissionAudit,
                        ),
                      ),
                    ],
                  ),
                ),
                UndoHistoryPanel(),
              ],
            ),
          ),
        ],
      );
    }

    return Scaffold(
      backgroundColor: HudTheme.bgPanel,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: Text('STORAGE MATRICES', style: HudTheme.headerCyan),
      ),
      body: buildBody(),
    );
  }

  void _enterAnalyzer(WidgetRef ref, DriveInfo drive, StorageMode mode) {
    ref.read(selectedDriveNameProvider.notifier).state = drive.name;
    ref.read(storageModeProvider.notifier).state = mode;

    if (mode == StorageMode.diskAnalyzer) {
      // Force refresh so the TelemetryHudWidget tracks the exact duration of the real scan.
      ref
          .read(directoryProvider.notifier)
          .scanDirectory(drive.name, forceRefresh: true);
    }

    ref.read(storageViewProvider.notifier).state = StorageView.analyzer;
  }
}

class _ScanLaunchTile extends StatelessWidget {
  final String title;
  final String subtitle;
  final IconData icon;
  final VoidCallback onLaunch;
  final HudDensity d;

  const _ScanLaunchTile({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.onLaunch,
    required this.d,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: d.gap),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onLaunch,
          borderRadius: BorderRadius.circular(8),
          child: Container(
            padding: EdgeInsets.all(d.panelPad),
            decoration: HudTheme.hudPanelDecoration,
            child: Row(
              children: [
                Icon(icon, color: HudTheme.accentCyan),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: HudTheme.headerCyan.copyWith(
                          color: HudTheme.accentCyan,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        subtitle,
                        style: HudTheme.bodyText.copyWith(
                          color: HudTheme.textDim,
                        ),
                      ),
                    ],
                  ),
                ),
                const Icon(Icons.chevron_right, color: HudTheme.textDim),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _DriveSelectorBar extends ConsumerWidget {
  final List<DriveInfo> drives;
  final DriveInfo selectedDrive;

  const _DriveSelectorBar({required this.drives, required this.selectedDrive});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: drives.map((drive) {
          final isSelected = drive.name == selectedDrive.name;
          return GestureDetector(
            onTap: () =>
                ref.read(selectedDriveNameProvider.notifier).state = drive.name,
            child: _DriveTab(drive: drive, isActive: isSelected),
          );
        }).toList(),
      ),
    );
  }
}

class _DriveTab extends ConsumerWidget {
  final DriveInfo drive;
  final bool isActive;

  const _DriveTab({required this.drive, required this.isActive});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    IconData getIcon() {
      if (drive.type.toLowerCase() == 'removable') return Icons.usb;
      if (drive.type.toLowerCase() == 'network') return Icons.cloud;
      return Icons.storage;
    }

    // Connect threshold to user settings!
    final alertSettings = ref.watch(settingsProvider).currentSettings?.alerts;
    final redThreshold = alertSettings?.diskThresholdPercent ?? 90;

    Color getStatusColor() {
      if (drive.percentageUsed >= redThreshold) return Colors.redAccent;
      if (drive.percentageUsed >= redThreshold - 10) return Colors.amber;
      return HudTheme.accentCyan; // Green or Cyan for healthy
    }

    return Container(
      width: 160,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isActive
            ? Colors.white.withValues(alpha: 0.05)
            : Colors.transparent,
        border: Border(
          bottom: BorderSide(
            color: isActive ? HudTheme.accentCyan : Colors.transparent,
            width: 3,
          ),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                getIcon(),
                color: isActive ? HudTheme.accentCyan : HudTheme.textDim,
                size: 16,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '${drive.label} (${drive.name})',
                  style: HudTheme.bodyText.copyWith(
                    color: isActive ? HudTheme.accentCyan : HudTheme.textDim,
                    fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          LinearProgressIndicator(
            value: drive.percentageUsed / 100,
            backgroundColor: Colors.white10,
            color: getStatusColor(),
            minHeight: 2,
          ),
        ],
      ),
    );
  }
}

class _DriveDetailCard extends ConsumerWidget {
  final DriveInfo drive;
  final HudDensity d;

  const _DriveDetailCard({required this.drive, required this.d});

  String _formatGB(int bytes) =>
      (bytes / (1024 * 1024 * 1024)).toStringAsFixed(1);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final alertSettings = ref.watch(settingsProvider).currentSettings?.alerts;
    final redThreshold = alertSettings?.diskThresholdPercent ?? 90;

    final Color statusColor = drive.percentageUsed >= redThreshold
        ? Colors.redAccent
        : (drive.percentageUsed >= redThreshold - 10
              ? Colors.amber
              : HudTheme.accentCyan);

    return Container(
      padding: EdgeInsets.all(d.panelPad),
      decoration: HudTheme.hudPanelDecoration,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'DRIVE: ${drive.label} (${drive.name})',
                style: HudTheme.headerCyan.copyWith(color: HudTheme.accentCyan),
              ),
              if (drive.percentageUsed >= redThreshold)
                Text(
                  '● CRITICAL SPACE',
                  style: HudTheme.bodyText.copyWith(
                    color: Colors.redAccent,
                    fontWeight: FontWeight.bold,
                  ),
                ),
            ],
          ),
          Divider(color: Colors.white10, height: d.gap * 3, thickness: 1),

          Wrap(
            spacing: d.gap * 3,
            runSpacing: d.gap,
            children: [
              _buildMetaTag('TYPE', drive.type.toUpperCase()),
              _buildMetaTag('FORMAT', drive.format.toUpperCase()),
              _buildMetaTag('MOUNT', drive.name.toUpperCase()),
            ],
          ),
          SizedBox(height: d.gap * 2),

          Row(
            children: [
              Expanded(
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(
                    value: drive.percentageUsed / 100,
                    backgroundColor: Colors.white10,
                    color: statusColor,
                    minHeight: 16,
                  ),
                ),
              ),
              const SizedBox(width: 16),
              Text(
                '${drive.percentageUsed.toStringAsFixed(1)}%',
                style: HudTheme.bodyText,
              ),
            ],
          ),
          SizedBox(height: d.gap * 2),

          LayoutBuilder(
            builder: (context, constraints) {
              final wide = constraints.maxWidth > 520;
              final used = Text(
                'USED: ${_formatGB(drive.usedBytes)} GB',
                style: HudTheme.bodyText.copyWith(color: statusColor),
              );
              final free = Text(
                'FREE: ${_formatGB(drive.freeBytes)} GB',
                style: HudTheme.bodyText,
              );
              final total = Text(
                'TOTAL: ${_formatGB(drive.totalBytes)} GB',
                style: HudTheme.bodyText.copyWith(color: HudTheme.textDim),
              );

              if (wide) {
                return Row(
                  children: [
                    Expanded(child: used),
                    Expanded(child: free),
                    Expanded(child: total),
                  ],
                );
              }

              return Wrap(
                spacing: d.gap * 3,
                runSpacing: d.gap,
                alignment: WrapAlignment.spaceBetween,
                children: [used, free, total],
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildMetaTag(String label, String value) {
    return Row(
      children: [
        Text(
          '$label: ',
          style: HudTheme.bodyText.copyWith(color: HudTheme.textDim),
        ),
        Text(
          value,
          style: HudTheme.bodyText.copyWith(fontWeight: FontWeight.bold),
        ),
      ],
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/scan_diff_model.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/providers/scan_diff_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_mode_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_view_provider.dart';
import 'package:intl/intl.dart';
import 'package:path/path.dart' as p;

class ScanDiffScreen extends ConsumerWidget {
  const ScanDiffScreen({Key? key}) : super(key: key);

  String _formatGB(int bytes) =>
      (bytes / (1024 * 1024 * 1024)).toStringAsFixed(2) + ' GB';

  String _formatMB(int bytes) =>
      (bytes / (1024 * 1024)).toStringAsFixed(0) + ' MB';

  String _formatSize(int bytes) {
    if (bytes.abs() >= 1024 * 1024 * 1024) return _formatGB(bytes);
    return _formatMB(bytes);
  }

  void _jumpToDir(BuildContext context, WidgetRef ref, String fullPath) {
    final parent = p.dirname(fullPath).replaceAll('\\', '/');
    ref.read(directoryProvider.notifier).scanDirectory(parent);
    ref.read(storageModeProvider.notifier).state = StorageMode.diskAnalyzer;
    ref.read(storageViewProvider.notifier).state = StorageView.analyzer;
    Navigator.of(context).pop(); // Close the screen and return to main view
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final drive = ref.watch(currentDriveProvider);

    return Scaffold(
      backgroundColor: HudTheme.bgPanel,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: Text('SCAN DIFF', style: HudTheme.headerCyan),
        actions: [
          if (drive != null)
            IconButton(
              icon: const Icon(Icons.refresh, color: HudTheme.accentCyan),
              tooltip: 'Re-Scan & Update',
              onPressed: () {
                ref.read(directoryProvider.notifier).scanDirectory(drive.name);
                ref.read(storageModeProvider.notifier).state = StorageMode.diskAnalyzer;
                ref.read(storageViewProvider.notifier).state = StorageView.analyzer;
                Navigator.of(context).pop(); // Jump back to scanner to see progress
              },
            ),
        ],
      ),
      body: drive == null
          ? const Center(child: Text('No drive selected', style: HudTheme.bodyText))
          : _buildBody(context, ref, drive.name),
    );
  }

  Widget _buildBody(BuildContext context, WidgetRef ref, String driveName) {
    final diffAsync = ref.watch(scanDiffProvider(driveName));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
          child: Row(
            children: [
              Text(
                'WHAT CHANGED',
                style: HudTheme.headerCyan.copyWith(
                  color: HudTheme.accentCyan,
                ),
              ),
              const SizedBox(width: 8),
              diffAsync.when(
                data: (diff) {
                  if (!diff.hasBaseline) return const SizedBox.shrink();
                  final dateStr = diff.baselineScannedAt != null
                      ? DateFormat('yyyy-MM-dd HH:mm')
                          .format(diff.baselineScannedAt!.toLocal())
                      : 'Unknown';
                  return Text(
                    '(since $dateStr)',
                    style: HudTheme.labelMuted,
                  );
                },
                loading: () => const SizedBox.shrink(),
                error: (_, __) => const SizedBox.shrink(),
              ),
            ],
          ),
        ),
        Expanded(
          child: diffAsync.when(
            data: (diff) => SingleChildScrollView(child: _buildContent(context, ref, diff)),
            loading: () => _buildLoading(),
            error: (err, _) => _buildError(context, err, ref, driveName),
          ),
        ),
      ],
    );
  }

  Widget _buildLoading() {
    return Padding(
      padding: const EdgeInsets.all(32.0),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const CircularProgressIndicator(color: HudTheme.accentCyan),
            const SizedBox(height: 16),
            Text('ANALYZING CHANGES...', style: HudTheme.labelMuted),
          ],
        ),
      ),
    );
  }

  Widget _buildError(BuildContext context, Object err, WidgetRef ref, String root) {
    if (err is ScanDiffNoScanException) {
      return Padding(
        padding: const EdgeInsets.all(32.0),
        child: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text('NO SCAN DATA — RUN A SCAN FIRST',
                  style: HudTheme.labelMuted),
              const SizedBox(height: 16),
              OutlinedButton(
                onPressed: () {
                  ref.read(directoryProvider.notifier).scanDirectory(root);
                  ref.read(storageModeProvider.notifier).state =
                      StorageMode.diskAnalyzer;
                  ref.read(storageViewProvider.notifier).state =
                      StorageView.analyzer;
                  Navigator.of(context).pop();
                },
                style: OutlinedButton.styleFrom(
                  foregroundColor: HudTheme.accentCyan,
                  side: const BorderSide(color: HudTheme.accentCyan),
                ),
                child: const Text('SCAN NOW'),
              ),
            ],
          ),
        ),
      );
    }
    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Center(
        child: Text('Error: $err',
            style: HudTheme.bodyText.copyWith(color: HudTheme.accentRed)),
      ),
    );
  }

  Widget _buildContent(BuildContext context, WidgetRef ref, ScanDiff diff) {
    if (!diff.hasBaseline) {
      return Padding(
        padding: const EdgeInsets.all(16.0),
        child: Text(
          'FIRST SCAN — BASELINE SAVED. NEXT SCAN WILL SHOW CHANGES.',
          style: HudTheme.bodyText.copyWith(color: HudTheme.textDim),
        ),
      );
    }

    if (diff.summary.addedCount == 0 &&
        diff.summary.removedCount == 0 &&
        diff.summary.grownCount == 0 &&
        diff.summary.shrunkCount == 0 &&
        diff.summary.netDeltaBytes == 0) {
      return Padding(
        padding: const EdgeInsets.all(16.0),
        child: Text(
          'NO CHANGES SINCE LAST SCAN',
          style: HudTheme.bodyText.copyWith(color: HudTheme.accentGreen),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _buildSummaryStrip(diff.summary),
        if (diff.added.isNotEmpty)
          _buildSection(ref, 'ADDED', diff.added, HudTheme.accentGreen),
        if (diff.removed.isNotEmpty)
          _buildSection(ref, 'REMOVED', diff.removed, HudTheme.accentRed),
        if (diff.grown.isNotEmpty)
          _buildSection(ref, 'GROWN', diff.grown, HudTheme.accentAmber),
        if (diff.shrunk.isNotEmpty)
          _buildSection(ref, 'SHRUNK', diff.shrunk, HudTheme.accentCyan),
        const SizedBox(height: 32),
      ],
    );
  }

  Widget _buildSummaryStrip(ScanDiffSummary summary) {
    final netColor = summary.netDeltaBytes > 0
        ? HudTheme.accentRed
        : (summary.netDeltaBytes < 0 ? HudTheme.accentGreen : HudTheme.textDim);
    final netPrefix = summary.netDeltaBytes > 0 ? '+' : '';

    return Container(
      color: Colors.white.withValues(alpha: 0.02),
      padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
      child: Wrap(
        spacing: 16.0,
        runSpacing: 8.0,
        alignment: WrapAlignment.spaceBetween,
        children: [
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _buildSummaryChip(
                  '+${summary.addedCount} ADDED', HudTheme.accentGreen),
              const SizedBox(width: 16),
              _buildSummaryChip(
                  '−${summary.removedCount} REMOVED', HudTheme.accentRed),
              const SizedBox(width: 16),
              _buildSummaryChip(
                  '▲${summary.grownCount} GROWN', HudTheme.accentAmber),
              const SizedBox(width: 16),
              _buildSummaryChip(
                  '▼${summary.shrunkCount} SHRUNK', HudTheme.accentCyan),
            ],
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              border: Border.all(color: netColor.withValues(alpha: 0.5)),
              borderRadius: BorderRadius.circular(4),
            ),
            child: Text(
              'NET: $netPrefix${_formatSize(summary.netDeltaBytes)}',
              style: HudTheme.bodyText
                  .copyWith(color: netColor, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryChip(String text, Color color) {
    return Text(
      text,
      style:
          HudTheme.bodyText.copyWith(color: color, fontWeight: FontWeight.bold),
    );
  }

  Widget _buildSection(
      WidgetRef ref, String title, List<ScanDiffEntry> entries, Color color) {
    return Theme(
      data: ThemeData(dividerColor: Colors.transparent),
      child: ExpansionTile(
        title: Text(
          '$title (${entries.length})',
          style: HudTheme.bodyText.copyWith(color: color),
        ),
        iconColor: color,
        collapsedIconColor: color,
        children: [
          ListView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            itemCount: entries.length,
            itemBuilder: (context, index) {
              final entry = entries[index];
              final prefix = entry.deltaBytes > 0 ? '+' : '';
              return InkWell(
                onTap: () => _jumpToDir(context, ref, entry.path),
                child: Container(
                  height: 40,
                  padding: const EdgeInsets.symmetric(horizontal: 16.0),
                  decoration: HudTheme.listItemDecoration,
                  child: Row(
                    children: [
                      Icon(
                        entry.isDirectory ? Icons.folder : Icons.insert_drive_file,
                        size: 16,
                        color:
                            entry.isDirectory ? HudTheme.accentAmber : HudTheme.accentGreen,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text.rich(
                          TextSpan(
                            children: [
                              TextSpan(
                                  text: entry.path,
                                  style: HudTheme.bodyText.copyWith(
                                      color: HudTheme.textMain)),
                              if (entry.childCount != null)
                                TextSpan(
                                  text: ' (${entry.childCount})',
                                  style: HudTheme.bodyText
                                      .copyWith(color: HudTheme.textDim),
                                ),
                            ],
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(width: 16),
                      Text(
                        '$prefix${_formatSize(entry.deltaBytes)}',
                        style: HudTheme.bodyText.copyWith(
                          color: color,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

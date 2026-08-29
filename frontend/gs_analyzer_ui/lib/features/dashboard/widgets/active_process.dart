import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/process_filter.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/process_table.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/status.dart';
import 'package:gs_analyzer_ui/providers/hud_density_provider.dart';
import 'package:gs_analyzer_ui/providers/process_explorer_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class ActiveProcess extends ConsumerStatefulWidget {
  const ActiveProcess({super.key});

  @override
  ConsumerState<ActiveProcess> createState() => _ActiveProcessState(); 
}

class _ActiveProcessState extends ConsumerState<ActiveProcess> {

  @override
  Widget build(BuildContext context) {
    final processes = ref.watch(filteredProcessesProvider).take(4).toList();
    final d = ref.watch(hudDensityProvider);
    final selectedPid = ref.watch(selectedProcessPidProvider);

    return CustomContainer(
      color: HudTheme.bgPanel,
      padding: EdgeInsets.all(20),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Text(
                  'ACTIVE PROCESS TREE',
                  style: HudTheme.labelMuted,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              ProcessFilter(),
            ],
          ),
          const SizedBox(height: 10,),
          ProcessTable(),
          ...processes.map((group) {
            final isSelected = group.primaryPid == selectedPid;
            final displayName = group.count > 1 ? '${group.name} (x${group.count})' : group.name;
            final isCpuHot = group.totalCpuPercent > 10.0;
            final isMemHot = group.totalPercentMem > 10.0;
            final isHot = isCpuHot || isMemHot;            
            final textColor = isHot ? HudTheme.accentAmber : HudTheme.textMain;

            return SizedBox(
              height: d.rowHeight + 16,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      group.primaryPid.toString(),
                      style: HudTheme.bodyText,
                    ),
                  ),
                  Expanded(
                    child: Text(
                      displayName,
                      overflow: TextOverflow.ellipsis,
                      style: HudTheme.bodyText.copyWith(color: isSelected ? HudTheme.accentCyan : textColor),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      group.primaryUser,
                      style: HudTheme.bodyText,
                    ),
                  ),
                  Expanded(
                    child: Text(
                      '${group.totalCpuPercent.toStringAsFixed(1)}%',
                      style: HudTheme.statGreen.copyWith(color: isCpuHot ? HudTheme.accentAmber : HudTheme.accentCyan),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      '${group.totalPercentMem.toStringAsFixed(1)}%',
                      style: HudTheme.statGreen.copyWith(color: textColor),
                    ),
                  ),
                  Expanded(
                    child: Align(
                      alignment: Alignment.centerLeft,
                      child: Status(
                        status: group.dominantStatus
                      ),
                    ),
                  ),
                ],
              ),
            );
          }),
        ],
      )
    );
  }
}
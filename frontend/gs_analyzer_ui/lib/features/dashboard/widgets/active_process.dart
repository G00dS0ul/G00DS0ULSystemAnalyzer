import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/custom_button.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/process_table.dart';
import 'package:gs_analyzer_ui/providers/hud_density_provider.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class ActiveProcess extends ConsumerStatefulWidget {
  const ActiveProcess({super.key});

  @override
  ConsumerState<ActiveProcess> createState() => _ActiveProcessState(); 
}

class _ActiveProcessState extends ConsumerState<ActiveProcess> {

  @override
  Widget build(BuildContext context) {
    final ramState = ref.watch(ramProvider);
    final d = ref.watch(hudDensityProvider);
    final processes = ramState.groupedProcesses.take(4).toList();
    return CustomContainer(
      color: Color(0xFF2A2A2A),
      padding: EdgeInsets.all(20),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'ACTIVE_PROCESS_TREE'
              ),
              Row(
                children: [
                  CustomContainer(
                    color: Colors.black,
                    padding: EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                    child: Text(
                      'FILTER'
                    ),
                  ),
                  const SizedBox(width: 10,),
                  Container(
                    color: Colors.black,
                    padding: EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                    child: Text(
                      'SORT BY CPU'
                    ),
                  )
                ],
              )
            ],
          ),
          const SizedBox(height: 10,),
          ProcessTable(),
          ...processes.map((group) {
            final displayName = group.count > 1 ? '${group.name} (x${group.count})' : group.name;
            return SizedBox(
              height: d.rowHeight + 16,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      group.primaryPid.toString()
                    ),
                  ),
                  Expanded(
                    child: Text(
                      displayName,
                    ),
                  ),
                  Expanded(
                    child: Text(
                      group.primaryUser
                    ),
                  ),
                  Expanded(
                    child: Text(
                      '${group.totalCpuPercent.toStringAsFixed(1)}%'
                    ),
                  ),
                  Expanded(
                    child: Text(
                      '${group.totalPercentMem.toStringAsFixed(1)}%'
                    ),
                  ),
                  CustomButton(
                    padding: EdgeInsets.symmetric(vertical: 2, horizontal: 5),
                    label: group.dominantStatus
                  )
                ],
              ),
            );
          }),
        ],
      )
    );
  }
}
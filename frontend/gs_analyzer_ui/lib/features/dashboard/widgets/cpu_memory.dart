import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/custom_progress_indicator.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class CpuMemory extends ConsumerStatefulWidget {
  const CpuMemory({super.key});

  @override
  ConsumerState<CpuMemory> createState() => _CpuMemoryState();
}

class _CpuMemoryState extends ConsumerState<CpuMemory>{
  String _formatGB(int bytes) => (bytes / (1024 * 1024 * 1024)).toStringAsFixed(1);

  @override
  Widget build(BuildContext context) {
    final ramstate = ref.watch(ramProvider);
    final drive = ref.watch(currentDriveProvider)!;

    return CustomContainer(
      color: Color(0xFF2A2A2A),
      padding: EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(
              'MEM_ALLOCATION'
            ),
            subtitle: RichText(
              text: TextSpan(
                text: '${drive.percentageUsed.toStringAsFixed(0)}',
                style: TextStyle(
                  fontSize: 28,
                  color: Colors.greenAccent
                ),
                children: [
                  TextSpan(
                    text: '%',
                    style: HudTheme.statGreen
                  ),
                  TextSpan(
                    text: ' of ${_formatGB(drive.totalBytes)}GB',
                    style: TextStyle(
                      fontSize: 14,
                      color: Colors.white
                    )
                  )
                ]
              )
            ),
            trailing: Icon(
              Icons.analytics_outlined,
              size: 35,
              color: Color(0xFF38453B),
            ),
          ),
          const SizedBox(height: 20,),
          CustomProgressIndicator(
            label: 'Active', 
            tag: '${ramstate.activeGb.toStringAsFixed(1)} GB', 
            value: ramstate.totalGb > 0 ? ramstate.activeGb / ramstate.totalGb : 0.0, 
            height: 6,
            color: AlwaysStoppedAnimation(Colors.greenAccent),
          ),
          const SizedBox(height: 20,),
          Row(
            children: [
              Expanded(
                child: CustomContainer(
                  color: Colors.black,
                  padding: EdgeInsets.all(8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'CACHED',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w500
                        ),
                      ),
                      const SizedBox(height: 4,),
                      Text(
                        '${ramstate.cacheGb.toStringAsFixed(1)} GB',
                        style: TextStyle(
                          fontSize: 18
                        ),
                      )
                    ],
                  )             
                ),
              ),
              const SizedBox(width: 10,),
              Expanded(
                child: CustomContainer(
                  color: Colors.black,
                  padding: EdgeInsets.all(8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'SWAP',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w500
                        ),
                      ),
                      const SizedBox(height: 4,),
                      Text(
                        '${ramstate.swapGb.toStringAsFixed(1)} GB',
                        style: TextStyle(
                          fontSize: 18
                        ),
                      )
                    ],
                  )             
                ),
              )
            ],
          ),
          const SizedBox(height: 10,),
        ],
      )
    );
  }
}
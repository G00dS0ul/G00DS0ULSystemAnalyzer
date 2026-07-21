import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/cpu_bar_chart.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class CpuLoadReport extends StatelessWidget {
  const CpuLoadReport({super.key});

  @override
  Widget build(BuildContext context) {
    return CustomContainer(
      color: Color(0xFF2A2A2A),
      padding: EdgeInsets.all(20),
      child: Column(
        children: [
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(
              'CPU_LOAD [AVG]'
            ),
            subtitle: Row(
              children: [
                RichText(
                  text: TextSpan(
                    text: '72',
                    style: TextStyle(
                      color: Color(0xFFACC3FC),
                      fontSize: 28
                    ),
                    children: [
                      TextSpan(
                        text: '%',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w300
                        )
                      )
                    ]
                  )
                ),
                const SizedBox(width: 5,),
                Icon(Icons.arrow_upward, size: 14, color: Colors.greenAccent,),
                Text(
                  '2.4%',
                  style: HudTheme.statGreen,
                )
              ],
            ),
            trailing: Icon(
              Icons.memory,
              size: 40,
              color: Color(0xFF383A3F),
            ),
          ),
          const SizedBox(height: 10,),
          CpuBarChart(
            values: [18, 32, 48, 20, 55, 37, 23, 45]
          ),
          const SizedBox(height: 10,),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'CORE 0-3',
                style: HudTheme.labelMuted,
              ),
              Text(
                'CORE 4-7',
                style: HudTheme.labelMuted,
              ),
              Text(
                'CORE 8-15',
                style: HudTheme.labelMuted,
              )
            ],
          )
        ],
      )
    );
  }
}
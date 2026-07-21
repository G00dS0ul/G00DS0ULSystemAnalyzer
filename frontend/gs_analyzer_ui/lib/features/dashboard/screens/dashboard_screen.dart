import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/active_process.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/cpu_load_report.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/cpu_memory.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/custom_button.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/net_rate.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/thermal_sensor.dart';
import 'package:gs_analyzer_ui/providers/hud_density_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';

class DashboardScreen extends ConsumerStatefulWidget {
  const DashboardScreen({super.key});

  @override
  ConsumerState<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends ConsumerState<DashboardScreen> {

  @override
  Widget build(BuildContext context) {
    final d = ref.watch(hudDensityProvider);

    return Container(
      child: ListView(
        padding: EdgeInsets.all(d.panelPad),
        children: [
          Padding(
            padding: EdgeInsets.all(d.panelPad),
            child: Column(
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'System_Overview'.toUpperCase(),
                          style:TextStyle(
                            fontSize: 28,
                            letterSpacing: 0.5,
                            fontWeight: FontWeight.w600
                          ),
                        ),
                        Text(
                          'Real-Time Telemetry Stream'.toUpperCase(),
                          style: HudTheme.bodyText,
                        )
                      ],
                    ),
                    CustomButton(
                      padding: EdgeInsets.symmetric(horizontal: 9, vertical: 5),
                      icon: Icons.circle,
                      label: 'SYSTEM STABLE',
                    )
                  ],
                ),
                const SizedBox(height: 30,),
                IntrinsicHeight(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        child: CpuLoadReport()
                      ),
                      const SizedBox(width: 15,),
                      Expanded(
                        child: CpuMemory()
                      ),
                      const SizedBox(width: 15,),
                      Expanded(
                        child: NetRate()
                      )
                    ],
                  ),
                ),
                const SizedBox(height: 25,),
                IntrinsicHeight(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        flex: 2,
                        child: ActiveProcess()
                      ),
                      const SizedBox(width: 15,),
                      Expanded(
                        flex: 1,
                        child: ThermalSensor()
                      )
                    ],
                  ),
                )
              ],
            ),
          ),
        ],
      ),
    );
  }
} 
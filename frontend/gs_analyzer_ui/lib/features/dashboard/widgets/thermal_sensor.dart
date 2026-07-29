import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/features/dashboard/widgets/custom_progress_indicator.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class ThermalSensor extends StatelessWidget {
  const ThermalSensor({super.key});

  @override
  Widget build(BuildContext context) {
    return CustomContainer(
      color: Color(0xFF2A2A2A),
      padding: EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'THERMAL SENSORS',
            style: HudTheme.labelMuted,
          ),
          const SizedBox(height: 17,),
          CustomContainer(
            color: Colors.black,
            child: ListTile(
              leading: Icon(Icons.thermostat, color: Color(0XFFFEB694),),
              title: Text(
                'CPU_PKG'
              ),
              trailing: Text(
                '68\u{00B0}C',
                style: TextStyle(
                  fontSize: 18
                ),
              ),
            ),
          ),
          const SizedBox(height: 15,),
          CustomContainer(
            color: Colors.black,
            child: ListTile(
              leading: Icon(Icons.thermostat, color: Color(0XFFA4B7ED),),
              title: Text(
                'SYS_BOARD'
              ),
              trailing: Text(
                '42\u{00B0}C',
                style: TextStyle(
                  fontSize: 18
                ),
              ),
            ),
          ),
          const SizedBox(height: 50,),
          CustomProgressIndicator(
            label: 'fan speed', 
            tag: '2400  rpm', 
            value: 0.8, 
            height: 4
          )
        ],
      )
    );
  }
}
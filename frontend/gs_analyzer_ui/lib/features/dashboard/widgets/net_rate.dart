import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class NetRate extends StatelessWidget {
  const NetRate({super.key});

  @override
  Widget build(BuildContext context) {
    return CustomContainer(
      color: Color(0xFF2A2A2A),
      padding: EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'NET IO [ETH0]',
            style: HudTheme.labelMuted,
          ),
          const SizedBox(height: 15,),
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: CircleAvatar(
              backgroundColor: Color(0xFF3B3D44),
              foregroundColor: Color(0xFFA4B7ED),
              radius: 18,
              child: Icon(
                Icons.arrow_downward
              ),
            ),
            title: Text(
              'RX_RATE',
              style: TextStyle(
                fontSize: 12,
                color: Colors.white60
              ),
            ),
            subtitle: RichText(
              text: TextSpan(
                text: '1.2 ',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold
                ),
                children: [
                  TextSpan(
                    text: ' GB/S',
                    style: TextStyle(
                      fontSize: 13,
                      color: Color(0xFFA4B7ED)
                    )
                  )
                ]
              )
            )
          ),
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: CircleAvatar(
              backgroundColor: Color(0xFF41342D),
              foregroundColor: Color(0xFFFEB694),
              radius: 18,
              child: Icon(
                Icons.arrow_upward
              ),
            ),
            title: Text(
              'TX_RATE',
              style: TextStyle(
                fontSize: 12,
                color: Colors.white60
              ),
            ),
            subtitle: RichText(
              text: TextSpan(
                text: '450 ',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold
                ),
                children: [
                  TextSpan(
                    text: ' MB/S',
                    style: TextStyle(
                      fontSize: 13,
                      color: Color(0xFFFEB694)
                    )
                  )
                ]
              )
            )
          ),
          const SizedBox(height: 60,)
        ],
      ), 
    );
  }
}
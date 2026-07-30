import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/custom_container.dart';

class NetRate extends StatelessWidget {
  const NetRate({super.key});

  @override
  Widget build(BuildContext context) {
    return ClipRect(
      child: Stack(
        children: [
          Opacity(
            opacity: 0.3,
            child: CustomContainer(
              color: HudTheme.bgPanel,
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
                      backgroundColor: HudTheme.accentCyan.withValues(alpha: 0.1),
                      foregroundColor: HudTheme.accentCyan,
                      radius: 18,
                      child: Icon(
                        Icons.arrow_downward
                      ),
                    ),
                    title: Text(
                      'RX RATE',
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
                              color: HudTheme.primaryBorder
                            )
                          )
                        ]
                      )
                    )
                  ),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      backgroundColor: HudTheme.accentAmber.withValues(alpha: 0.1),
                      foregroundColor: HudTheme.accentAmber,
                      radius: 18,
                      child: Icon(
                        Icons.arrow_upward
                      ),
                    ),
                    title: Text(
                      'TX RATE',
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
                              color: HudTheme.accentAmber
                            )
                          )
                        ]
                      )
                    )
                  ),
                  const SizedBox(height: 60,)
                ],
              ), 
            ),
          ),
          Positioned.fill(
            child: BackdropFilter(
              filter: ImageFilter.blur(sigmaX: 7.0, sigmaY: 7.0),
              child: Container(
                color: Colors.black.withOpacity(0.1), // Gives a slight tint to the blur
              ),
            ),
          ),
        ],
      ),
    );
  }
}
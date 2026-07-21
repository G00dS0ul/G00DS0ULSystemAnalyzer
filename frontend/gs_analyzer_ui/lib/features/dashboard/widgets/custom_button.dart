import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';

class CustomButton extends StatelessWidget {
  final EdgeInsets? padding;
  final IconData? icon;
  final String label;
  
  const CustomButton({
    this.padding,
    this.icon,
    required this.label,
    super.key
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: Color(0xFF14402B)
      ),
      child: Row(
        children: [
          Icon(
            icon,
            size: 9,
            color: Color(0xFF6DE390),
          ),
          const SizedBox(width: 5,),
          Text(
            label,
            style: TextStyle(
              color: Color(0xFF6DE390),
              fontWeight: FontWeight.w600,
              letterSpacing: 0.4
            ),
          )
        ],
      ),
    );
  }
}
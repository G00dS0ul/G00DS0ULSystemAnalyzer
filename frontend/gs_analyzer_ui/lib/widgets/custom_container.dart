import 'package:flutter/widgets.dart';

class CustomContainer extends StatelessWidget {
  final EdgeInsets? padding;
  final Widget child;
  final Color color;

  const CustomContainer({
    this.padding,
    required this.child,
    required this.color,
    super.key
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(5)
      ),
      child: child,
    );
  }
}
import 'package:flutter/material.dart';

class CpuBarChart extends StatelessWidget {
  final List<double> values;

  const CpuBarChart({
    super.key,
    required this.values
  });

  @override
  Widget build(BuildContext context) {
    final maxValue = values.reduce((a, b) => a > b ? a : b);

    return SizedBox(
      height: 110,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: List.generate(values.length, (index) {
          final value = values[index];
          final isHighest = value == maxValue;

          return Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 2),
              child: TweenAnimationBuilder<double>(
                duration: const Duration(milliseconds: 600),
                tween: Tween(
                  begin: 0,
                  end: (value / maxValue) * 95,
                ),
                curve: Curves.easeOut,
                builder: (_, height, __) {
                  return Align(
                    alignment: Alignment.bottomCenter,
                    child: Container(
                      height: height,
                      decoration: BoxDecoration(
                        color: isHighest
                            ? const Color(0xffAFC2FF)
                            : const Color(0xff6C7389),
                        borderRadius: BorderRadius.circular(3),
                        boxShadow: isHighest
                            ? [
                                BoxShadow(
                                  color: const Color.fromARGB(115, 175, 194, 255),
                                  blurRadius: 18,
                                  spreadRadius: 1,
                                )
                              ]
                            : null,
                      ),
                    ),
                  );
                },
              ),
            ),
          );
        }),
      ),
    );
  }
}
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/hud_density_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_label.dart';

class ProcessTable extends ConsumerStatefulWidget {
  const ProcessTable({super.key});

  @override
  ConsumerState<ConsumerStatefulWidget> createState() => _ProcessTableState();    
}

class _ProcessTableState extends ConsumerState<ProcessTable> {

  @override
  Widget build(BuildContext context) {
    final d = ref.watch(hudDensityProvider);
    return SizedBox(
      height: d.rowHeight + 16,
      child: Row(
        children: [
          Expanded(child: HudLabel('PID')),
          Expanded(child: HudLabel('COMMAND')),
          Expanded(child: HudLabel('USER')),
          Expanded(child: HudLabel('%CPU')),
          Expanded(child: HudLabel('%MEM')),
          Expanded(child: HudLabel('STATUS'))
        ],
      ),
    );
  }
}
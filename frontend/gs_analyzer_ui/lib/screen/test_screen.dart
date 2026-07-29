import 'package:gs_analyzer_ui/utils/logger.dart';
import 'package:flutter/material.dart';

import '../services/api_service.dart';

class TestScreen extends StatelessWidget {
  const TestScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ElevatedButton(
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 40, vertical: 20),
            backgroundColor: Colors.blueAccent,
          ),
          onPressed: () async {
            appLogger.i('INITIATING ANALYZER BRIDGE...');
            try {
              final service = ApiService();
              final nodes = await service.scanDirectory(
                'C:/',
                '00000000-0000-0000-0000-000000000000',
              );

              appLogger.i(
                'BRIDGE SUCCESS! found ${nodes.length} items in root.',
              );
              appLogger.i("--------------------------------------------");

              for (var node in nodes) {
                appLogger.i(
                  '[${node.type.toUpperCase()}] ${node.name} | ${node.sizeBytes} bytes | ${node.lastModified}',
                );
              }

              appLogger.i('--------------------------------------------');
            } catch (e) {
              appLogger.i('BRIDGE FAILED: $e');
            }
          },
          child: const Text(
            'TEST API BRIDGE',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
        ),
      ),
    );
  }
}

// frontend/gs_analyzer_ui/lib/main.dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/screen/master_layout.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/providers/window_provider.dart';
import 'package:gs_analyzer_ui/utils/globals.dart';
import 'package:window_manager/window_manager.dart';

const kCompactSize = Size(900, 640);

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await windowManager.ensureInitialized();

  const opts = WindowOptions(
    size: kCompactSize,
    minimumSize: Size(720, 520),
    center: true,
    titleBarStyle: TitleBarStyle.normal,
  );
  windowManager.waitUntilReadyToShow(opts, () async {
    await windowManager.setSize(kCompactSize);
    await windowManager.center();
    await windowManager.show();
    await windowManager.focus();
  });

  runApp(const ProviderScope(child: GSAnalyzerApp()));
}

class GSAnalyzerApp extends ConsumerStatefulWidget {
  const GSAnalyzerApp({super.key});

  @override
  ConsumerState<GSAnalyzerApp> createState() => _GSAnalyzerAppState();
}

class _GSAnalyzerAppState extends ConsumerState<GSAnalyzerApp>
    with WindowListener {
  @override
  void initState() {
    super.initState();
    windowManager.addListener(this);
    _checkInitialState();
  }

  Future<void> _checkInitialState() async {
    final isMax = await windowManager.isMaximized();
    ref.read(windowMaximizedProvider.notifier).state = isMax;
  }

  @override
  void dispose() {
    windowManager.removeListener(this);
    super.dispose();
  }

  @override
  void onWindowMaximize() {
    ref.read(windowMaximizedProvider.notifier).state = true;
  }

  @override
  void onWindowUnmaximize() {
    ref.read(windowMaximizedProvider.notifier).state = false;
    windowManager.setSize(kCompactSize);
    windowManager.center();
  }

  @override
  Widget build(BuildContext context) {
    final appearance = ref.watch(
      settingsProvider.select((s) => s.currentSettings?.appearance),
    );

    final accentColor = HudTheme.resolveAccent(appearance?.accentColor);
    final bgColor = HudTheme.resolveBgBase(appearance?.theme);

    return MaterialApp(
      scaffoldMessengerKey: snackbarKey,
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark().copyWith(
        scaffoldBackgroundColor: bgColor,
        colorScheme: ColorScheme.dark(primary: accentColor),
      ),
      home: const MasterLayout(),
    );
  }
}

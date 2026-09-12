import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/routing/app_router.dart';

class SurplusLinkApp extends StatefulWidget {
  const SurplusLinkApp({
    required this.authController,
    this.initialLocation = AppRoutes.splash,
    super.key,
  });

  final AuthController authController;
  final String initialLocation;

  @override
  State<SurplusLinkApp> createState() => _SurplusLinkAppState();
}

class _SurplusLinkAppState extends State<SurplusLinkApp> {
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    _router = createAppRouter(
      authController: widget.authController,
      initialLocation: widget.initialLocation,
    );
    unawaited(widget.authController.initialize());
  }

  @override
  void dispose() {
    _router.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => MaterialApp.router(
    title: 'SurplusLink',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF16794B)),
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
      ),
      useMaterial3: true,
    ),
    routerConfig: _router,
  );
}

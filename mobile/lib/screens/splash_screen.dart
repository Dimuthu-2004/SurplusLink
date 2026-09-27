import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: const Color(0xFFFFFDF9),
    body: SafeArea(
      child: LayoutBuilder(
        builder: (context, constraints) => Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Lottie.asset(
                animationAsset,
                key: const Key('session-loading'),
                decoder: decodeAnimation,
                width: (constraints.maxWidth * .65).clamp(0, 280),
                height: (constraints.maxHeight * .45).clamp(0, 280),
                fit: BoxFit.contain,
                animate: !MediaQuery.disableAnimationsOf(context),
                errorBuilder: (_, _, _) => const SizedBox(
                  width: 32,
                  height: 32,
                  child: CircularProgressIndicator(),
                ),
              ),
              const SizedBox(height: 16),
              const SurplusLinkLogo(size: 40),
              const SizedBox(height: 12),
              const Text('Restoring your session…'),
            ],
          ),
        ),
      ),
    ),
  );

  static const animationAsset =
      'assets/animations/architecture_and_construction.lottie';
  static Future<LottieComposition?> decodeAnimation(List<int> bytes) =>
      LottieComposition.decodeZip(
        bytes,
        filePicker: (files) =>
            files.firstWhere((file) => file.name == 'animations/12345.json'),
      );
}

import 'package:flutter/material.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(
    backgroundColor: SurplusLinkTheme.surfaceSoft,
    body: Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SurplusLinkLogo(size: 88),
          SizedBox(height: 20),
          CircularProgressIndicator(key: Key('session-loading')),
          SizedBox(height: 16),
          Text('Restoring your session…'),
        ],
      ),
    ),
  );
}

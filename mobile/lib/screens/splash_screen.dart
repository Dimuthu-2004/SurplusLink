import 'package:flutter/material.dart';

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(
    body: Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.recycling, size: 64),
          SizedBox(height: 20),
          CircularProgressIndicator(key: Key('session-loading')),
          SizedBox(height: 16),
          Text('Restoring your session…'),
        ],
      ),
    ),
  );
}

import 'package:flutter/material.dart';

void main() => runApp(const SurplusLinkApp());

class SurplusLinkApp extends StatelessWidget {
  const SurplusLinkApp({super.key});

  @override
  Widget build(BuildContext context) => const MaterialApp(
        title: 'SurplusLink',
        home: Scaffold(body: Center(child: Text('SurplusLink mobile scaffold'))),
      );
}

import 'package:flutter/material.dart';

class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    required this.title,
    required this.subtitle,
    required this.child,
    super.key,
  });

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(child: Center(child: SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: ConstrainedBox(constraints: const BoxConstraints(maxWidth: 460), child: Card(
        child: Padding(padding: const EdgeInsets.all(28), child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Container(width: 52, height: 52, alignment: Alignment.center, decoration: BoxDecoration(color: const Color(0xFF0F172A), borderRadius: BorderRadius.circular(14)), child: const Text('S', style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.w900))),
          const SizedBox(height: 24),
          Text('SURPLUSLINK', style: Theme.of(context).textTheme.labelSmall?.copyWith(color: const Color(0xFFC2410C), fontWeight: FontWeight.w900, letterSpacing: 1.2)),
          const SizedBox(height: 8), Text(title, style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w900)),
          const SizedBox(height: 8), Text(subtitle, style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: const Color(0xFF475569))),
          const SizedBox(height: 28), child,
        ])),
      )),
    ))),
  );
}

import 'package:flutter/material.dart';

class SurplusLinkLogo extends StatelessWidget {
  const SurplusLinkLogo({
    this.size = 36,
    this.showWordmark = true,
    super.key,
  });

  final double size;
  final bool showWordmark;

  @override
  Widget build(BuildContext context) => Semantics(
    label: 'SurplusLink',
    image: true,
    child: Image.asset(
      showWordmark
          ? 'assets/branding/surpluslink-logo.png'
          : 'assets/branding/surpluslink-mark.png',
      width: showWordmark ? null : size,
      height: size,
      fit: BoxFit.contain,
      filterQuality: FilterQuality.high,
    ),
  );
}

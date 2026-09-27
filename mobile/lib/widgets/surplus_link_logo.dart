import 'package:flutter/material.dart';

class SurplusLinkLogo extends StatelessWidget {
  const SurplusLinkLogo({this.size = 36, this.showWordmark = true, super.key});

  final double size;
  final bool showWordmark;

  @override
  Widget build(BuildContext context) => Semantics(
    label: 'SurplusLink',
    image: true,
    child: showWordmark
        ? Image.asset(
            'assets/branding/surpluslink_logo_transparent.png',
            height: size,
            fit: BoxFit.contain,
            filterQuality: FilterQuality.high,
          )
        : ClipRect(
            child: Align(
              alignment: Alignment.centerLeft,
              widthFactor: .22,
              child: Image.asset(
                'assets/branding/surpluslink_logo_transparent.png',
                height: size,
                fit: BoxFit.contain,
                filterQuality: FilterQuality.high,
              ),
            ),
          ),
  );
}

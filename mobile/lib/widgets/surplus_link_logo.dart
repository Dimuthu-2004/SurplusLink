import 'package:flutter/material.dart';

class SurplusLinkLogo extends StatelessWidget {
  const SurplusLinkLogo({
    this.size = 36,
    this.showWordmark = true,
    this.foregroundColor,
    super.key,
  });

  final double size;
  final bool showWordmark;
  final Color? foregroundColor;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      ClipRRect(
        borderRadius: BorderRadius.circular(size * .22),
        child: Image.asset(
          'assets/images/surpluslink_logo.png',
          width: size,
          height: size,
          fit: BoxFit.cover,
          errorBuilder: (_, _, _) => Container(
            width: size,
            height: size,
            color: foregroundColor ?? const Color(0xFF0B1F3A),
            alignment: Alignment.center,
            child: Icon(Icons.recycling, size: size * .62, color: Colors.white),
          ),
        ),
      ),
      if (showWordmark) ...[
        const SizedBox(width: 10),
        Text(
          'SurplusLink',
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
            color: const Color(0xFF0B1F3A),
            fontWeight: FontWeight.w900,
          ),
        ),
      ],
    ],
  );
}

import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';

class StatusAnimation extends StatelessWidget {
  const StatusAnimation({required this.asset, required this.semanticLabel, this.size = 180, this.repeat = false, this.onLoaded, super.key});
  final String asset;
  final String semanticLabel;
  final double size;
  final bool repeat;
  final void Function(LottieComposition)? onLoaded;

  @override
  Widget build(BuildContext context) {
    final reduceMotion = MediaQuery.disableAnimationsOf(context);
    return Semantics(
      image: true,
      label: semanticLabel,
      child: SizedBox.square(
        dimension: size,
        child: Lottie.asset(asset, repeat: !reduceMotion && repeat, animate: !reduceMotion, onLoaded: onLoaded),
      ),
    );
  }
}

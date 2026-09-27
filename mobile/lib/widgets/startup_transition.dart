import 'package:flutter/material.dart';
import 'package:mobile/screens/splash_screen.dart';

/// Presentation only: initialization and routing continue under this cover.
/// No minimum playback timer or authentication dependency on Lottie.
class StartupTransition extends StatefulWidget {
  const StartupTransition({
    required this.initializing,
    required this.child,
    super.key,
  });
  final bool initializing;
  final Widget child;

  @override
  State<StartupTransition> createState() => _StartupTransitionState();
}

class _StartupTransitionState extends State<StartupTransition> {
  bool _visible = true;
  bool _finished = false;

  @override
  void initState() {
    super.initState();
    _finishWhenReady();
  }

  @override
  void didUpdateWidget(StartupTransition oldWidget) {
    super.didUpdateWidget(oldWidget);
    _finishWhenReady();
  }

  void _finishWhenReady() {
    if (!widget.initializing && _visible) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && !widget.initializing) setState(() => _visible = false);
      });
    }
  }

  @override
  Widget build(BuildContext context) => Stack(
    fit: StackFit.expand,
    children: [
      Offstage(offstage: widget.initializing, child: widget.child),
      if (!_finished)
        IgnorePointer(
          ignoring: !_visible,
          child: ExcludeSemantics(
            child: AnimatedOpacity(
              opacity: _visible ? 1 : 0,
              duration: MediaQuery.disableAnimationsOf(context)
                  ? Duration.zero
                  : const Duration(milliseconds: 220),
              onEnd: () {
                if (!_visible) setState(() => _finished = true);
              },
              child: const SplashScreen(),
            ),
          ),
        ),
    ],
  );
}

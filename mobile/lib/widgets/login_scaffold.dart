import 'package:flutter/material.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

/// Login-only presentation; authentication and form state live in LoginScreen.
class LoginScaffold extends StatelessWidget {
  const LoginScaffold({
    required this.title,
    required this.subtitle,
    required this.child,
    super.key,
  });

  final String title, subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) => Stack(
    fit: StackFit.expand,
    children: [
      const Positioned.fill(child: _LoginBackground()),
      Scaffold(
        backgroundColor: Colors.transparent,
        body: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) => SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
              child: ConstrainedBox(
                constraints: BoxConstraints(
                  minHeight: (constraints.maxHeight - 48).clamp(
                    0,
                    double.infinity,
                  ),
                ),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 440),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // Match the React brand mark; the PNG currently contains
                        // the Flutter placeholder, not the SurplusLink logo.
                        Semantics(
                          label: 'SurplusLink',
                          image: true,
                          child: ExcludeSemantics(
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Container(
                                  width: 44,
                                  height: 44,
                                  alignment: Alignment.center,
                                  decoration: BoxDecoration(
                                    color: SurplusLinkTheme.amber,
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: const Text(
                                    'S',
                                    style: TextStyle(
                                      color: Colors.white,
                                      fontSize: 28,
                                      fontWeight: FontWeight.w900,
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 12),
                                const Flexible(
                                  child: Text(
                                    'SurplusLink',
                                    style: TextStyle(
                                      color: Colors.white,
                                      fontSize: 26,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(height: 28),
                        Container(
                          key: const Key('login-glass-card'),
                          padding: const EdgeInsets.all(24),
                          decoration: BoxDecoration(
                            // A translucent surface gives glass depth without
                            // continuously blurring the moving photograph.
                            color: Colors.white.withValues(alpha: .91),
                            borderRadius: BorderRadius.circular(24),
                            border: Border.all(
                              color: Colors.white.withValues(alpha: .7),
                            ),
                            boxShadow: [
                              BoxShadow(
                                color: Colors.black.withValues(alpha: .18),
                                blurRadius: 32,
                                offset: const Offset(0, 12),
                              ),
                            ],
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Align(
                                alignment: Alignment.centerLeft,
                                child: Container(
                                  width: 36,
                                  height: 4,
                                  decoration: BoxDecoration(
                                    color: SurplusLinkTheme.amber,
                                    borderRadius: BorderRadius.circular(2),
                                  ),
                                ),
                              ),
                              const SizedBox(height: 16),
                              Text(
                                title,
                                style: Theme.of(context).textTheme.headlineSmall
                                    ?.copyWith(
                                      color: SurplusLinkTheme.slate900,
                                      fontWeight: FontWeight.w800,
                                    ),
                              ),
                              const SizedBox(height: 8),
                              Text(
                                subtitle,
                                style: Theme.of(context).textTheme.bodyLarge
                                    ?.copyWith(
                                      color: SurplusLinkTheme.slate600,
                                    ),
                              ),
                              const SizedBox(height: 24),
                              child,
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    ],
  );
}

class _LoginBackground extends StatefulWidget {
  const _LoginBackground();
  @override
  State<_LoginBackground> createState() => _LoginBackgroundState();
}

class _LoginBackgroundState extends State<_LoginBackground>
    with SingleTickerProviderStateMixin {
  late final AnimationController _zoom = AnimationController(
    vsync: this,
    duration: const Duration(seconds: 24),
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (MediaQuery.disableAnimationsOf(context)) {
      _zoom.stop();
      _zoom.value = 0;
    } else {
      // One slow entrance, then no idle animation/rendering work.
      _zoom.forward();
    }
  }

  @override
  void dispose() {
    _zoom.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ExcludeSemantics(
    child: IgnorePointer(
      child: ClipRect(
        child: Stack(
          fit: StackFit.expand,
          children: [
            const ColoredBox(color: SurplusLinkTheme.slate900),
            AnimatedBuilder(
              animation: _zoom,
              builder: (context, child) => Transform.scale(
                key: const Key('login-background-zoom'),
                scale: 1 + .035 * Curves.easeInOut.transform(_zoom.value),
                child: child,
              ),
              child: RepaintBoundary(
                child: Image.asset(
                  'assets/images/login_background.jpg',
                  key: const Key('login-background-image'),
                  fit: BoxFit.cover,
                  alignment: const Alignment(.15, 0),
                  filterQuality: FilterQuality.low,
                  // Bound decoded memory while retaining the portrait source.
                  cacheWidth: 768,
                  gaplessPlayback: true,
                  errorBuilder: (_, _, _) => const SizedBox.expand(),
                ),
              ),
            ),
            const DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Color(0xC70B1F3A),
                    Color(0x800B1F3A),
                    Color(0xDB0B1F3A),
                  ],
                  stops: [0, .5, 1],
                ),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

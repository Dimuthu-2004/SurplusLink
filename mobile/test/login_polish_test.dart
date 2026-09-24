import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/screens/login_screen.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/login_scaffold.dart';

import 'support/fakes.dart';

Future<void> showLogin(
  WidgetTester tester, {
  bool reducedMotion = false,
  double keyboard = 0,
  double textScale = 1,
}) async {
  final auth = AuthController(FakeAuthGateway());
  addTearDown(auth.dispose);
  await tester.pumpWidget(
    MaterialApp(
      theme: SurplusLinkTheme.light,
      home: MediaQuery(
        data: MediaQueryData(
          disableAnimations: reducedMotion,
          viewInsets: EdgeInsets.only(bottom: keyboard),
          textScaler: TextScaler.linear(textScale),
        ),
        child: LoginScreen(authController: auth),
      ),
    ),
  );
  await tester.pump();
}

void main() {
  testWidgets('local cover image, translucent card and visible brand', (
    tester,
  ) async {
    await showLogin(tester);
    final image = tester.widget<Image>(
      find.byKey(const Key('login-background-image')),
    );
    expect(image.fit, BoxFit.cover);
    expect(image.image, isA<ResizeImage>());
    expect(find.text('SurplusLink'), findsOneWidget);
    final card = tester.widget<Container>(
      find.byKey(const Key('login-glass-card')),
    );
    expect((card.decoration! as BoxDecoration).color!.a, closeTo(.91, .01));
    expect(find.byType(BackdropFilter), findsNothing);
    expect(find.byType(LoginScaffold), findsOneWidget);
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
  });

  testWidgets('slow zoom is subtle, settles, and respects reduced motion', (
    tester,
  ) async {
    await showLogin(tester);
    double scale() => tester
        .widget<Transform>(find.byKey(const Key('login-background-zoom')))
        .transform
        .entry(0, 0);
    final start = scale();
    await tester.pump(const Duration(seconds: 12));
    expect(scale(), greaterThan(start));
    expect(scale(), lessThanOrEqualTo(1.035));
    await tester.pump(const Duration(seconds: 13));
    await tester.pumpAndSettle();
    expect(tester.binding.hasScheduledFrame, isFalse);
    await showLogin(tester, reducedMotion: true);
    final still = scale();
    await tester.pump(const Duration(seconds: 12));
    expect(scale(), still);
  });

  for (final size in [
    const Size(320, 568),
    const Size(430, 932),
    const Size(844, 390),
  ]) {
    testWidgets('form scrolls with keyboard and large text at $size', (
      tester,
    ) async {
      await tester.binding.setSurfaceSize(size);
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await showLogin(
        tester,
        reducedMotion: true,
        keyboard: 180,
        textScale: 1.3,
      );
      await tester.ensureVisible(find.byKey(const Key('login-submit')));
      await tester.tap(find.byKey(const Key('login-submit')));
      await tester.pumpAndSettle();
      expect(find.text('Enter a valid email address.'), findsOneWidget);
      expect(
        find.text('Password must contain at least 8 characters.'),
        findsOneWidget,
      );
      await tester.ensureVisible(find.byKey(const Key('go-register')));
      expect(tester.takeException(), isNull);
    });
  }
}

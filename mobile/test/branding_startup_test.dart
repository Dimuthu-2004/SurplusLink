import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lottie/lottie.dart';
import 'package:mobile/screens/splash_screen.dart';
import 'package:mobile/widgets/startup_transition.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('bundled brand PNG has transparent background pixels', () async {
    final data = await rootBundle.load(
      'assets/branding/surpluslink_logo_transparent.png',
    );
    final codec = await ui.instantiateImageCodec(data.buffer.asUint8List());
    final image = (await codec.getNextFrame()).image;
    final pixels = (await image.toByteData(
      format: ui.ImageByteFormat.rawRgba,
    ))!;
    expect(pixels.getUint8(3), 0);
    expect(pixels.getUint8(pixels.lengthInBytes - 1), 0);
    image.dispose();
    codec.dispose();
  });

  test(
    'uploaded dotLottie decodes its actual animation and embedded image',
    () async {
      final data = await rootBundle.load(SplashScreen.animationAsset);
      final composition = await SplashScreen.decodeAnimation(
        data.buffer.asUint8List(),
      );
      expect(composition, isNotNull);
      expect(composition!.duration, greaterThan(Duration.zero));
      expect(composition.images, isNotEmpty);
    },
  );

  testWidgets(
    'startup is responsive, animated and uses a small transparent logo',
    (tester) async {
      tester.view.physicalSize = const Size(320, 568);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(const MaterialApp(home: SplashScreen()));
      await tester.pump(const Duration(milliseconds: 100));
      expect(find.byType(LottieBuilder), findsOneWidget);
      expect(
        tester.getSize(find.byType(LottieBuilder)).width,
        lessThanOrEqualTo(280),
      );
      expect(
        tester.widget<SurplusLinkLogo>(find.byType(SurplusLinkLogo)).size,
        40,
      );
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets(
    'fast initialization fades into app without waiting for animation completion',
    (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: StartupTransition(
            initializing: false,
            child: Scaffold(body: Text('Ready')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 250));
      await tester.pump();
      expect(find.byType(SplashScreen), findsNothing);
      expect(find.text('Ready'), findsOneWidget);
    },
  );
}

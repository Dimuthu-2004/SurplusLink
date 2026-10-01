import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lottie/lottie.dart';
import 'package:mobile/widgets/submission_animation_overlays.dart';

void main() {
  testWidgets('submitted overlay uses a non-repeating animation', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Material(
          child: SubmittedAnimationOverlay(
            title: 'Listing submitted',
            message: 'Your material was sent for manager review.',
          ),
        ),
      ),
    );

    expect(find.byKey(const Key('submitted-animation-title')), findsOneWidget);
    expect(tester.widget<Lottie>(find.byType(Lottie)).repeat, isFalse);
  });

  testWidgets('workflow overlay uses a looping animation', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Material(
          child: WorkflowRunningOverlay(
            title: 'Finding suitable matches...',
            message: 'Our matching workflow is checking available sellers.',
          ),
        ),
      ),
    );

    expect(find.byKey(const Key('workflow-running-title')), findsOneWidget);
    expect(tester.widget<Lottie>(find.byType(Lottie)).repeat, isTrue);
  });
}

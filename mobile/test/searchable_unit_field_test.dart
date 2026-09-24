import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/categories/searchable_unit_field.dart';

void main() {
  testWidgets('filters units, supports keyboard selection and rejects arbitrary typing', (tester) async {
    final form = GlobalKey<FormState>();
    String? selected;
    await tester.pumpWidget(MaterialApp(home: Scaffold(body: Form(key: form,
      child: SearchableUnitField(units: const ['kg', 'box', 'l'], onChanged: (value) => selected = value)))));
    await tester.enterText(find.byType(TextField), 'bo');
    await tester.pumpAndSettle();
    expect(find.text('box'), findsOneWidget);
    expect(find.text('kg'), findsNothing);
    await tester.tap(find.text('box'));
    expect(selected, 'box');
    expect(form.currentState!.validate(), isTrue);
    await tester.enterText(find.byType(TextField), 'k');
    await tester.pumpAndSettle();
    expect(selected, isNull);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pumpAndSettle();
    expect(selected, 'kg');
    await tester.enterText(find.byType(TextField), 'invented');
    expect(selected, isNull);
    expect(form.currentState!.validate(), isFalse);
  });

  testWidgets('shows familiar labels while returning canonical storage values', (tester) async {
    String? selected;
    await tester.pumpWidget(MaterialApp(home: Scaffold(body: SearchableUnitField(
      units: const ['l', 'm2'], onChanged: (value) => selected = value))));
    await tester.enterText(find.byType(TextField), 'L'); await tester.pumpAndSettle();
    await tester.tap(find.text('L').last); await tester.pumpAndSettle();
    expect(selected, 'l');
    await tester.enterText(find.byType(TextField), 'm'); await tester.pumpAndSettle();
    await tester.tap(find.text('m²')); await tester.pumpAndSettle();
    expect(selected, 'm2');
  });
}

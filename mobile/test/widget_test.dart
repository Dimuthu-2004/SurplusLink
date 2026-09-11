import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/main.dart';

void main() {
  testWidgets('renders scaffold', (WidgetTester tester) async {
    await tester.pumpWidget(const SurplusLinkApp());
    expect(find.text('SurplusLink mobile scaffold'), findsOneWidget);
  });
}

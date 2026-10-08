import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/auth/password_validation.dart';

void main() {
  test('password policy matches the backend boundaries and complexity rules', () {
    expect(validatePasswordPolicy('abcdefgh'), isNotNull);
    expect(validatePasswordPolicy('Abcdefgh'), isNotNull);
    expect(validatePasswordPolicy('Abcdefg1'), isNotNull);
    expect(validatePasswordPolicy('Abcdefg1!'), isNull);
    expect(validatePasswordPolicy('${'A'}bcdefg1!${'x' * 120}'), isNotNull);
  });
}

const passwordPolicyMessage = 'Password must be 8–128 characters and include uppercase, lowercase, a number, and a special character.';

String? validatePasswordPolicy(String? value) {
  final password = value ?? '';
  if (password.length < 8 || password.length > 128 ||
      !RegExp(r'[A-Z]').hasMatch(password) || !RegExp(r'[a-z]').hasMatch(password) ||
      !RegExp(r'\d').hasMatch(password) || !RegExp(r'[^A-Za-z0-9]').hasMatch(password)) {
    return passwordPolicyMessage;
  }
  return null;
}

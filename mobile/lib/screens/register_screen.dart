import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/widgets/auth_error_message.dart';
import 'package:mobile/widgets/auth_scaffold.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({required this.authController, super.key});

  final AuthController authController;

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  AppRole _role = AppRole.buyer;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) {
      return;
    }
    await widget.authController.register(
      email: _emailController.text,
      password: _passwordController.text,
      role: _role,
    );
  }

  @override
  Widget build(BuildContext context) => AuthScaffold(
    title: 'Create your account',
    subtitle: 'Join as a material seller or buyer.',
    child: AnimatedBuilder(
      animation: widget.authController,
      builder: (context, child) => Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AuthErrorMessage(widget.authController.errorMessage),
            TextFormField(
              key: const Key('register-email'),
              controller: _emailController,
              keyboardType: TextInputType.emailAddress,
              autofillHints: const [AutofillHints.newUsername],
              decoration: const InputDecoration(labelText: 'Email'),
              validator: _validateEmail,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-password'),
              controller: _passwordController,
              obscureText: true,
              autofillHints: const [AutofillHints.newPassword],
              decoration: const InputDecoration(labelText: 'Password'),
              validator: _validatePassword,
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<AppRole>(
              key: const Key('register-role'),
              initialValue: _role,
              decoration: const InputDecoration(labelText: 'Account type'),
              items: const [AppRole.seller, AppRole.buyer]
                  .map(
                    (role) =>
                        DropdownMenuItem(value: role, child: Text(role.label)),
                  )
                  .toList(),
              onChanged: widget.authController.isBusy
                  ? null
                  : (role) => setState(() => _role = role ?? AppRole.buyer),
            ),
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('register-submit'),
              onPressed: widget.authController.isBusy ? null : _submit,
              child: widget.authController.isBusy
                  ? const SizedBox.square(
                      dimension: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Register'),
            ),
            const SizedBox(height: 12),
            TextButton(
              key: const Key('go-login'),
              onPressed: widget.authController.isBusy
                  ? null
                  : () {
                      widget.authController.clearError();
                      context.go(AppRoutes.login);
                    },
              child: const Text('Already have an account? Sign in'),
            ),
          ],
        ),
      ),
    ),
  );
}

String? _validateEmail(String? value) {
  final email = value?.trim() ?? '';
  if (email.isEmpty || !email.contains('@') || !email.contains('.')) {
    return 'Enter a valid email address.';
  }
  return null;
}

String? _validatePassword(String? value) {
  if (value == null || value.length < 8) {
    return 'Password must contain at least 8 characters.';
  }
  return null;
}

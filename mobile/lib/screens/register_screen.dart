import 'package:mobile/widgets/profile_fields.dart';
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
  final _profile = ProfileFieldsController();
  int _step = 0;
  String? _usage;

  @override
  void dispose() {
    _profile.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) {
      return;
    }
    if (_step == 0) {
      setState(() => _step = 1);
      return;
    }
    final registered = await widget.authController.register(
      email: _emailController.text,
      password: _passwordController.text,
      roles: switch (_usage) {
        'sell' => [AppRole.seller],
        'buy' => [AppRole.buyer],
        _ => [AppRole.seller, AppRole.buyer],
      },
      profile: _profile.profile,
    );
    if (registered && mounted) context.go('${AppRoutes.verifyEmail}?email=${Uri.encodeComponent(_emailController.text.trim())}');
  }

  @override
  Widget build(BuildContext context) => AuthScaffold(
    title: 'Create your account',
    subtitle: _step == 0
        ? 'Start with your account details.'
        : 'How will you use SurplusLink?',
    child: AnimatedBuilder(
      animation: widget.authController,
      builder: (context, child) => Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AuthErrorMessage(widget.authController.errorMessage),
            if (_step == 0) ...[
              ProfileFields(
                controller: _profile,
                enabled: !widget.authController.isBusy,
              ),
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
            ],
            if (_step == 1) ...[
              DropdownButtonFormField<String>(
                isExpanded: true,
                key: const Key('register-usage'),
                initialValue: _usage,
                decoration: const InputDecoration(
                  labelText: 'How will you use SurplusLink?',
                ),
                items: const [
                  DropdownMenuItem(
                    value: 'sell',
                    child: Text('Sell surplus materials'),
                  ),
                  DropdownMenuItem(
                    value: 'buy',
                    child: Text('Buy / request materials'),
                  ),
                  DropdownMenuItem(value: 'both', child: Text('Both')),
                ],
                validator: (value) => value == null
                    ? 'Choose how you will use SurplusLink.'
                    : null,
                onChanged: widget.authController.isBusy
                    ? null
                    : (value) => setState(() => _usage = value),
              ),
              TextButton(
                onPressed: widget.authController.isBusy
                    ? null
                    : () => setState(() => _step = 0),
                child: const Text('Back to account details'),
              ),
            ],
            const SizedBox(height: 24),
            FilledButton(
              key: const Key('register-submit'),
              onPressed: widget.authController.isBusy ? null : _submit,
              child: widget.authController.isBusy
                  ? const SizedBox.square(
                      dimension: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(_step == 0 ? 'Continue' : 'Register'),
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

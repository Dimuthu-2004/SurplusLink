import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/auth_error_message.dart';
import 'package:mobile/widgets/login_scaffold.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({
    required this.authController,
    this.initialEmail = '',
    super.key,
  });

  final AuthController authController;
  final String initialEmail;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _showVerification = false;
  int _resendSeconds = 0;
  Timer? _resendTimer;
  bool _switchingAuthMode = false;

  Future<void> _goToRegister() async {
    setState(() => _switchingAuthMode = true);
    await Future<void>.delayed(const Duration(milliseconds: 180));
    if (mounted) context.go(AppRoutes.register);
  }

  @override
  void initState() {
    super.initState();
    _emailController.text = widget.initialEmail;
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _resendTimer?.cancel();
    super.dispose();
  }

  Future<void> _submit() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) {
      return;
    }
    final result = await widget.authController.login(
      email: _emailController.text,
      password: _passwordController.text,
    );
    if (result == LoginResult.emailNotVerified && mounted) {
      setState(() => _showVerification = true);
    }
  }

  void _openVerification() {
    widget.authController.clearError();
    final email = Uri.encodeQueryComponent(_emailController.text.trim());
    context.go(
      '${AppRoutes.verifyEmail}?email=$email&cooldown=$_resendSeconds',
    );
  }

  Future<void> _resendVerification() async {
    if (_resendSeconds > 0 || widget.authController.isBusy) return;
    final sent = await widget.authController.resendVerification(
      email: _emailController.text.trim(),
    );
    if (!sent || !mounted) return;
    setState(() => _resendSeconds = 60);
    _resendTimer?.cancel();
    _resendTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) return timer.cancel();
      setState(() {
        if (_resendSeconds <= 1) {
          _resendSeconds = 0;
          timer.cancel();
        } else {
          _resendSeconds--;
        }
      });
    });
  }

  @override
  Widget build(BuildContext context) => LoginScaffold(
    showIllustration: !_switchingAuthMode && !widget.authController.isBusy,
    title: 'Welcome to SurplusLink',
    subtitle: 'Sign in to continue.',
    child: AnimatedBuilder(
      animation: widget.authController,
      builder: (context, child) => AnimatedSwitcher(
        duration: const Duration(milliseconds: 320),
        switchInCurve: Curves.easeOutCubic,
        switchOutCurve: Curves.easeInCubic,
        transitionBuilder: (child, animation) => FadeTransition(
          opacity: animation,
          child: SlideTransition(
            position: Tween(
              begin: const Offset(.08, 0),
              end: Offset.zero,
            ).animate(animation),
            child: child,
          ),
        ),
        child: _showVerification
            ? _UnverifiedPanel(
                key: const ValueKey('unverified-panel'),
                email: _emailController.text.trim(),
                isBusy: widget.authController.isBusy,
                errorMessage:
                    widget.authController.errorCode == 'EMAIL_NOT_VERIFIED'
                    ? null
                    : widget.authController.errorMessage,
                resendSeconds: _resendSeconds,
                onVerify: _openVerification,
                onResend: _resendVerification,
              )
            : Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    AuthErrorMessage(widget.authController.errorMessage),
                    TextFormField(
                      key: const Key('login-email'),
                      controller: _emailController,
                      keyboardType: TextInputType.emailAddress,
                      autofillHints: const [AutofillHints.email],
                      decoration: const InputDecoration(labelText: 'Email'),
                      validator: _validateEmail,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      key: const Key('login-password'),
                      controller: _passwordController,
                      obscureText: true,
                      autofillHints: const [AutofillHints.password],
                      decoration: const InputDecoration(labelText: 'Password'),
                      validator: _validatePassword,
                      onFieldSubmitted: (_) => _submit(),
                    ),
                    const SizedBox(height: 24),
                    FilledButton(
                      key: const Key('login-submit'),
                      onPressed: widget.authController.isBusy ? null : _submit,
                      child: widget.authController.isBusy
                          ? const SizedBox.square(
                              dimension: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Text('Sign in'),
                    ),
                    const SizedBox(height: 12),
                    TextButton(
                      onPressed: widget.authController.isBusy
                          ? null
                          : () => context.go(AppRoutes.forgotPassword),
                      child: const Text('Forgot password?'),
                    ),
                    const SizedBox(height: 12),
                    TextButton(
                      key: const Key('go-register'),
                      onPressed: widget.authController.isBusy
                          ? null
                          : () {
                              widget.authController.clearError();
                              _goToRegister();
                            },
                      child: const Text('Create an account'),
                    ),
                    const SizedBox(height: 12),
                    const Row(
                      children: [
                        Expanded(child: Divider()),
                        Padding(
                          padding: EdgeInsets.symmetric(horizontal: 12),
                          child: Text(
                            'OR',
                            style: TextStyle(color: Colors.grey, fontSize: 12),
                          ),
                        ),
                        Expanded(child: Divider()),
                      ],
                    ),
                    const SizedBox(height: 12),
                    OutlinedButton.icon(
                      key: const Key('login-scan-qr'),
                      onPressed: () => context.push('/scan-qr'),
                      icon: const Icon(Icons.qr_code_scanner_rounded),
                      label: const Text('Scan Web Handoff QR'),
                    ),
                  ],
                ),
              ),
      ),
    ),
  );
}

class _UnverifiedPanel extends StatelessWidget {
  const _UnverifiedPanel({
    required this.email,
    required this.isBusy,
    required this.errorMessage,
    required this.resendSeconds,
    required this.onVerify,
    required this.onResend,
    super.key,
  });
  final String email;
  final bool isBusy;
  final String? errorMessage;
  final int resendSeconds;
  final VoidCallback onVerify;
  final VoidCallback onResend;

  @override
  Widget build(BuildContext context) => Column(
    key: const Key('unverified-actions'),
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      const Center(child: _VerificationStatusIcon()),
      const SizedBox(height: 20),
      Text(
        'Your email is not verified yet.',
        textAlign: TextAlign.center,
        style: Theme.of(context).textTheme.headlineSmall
            ?.copyWith(fontWeight: FontWeight.w900),
      ),
      const SizedBox(height: 8),
      Text(
        'Verify your email to continue.',
        textAlign: TextAlign.center,
        style: Theme.of(context).textTheme.bodyLarge
            ?.copyWith(color: SurplusLinkTheme.slate600),
      ),
      const SizedBox(height: 10),
      Text(
        email,
        textAlign: TextAlign.center,
        style: const TextStyle(
          color: SurplusLinkTheme.amberDark,
          fontWeight: FontWeight.w700,
        ),
      ),
      const SizedBox(height: 18),
      AuthErrorMessage(errorMessage),
      FilledButton(
        key: const Key('unverified-verify'),
        onPressed: isBusy ? null : onVerify,
        child: const Text('Verify Email'),
      ),
      const SizedBox(height: 10),
      OutlinedButton(
        key: const Key('unverified-resend'),
        onPressed: isBusy || resendSeconds > 0 ? null : onResend,
        child: isBusy
            ? const SizedBox.square(
                dimension: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : const Text('Resend Verification Code'),
      ),
      if (resendSeconds > 0)
        Padding(
          padding: const EdgeInsets.only(top: 10),
          child: Text(
            'Resend available in ${_countdown(resendSeconds)}',
            key: const Key('verification-countdown'),
            textAlign: TextAlign.center,
            style: const TextStyle(color: SurplusLinkTheme.slate600),
          ),
        ),
    ],
  );
}

class _VerificationStatusIcon extends StatelessWidget {
  const _VerificationStatusIcon();
  @override
  Widget build(BuildContext context) => TweenAnimationBuilder<double>(
    tween: Tween(begin: .75, end: 1),
    duration: const Duration(milliseconds: 420),
    curve: Curves.easeOutBack,
    builder: (_, scale, child) => Transform.scale(
      scale: scale,
      child: Opacity(opacity: scale.clamp(0, 1), child: child),
    ),
    child: Container(
      width: 72,
      height: 72,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: SurplusLinkTheme.amberSoft,
        border: Border.all(color: SurplusLinkTheme.amber),
      ),
      child: const Icon(
        Icons.mark_email_unread_rounded,
        color: SurplusLinkTheme.amberDark,
        size: 34,
      ),
    ),
  );
}

String _countdown(int seconds) =>
    '${(seconds ~/ 60).toString().padLeft(2, '0')}:${(seconds % 60).toString().padLeft(2, '0')}';

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

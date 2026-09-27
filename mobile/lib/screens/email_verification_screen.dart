import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/widgets/auth_error_message.dart';
import 'package:mobile/widgets/auth_scaffold.dart';

class EmailVerificationScreen extends StatefulWidget {
  const EmailVerificationScreen({
    required this.authController,
    required this.email,
    this.initialCooldown = 0,
    super.key,
  });
  final AuthController authController;
  final String email;
  final int initialCooldown;
  @override
  State<EmailVerificationScreen> createState() =>
      _EmailVerificationScreenState();
}

class _EmailVerificationScreenState extends State<EmailVerificationScreen> {
  final _code = TextEditingController();
  Timer? _timer;
  late int _seconds;
  bool _verified = false;
  bool get _isCodeValid => RegExp(r'^\d{6}$').hasMatch(_code.text);

  @override
  void initState() {
    super.initState();
    _seconds = widget.initialCooldown.clamp(0, 60);
    _code.addListener(_codeChanged);
    if (_seconds > 0) _startTimer();
  }

  void _codeChanged() => setState(() {});

  @override
  void dispose() {
    _code.removeListener(_codeChanged);
    _code.dispose();
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _verify() async {
    if (!_isCodeValid || widget.authController.isBusy) return;
    final verified = await widget.authController.verifyEmail(
      email: widget.email,
      code: _code.text,
    );
    if (!verified || !mounted) return;
    setState(() => _verified = true);
    await Future<void>.delayed(const Duration(milliseconds: 900));
    if (mounted) {
      context.go(
        '${AppRoutes.login}?email=${Uri.encodeQueryComponent(widget.email)}',
      );
    }
  }

  Future<void> _resend() async {
    if (_seconds > 0 || widget.authController.isBusy) return;
    final sent = await widget.authController.resendVerification(
      email: widget.email,
    );
    if (!sent || !mounted) return;
    setState(() => _seconds = 60);
    _startTimer();
  }

  void _startTimer() {
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) return timer.cancel();
      setState(() {
        if (_seconds <= 1) {
          _seconds = 0;
          timer.cancel();
        } else {
          _seconds--;
        }
      });
    });
  }

  @override
  Widget build(BuildContext context) => AuthScaffold(
    title: _verified ? 'Email verified successfully' : 'Verify your email',
    subtitle: _verified
        ? 'Returning you to Sign In...'
        : 'Enter the six-digit code sent to ${widget.email}.',
    child: AnimatedBuilder(
      animation: widget.authController,
      builder: (_, _) => AnimatedSwitcher(
        duration: const Duration(milliseconds: 300),
        child: _verified
            ? const _VerificationSuccess(key: ValueKey('verification-success'))
            : Column(
                key: const ValueKey('verification-form'),
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  AuthErrorMessage(widget.authController.errorMessage),
                  TextField(
                    key: const Key('verification-code'),
                    controller: _code,
                    maxLength: 6,
                    keyboardType: TextInputType.number,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.oneTimeCode],
                    inputFormatters: [
                      FilteringTextInputFormatter.digitsOnly,
                      LengthLimitingTextInputFormatter(6),
                    ],
                    decoration: const InputDecoration(
                      labelText: 'Verification code',
                      counterText: '',
                    ),
                    onSubmitted: (_) => _verify(),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
                    key: const Key('verify-email-submit'),
                    onPressed: widget.authController.isBusy || !_isCodeValid
                        ? null
                        : _verify,
                    child: widget.authController.isBusy
                        ? const SizedBox.square(
                            dimension: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Verify email'),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    key: const Key('verification-resend'),
                    onPressed: widget.authController.isBusy || _seconds > 0
                        ? null
                        : _resend,
                    child: Text(
                      _seconds > 0
                          ? 'Resend available in ${_countdown(_seconds)}'
                          : 'Resend Verification Code',
                    ),
                  ),
                  TextButton(
                    onPressed: widget.authController.isBusy
                        ? null
                        : () => context.go(
                            '${AppRoutes.login}?email=${Uri.encodeQueryComponent(widget.email)}',
                          ),
                    child: const Text('Back to sign in'),
                  ),
                ],
              ),
      ),
    ),
  );
}

class _VerificationSuccess extends StatelessWidget {
  const _VerificationSuccess({super.key});
  @override
  Widget build(BuildContext context) => Center(
    child: TweenAnimationBuilder<double>(
      tween: Tween(begin: .7, end: 1),
      duration: const Duration(milliseconds: 450),
      curve: Curves.easeOutBack,
      builder: (_, scale, child) => Transform.scale(scale: scale, child: child),
      child: Container(
        width: 82,
        height: 82,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: const Color(0xFFE8F7EE),
          border: Border.all(color: const Color(0xFF4B9B6D)),
        ),
        child: const Icon(
          Icons.check_rounded,
          color: Color(0xFF247A4A),
          size: 44,
        ),
      ),
    ),
  );
}

String _countdown(int seconds) =>
    '${(seconds ~/ 60).toString().padLeft(2, '0')}:${(seconds % 60).toString().padLeft(2, '0')}';

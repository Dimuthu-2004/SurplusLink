import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/widgets/auth_error_message.dart';
import 'package:mobile/widgets/auth_scaffold.dart';

class EmailVerificationScreen extends StatefulWidget {
  const EmailVerificationScreen({required this.authController, required this.email, super.key});
  final AuthController authController; final String email;
  @override State<EmailVerificationScreen> createState() => _EmailVerificationScreenState();
}
class _EmailVerificationScreenState extends State<EmailVerificationScreen> {
  final _code = TextEditingController(); int _seconds = 0; Timer? _timer;
  @override void dispose() { _code.dispose(); _timer?.cancel(); super.dispose(); }
  Future<void> _verify() async { if (RegExp(r'^\d{6}$').hasMatch(_code.text) && await widget.authController.verifyEmail(email: widget.email, code: _code.text) && mounted) context.go(AppRoutes.login); }
  Future<void> _resend() async { if (_seconds != 0) return; if (await widget.authController.resendVerification(email: widget.email) && mounted) { setState(() => _seconds = 60); _timer = Timer.periodic(const Duration(seconds: 1), (timer) { if (_seconds <= 1) { timer.cancel(); setState(() => _seconds = 0); } else { setState(() => _seconds--); } }); } }
  @override Widget build(BuildContext context) => AuthScaffold(title: 'Verify your email', subtitle: 'Enter the six-digit code sent to ${widget.email}.', child: AnimatedBuilder(animation: widget.authController, builder: (_, _) => Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [AuthErrorMessage(widget.authController.errorMessage), TextField(controller: _code, maxLength: 6, keyboardType: TextInputType.number, autofillHints: const [AutofillHints.oneTimeCode], decoration: const InputDecoration(labelText: 'Verification code')), const SizedBox(height: 16), FilledButton(onPressed: widget.authController.isBusy ? null : _verify, child: const Text('Verify email')), TextButton(onPressed: widget.authController.isBusy || _seconds > 0 ? null : _resend, child: Text(_seconds > 0 ? 'Resend code in 00:${_seconds.toString().padLeft(2, '0')}' : 'Resend code')), TextButton(onPressed: () => context.go(AppRoutes.login), child: const Text('Back to sign in'))])));
}

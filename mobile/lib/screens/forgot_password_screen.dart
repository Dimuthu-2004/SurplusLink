import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/widgets/auth_error_message.dart';
import 'package:mobile/widgets/auth_scaffold.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({required this.authController, super.key});
  final AuthController authController;
  @override State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}
class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _email = TextEditingController(), _code = TextEditingController(), _password = TextEditingController(), _confirm = TextEditingController();
  bool _sent = false; int _seconds = 0; Timer? _timer;
  @override void dispose() { _email.dispose(); _code.dispose(); _password.dispose(); _confirm.dispose(); _timer?.cancel(); super.dispose(); }
  void _startCooldown() { setState(() => _seconds = 60); _timer?.cancel(); _timer = Timer.periodic(const Duration(seconds: 1), (timer) { if (_seconds <= 1) { timer.cancel(); setState(() => _seconds = 0); } else { setState(() => _seconds--); } }); }
  Future<void> _submit() async {
    if (!_sent) { if (await widget.authController.forgotPassword(email: _email.text) && mounted) { setState(() => _sent = true); _startCooldown(); } return; }
    if (!RegExp(r'^\d{6}$').hasMatch(_code.text)) return;
    if (_password.text.length < 8 || _password.text != _confirm.text) return;
    if (await widget.authController.resetPassword(email: _email.text, code: _code.text, newPassword: _password.text) && mounted) context.go(AppRoutes.login);
  }
  Future<void> _resend() async { if (_seconds == 0 && await widget.authController.forgotPassword(email: _email.text) && mounted) _startCooldown(); }
  @override Widget build(BuildContext context) => AuthScaffold(title: _sent ? 'Choose a new password' : 'Reset your password', subtitle: _sent ? 'Enter the code from your email.' : 'We will send a reset code if an account exists.', child: AnimatedBuilder(animation: widget.authController, builder: (_, _) => Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [AuthErrorMessage(widget.authController.errorMessage), TextField(controller: _email, enabled: !_sent, keyboardType: TextInputType.emailAddress, decoration: const InputDecoration(labelText: 'Email')), if (_sent) ...[const SizedBox(height: 16), TextField(controller: _code, maxLength: 6, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Reset code')), const SizedBox(height: 16), TextField(controller: _password, obscureText: true, decoration: const InputDecoration(labelText: 'New password')), const SizedBox(height: 16), TextField(controller: _confirm, obscureText: true, decoration: const InputDecoration(labelText: 'Confirm new password'))], const SizedBox(height: 24), FilledButton(onPressed: widget.authController.isBusy ? null : _submit, child: Text(_sent ? 'Reset password' : 'Send reset code')), if (_sent) TextButton(onPressed: widget.authController.isBusy || _seconds > 0 ? null : _resend, child: Text(_seconds > 0 ? 'Resend code in 00:${_seconds.toString().padLeft(2, '0')}' : 'Resend code')), TextButton(onPressed: () => context.go(AppRoutes.login), child: const Text('Back to sign in'))])));
}

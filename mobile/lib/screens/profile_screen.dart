import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';
import 'package:mobile/widgets/profile_fields.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({required this.authController, super.key});
  final AuthController authController;
  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _form = GlobalKey<FormState>();
  late final _fields = ProfileFieldsController(widget.authController.user);
  @override
  void dispose() {
    _fields.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.authController,
    builder: (context, _) => Scaffold(
      appBar: AppBar(
        title: const Text('My profile'),
        leading: const DashboardBackButton(),
      ),
      body: Form(
        key: _form,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Text(widget.authController.user?.email ?? ''),
            const SizedBox(height: 20),
            ProfileFields(
              controller: _fields,
              enabled: !widget.authController.isBusy,
            ),
            if (widget.authController.errorMessage != null)
              Text(widget.authController.errorMessage!),
            FilledButton(
              onPressed: widget.authController.isBusy
                  ? null
                  : () async {
                      if (!_form.currentState!.validate()) return;
                      final saved = await widget.authController.updateProfile(
                        _fields.profile,
                      );
                      if (saved && context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Profile saved.')),
                        );
                      }
                    },
              child: Text(
                widget.authController.isBusy ? 'Saving...' : 'Save profile',
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

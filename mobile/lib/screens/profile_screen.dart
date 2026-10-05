import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';
import 'package:mobile/widgets/profile_fields.dart';
import 'package:mobile/widgets/role_navigation.dart';
import 'package:mobile/widgets/role_avatar.dart';
import 'package:image_picker/image_picker.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({required this.authController, super.key});
  final AuthController authController;
  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _form = GlobalKey<FormState>();
  late final _fields = ProfileFieldsController(widget.authController.user);
  final ImagePicker _picker = ImagePicker();

  Future<void> _pickPhoto(ImageSource source) async {
    try {
      final image = await _picker.pickImage(source: source, imageQuality: 85, maxWidth: 1600);
      if (image == null || !mounted) return;
      final bytes = await image.readAsBytes();
      if (!mounted) return;
      if (bytes.length > 5 * 1024 * 1024) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Choose a photo smaller than 5 MB.')));
        return;
      }
      final uploaded = await widget.authController.uploadProfilePhoto(bytes);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(uploaded ? 'Profile photo updated.' : widget.authController.errorMessage ?? 'Photo upload failed.')));
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Unable to open the photo picker. Check permission and try again.')));
    }
  }
  @override
  void dispose() {
    _fields.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.authController,
    builder: (context, _) {
      final user = widget.authController.user;
      if (user == null) return const Scaffold(body: SizedBox());
      return Scaffold(
      appBar: AppBar(
        title: const Text('My profile'),
        leading: const DashboardBackButton(),
      ),
      body: Form(
        key: _form,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Center(child: RoleAvatar(user: user, size: 88)),
            const SizedBox(height: 12),
            Center(child: Wrap(spacing: 8, children: [
              OutlinedButton.icon(onPressed: widget.authController.isBusy ? null : () => _pickPhoto(ImageSource.gallery), icon: const Icon(Icons.photo_library_outlined), label: const Text('Choose photo')),
              OutlinedButton.icon(onPressed: widget.authController.isBusy ? null : () => _pickPhoto(ImageSource.camera), icon: const Icon(Icons.camera_alt_outlined), label: const Text('Camera')),
            ])),
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
      bottomNavigationBar: RoleNavigation(user: user, current: '/profile'),
    );
    },
  );
}

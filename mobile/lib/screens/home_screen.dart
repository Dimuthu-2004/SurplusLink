import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({
    required this.authController,
    this.onOpenMaterials,
    super.key,
  });

  final AuthController authController;
  final VoidCallback? onOpenMaterials;

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: authController,
    builder: (context, child) {
      final user = authController.user;
      if (user == null) return const Scaffold(body: SizedBox.shrink());
      final (title, description, icon) = switch (user.role) {
        AppRole.seller => ('Seller home', 'Manage your material inventory and listings.', Icons.inventory_2_outlined),
        AppRole.buyer => ('Buyer home', 'Your buyer workspace is ready for future request features.', Icons.search),
        AppRole.manager => ('Manager home', 'Your manager workspace is ready for future administration features.', Icons.admin_panel_settings_outlined),
      };

      return Scaffold(
        appBar: AppBar(
          title: const Text('SurplusLink'),
          actions: [
            IconButton(
              key: const Key('logout-button'),
              tooltip: 'Sign out',
              onPressed: authController.isBusy ? null : authController.logout,
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon, size: 72),
                const SizedBox(height: 24),
                Text(title, key: const Key('role-home-title'), style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 12),
                Text(user.email),
                const SizedBox(height: 12),
                Text(description, textAlign: TextAlign.center),
                if (user.role == AppRole.seller && onOpenMaterials != null) ...[
                  const SizedBox(height: 24),
                  FilledButton.icon(
                    key: const Key('open-my-materials'),
                    onPressed: onOpenMaterials,
                    icon: const Icon(Icons.inventory_2_outlined),
                    label: const Text('My Materials'),
                  ),
                ],
              ],
            ),
          ),
        ),
      );
    },
  );
}
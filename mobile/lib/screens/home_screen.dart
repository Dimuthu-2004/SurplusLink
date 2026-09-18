import 'package:go_router/go_router.dart';
import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({
    required this.authController,
    this.onOpenMaterials,
    this.onOpenRequirements,
    super.key,
  });

  final AuthController authController;
  final VoidCallback? onOpenMaterials;
  final VoidCallback? onOpenRequirements;

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: authController,
    builder: (context, child) {
      final user = authController.user;
      if (user == null) return const Scaffold(body: SizedBox.shrink());
      final seller = user.hasRole(AppRole.seller);
      final buyer = user.hasRole(AppRole.buyer);
      final manager = user.hasRole(AppRole.manager);
      final title = manager
          ? 'Manager home'
          : seller && buyer
          ? 'Marketplace Home'
          : seller
          ? 'Seller home'
          : 'Buyer home';
      final description = manager
          ? 'Manage the marketplace from the web workspace.'
          : seller && buyer
          ? 'Sell surplus materials and find what your next project needs.'
          : seller
          ? 'Manage your material inventory and listings.'
          : 'Create requirements and follow matching progress.';
      final icon = manager
          ? Icons.admin_panel_settings_outlined
          : seller
          ? Icons.inventory_2_outlined
          : Icons.search;

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
        body: SingleChildScrollView(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon, size: 72),
                const SizedBox(height: 24),
                Text(
                  title,
                  key: const Key('role-home-title'),
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 12),
                Text(
                  user.fullName?.isNotEmpty == true
                      ? user.fullName!
                      : user.email,
                ),
                TextButton.icon(
                  onPressed: () => context.push('/profile'),
                  icon: const Icon(Icons.person_outline),
                  label: const Text('My profile'),
                ),
                const SizedBox(height: 12),
                Text(description, textAlign: TextAlign.center),
                if (user.hasRole(AppRole.buyer) &&
                    onOpenRequirements != null) ...[
                  const SizedBox(height: 24),
                  const Text('Buy'),
                  TextButton.icon(
                    onPressed: () => context.push('/requirements/new'),
                    icon: const Icon(Icons.add),
                    label: const Text('Create Requirement'),
                  ),
                  FilledButton.icon(
                    key: const Key('open-my-requirements'),
                    onPressed: onOpenRequirements,
                    icon: const Icon(Icons.assignment_outlined),
                    label: const Text('My Requirements'),
                  ),
                ],
                if (user.hasRole(AppRole.seller) &&
                    onOpenMaterials != null) ...[
                  const SizedBox(height: 24),
                  const Text('Sell'),
                  TextButton.icon(
                    onPressed: () => context.push('/materials/new'),
                    icon: const Icon(Icons.add),
                    label: const Text('Add Material'),
                  ),
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

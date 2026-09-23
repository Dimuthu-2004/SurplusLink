import 'package:go_router/go_router.dart';
import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/widgets/role_navigation.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({
    required this.authController,
    this.onOpenMaterials,
    this.onOpenRequirements,
    this.onOpenOffers,
    super.key,
  });

  final AuthController authController;
  final VoidCallback? onOpenMaterials;
  final VoidCallback? onOpenRequirements;
  final VoidCallback? onOpenOffers;

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
          title: const SurplusLinkLogo(size: 32),
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
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: [
                Card(child: Padding(padding: const EdgeInsets.all(22), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Container(padding: const EdgeInsets.all(12), decoration: BoxDecoration(color: const Color(0xFFFFF0E4), borderRadius: BorderRadius.circular(12)), child: Icon(icon, size: 30, color: const Color(0xFFC2410C))),
                  const SizedBox(height: 20),
                  Text(title, key: const Key('role-home-title'), style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w900)),
                  const SizedBox(height: 8),
                  Text(user.fullName?.isNotEmpty == true ? user.fullName! : user.email, style: Theme.of(context).textTheme.titleMedium?.copyWith(color: const Color(0xFF475569))),
                  const SizedBox(height: 10), Text(description, style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: const Color(0xFF475569))),
                  TextButton.icon(onPressed: () => context.push('/profile'), icon: const Icon(Icons.person_outline), label: const Text('My profile')),
                ]))),
                if (user.hasRole(AppRole.buyer) &&
                    onOpenRequirements != null) ...[
                  const SizedBox(height: 24),
                  Text('BUY', style: Theme.of(context).textTheme.labelSmall?.copyWith(letterSpacing: 1.1, fontWeight: FontWeight.w900, color: const Color(0xFFC2410C))),
                  const SizedBox(height: 8),
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
                if ((seller || buyer) && onOpenOffers != null) ...[
                  const SizedBox(height: 12),
                  FilledButton.icon(onPressed: onOpenOffers, icon: const Icon(Icons.local_offer_outlined), label: const Text('My Offers')),
                ],
                if (user.hasRole(AppRole.seller) &&
                    onOpenMaterials != null) ...[
                  const SizedBox(height: 24),
                  Text('SELL', style: Theme.of(context).textTheme.labelSmall?.copyWith(letterSpacing: 1.1, fontWeight: FontWeight.w900, color: const Color(0xFFC2410C))),
                  const SizedBox(height: 8),
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
        bottomNavigationBar: RoleNavigation(user: user, current: '/home'),
      );
    },
  );
}

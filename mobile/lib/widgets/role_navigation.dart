import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/marketplace/marketplace_mode_controller.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_models.dart';

class RoleNavigation extends StatelessWidget {
  const RoleNavigation({
    required this.user,
    required this.current,
    this.mode,
    super.key,
  });
  final AppUser user;
  final MarketplaceMode? mode;
  final String current;

  @override
  Widget build(BuildContext context) {
    final activeMode =
        mode ??
        MarketplaceModeScope.maybeOf(context)?.activeMode ??
        availableMode(user);
    final destinations = <({String path, String label, IconData icon})>[
      (path: '/home', label: 'Home', icon: Icons.home_outlined),
      if (activeMode == MarketplaceMode.buyer)
        (
          path: '/requirements',
          label: 'Needs',
          icon: Icons.assignment_outlined,
        ),
      if (activeMode == MarketplaceMode.seller)
        (
          path: '/materials',
          label: 'Listings',
          icon: Icons.inventory_2_outlined,
        ),
      if (user.hasRole(AppRole.buyer) || user.hasRole(AppRole.seller))
        (path: '/offers', label: 'Offers', icon: Icons.local_offer_outlined),
      (path: '/profile', label: 'Profile', icon: Icons.person_outline),
    ];
    final selected = destinations
        .indexWhere((item) => item.path == current)
        .clamp(0, destinations.length - 1);
    return NavigationBar(
      selectedIndex: selected,
      onDestinationSelected: (index) => context.go(destinations[index].path),
      destinations: [
        for (final item in destinations)
          NavigationDestination(icon: Icon(item.icon), label: item.label),
      ],
    );
  }
}

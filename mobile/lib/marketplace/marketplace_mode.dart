import 'package:mobile/auth/auth_models.dart';

/// Presentation preference only. Never replaces the authenticated roles.
enum MarketplaceMode { buyer, seller }

MarketplaceMode? availableMode(AppUser? user, [MarketplaceMode? preferred]) {
  if (user == null) return null;
  if (preferred == MarketplaceMode.seller && user.hasRole(AppRole.seller)) {
    return MarketplaceMode.seller;
  }
  if (user.hasRole(AppRole.buyer)) return MarketplaceMode.buyer;
  if (user.hasRole(AppRole.seller)) return MarketplaceMode.seller;
  return null;
}

bool isDualMarketplaceUser(AppUser? user) =>
    user?.hasRole(AppRole.buyer) == true &&
    user?.hasRole(AppRole.seller) == true;

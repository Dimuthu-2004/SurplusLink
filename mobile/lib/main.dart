import 'package:mobile/marketplace/marketplace_mode_controller.dart';
import 'package:mobile/marketplace/marketplace_mode_storage.dart';
import 'package:flutter/material.dart';
import 'package:mobile/matches/match_repository.dart';
import 'package:http/http.dart' as http;
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_repository.dart';
import 'package:mobile/config/app_config.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/token_storage.dart';
import 'package:mobile/materials/material_inventory_repository.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/requirements/requirement_repository.dart';
import 'package:mobile/offers/offer_repository.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final marketplace = MarketplaceModeController(
    storage: const SecureMarketplaceModeStorage(),
  );
  final tokenStorage = SecureTokenStorage();
  final apiClient = ApiClient(
    mutations: marketplace.mutations,
    baseUri: AppConfig.apiBaseUri,
    httpClient: http.Client(),
    tokenStorage: tokenStorage,
  );
  final authController = AuthController(
    AuthRepository(apiClient: apiClient, tokenStorage: tokenStorage),
    marketplace: marketplace,
  );

  runApp(
    SurplusLinkApp(
      authController: authController,
      materialGateway: MaterialInventoryRepository(apiClient),
      matchGateway: MatchRepository(
        apiClient,
        onSessionExpired: authController.logout,
      ),
      requirementGateway: RequirementRepository(
        apiClient,
        onSessionExpired: authController.logout,
      ),
      offerGateway: OfferRepository(apiClient),
      locationLookup: ApiLocationLookup(apiClient).lookup,
      addressSearch: ApiLocationLookup(apiClient).search,
    ),
  );
}

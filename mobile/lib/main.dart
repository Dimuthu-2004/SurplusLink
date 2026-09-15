import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_repository.dart';
import 'package:mobile/config/app_config.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/core/token_storage.dart';
import 'package:mobile/materials/material_inventory_repository.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final tokenStorage = SecureTokenStorage();
  final apiClient = ApiClient(
    baseUri: AppConfig.apiBaseUri,
    httpClient: http.Client(),
    tokenStorage: tokenStorage,
  );
  final authController = AuthController(
    AuthRepository(apiClient: apiClient, tokenStorage: tokenStorage),
  );

  runApp(
    SurplusLinkApp(
      authController: authController,
      materialGateway: MaterialInventoryRepository(apiClient),
    ),
  );
}
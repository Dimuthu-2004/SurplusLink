import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/screens/add_material_screen.dart';
import 'package:mobile/screens/edit_material_screen.dart';
import 'package:mobile/screens/home_screen.dart';
import 'package:mobile/screens/login_screen.dart';
import 'package:mobile/screens/material_details_screen.dart';
import 'package:mobile/screens/my_materials_screen.dart';
import 'package:mobile/screens/register_screen.dart';
import 'package:mobile/screens/splash_screen.dart';

abstract final class AppRoutes {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const home = '/home';
  static const materials = '/materials';
  static const addMaterial = '/materials/new';
}

GoRouter createAppRouter({
  required AuthController authController,
  MaterialInventoryGateway? materialGateway,
  String initialLocation = AppRoutes.splash,
}) {
  return GoRouter(
    initialLocation: initialLocation,
    refreshListenable: authController,
    redirect: (context, state) {
      final location = state.matchedLocation;
      final isAuthRoute =
          location == AppRoutes.login || location == AppRoutes.register;

      if (authController.status == AuthStatus.initializing) {
        return location == AppRoutes.splash ? null : AppRoutes.splash;
      }
      if (!authController.isAuthenticated) {
        return isAuthRoute ? null : AppRoutes.login;
      }
      if (isAuthRoute || location == AppRoutes.splash) {
        return AppRoutes.home;
      }
      return null;
    },
    routes: [
      GoRoute(
        path: AppRoutes.splash,
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) =>
            LoginScreen(authController: authController),
      ),
      GoRoute(
        path: AppRoutes.register,
        builder: (context, state) =>
            RegisterScreen(authController: authController),
      ),
      GoRoute(
        path: AppRoutes.home,
        builder: (context, state) => HomeScreen(
          authController: authController,
          onOpenMaterials: materialGateway == null
              ? null
              : () => context.go(AppRoutes.materials),
        ),
      ),
      if (materialGateway != null) ...[
        GoRoute(
          path: AppRoutes.materials,
          builder: (context, state) => _materialsScreen(authController, materialGateway),
        ),
        GoRoute(
          path: AppRoutes.addMaterial,
          builder: (context, state) => _sellerOnly(
            authController,
            AddMaterialScreen(gateway: materialGateway),
          ),
        ),
        GoRoute(
          path: '${AppRoutes.materials}/:id/edit',
          builder: (context, state) => _sellerOnly(
            authController,
            EditMaterialScreen(
              gateway: materialGateway,
              listingId: state.pathParameters['id']!,
            ),
          ),
        ),
        GoRoute(
          path: '${AppRoutes.materials}/:id',
          builder: (context, state) => _detailsScreen(
            authController,
            materialGateway,
            state.pathParameters['id']!,
          ),
        ),
      ],
    ],
    errorBuilder: (context, state) => Scaffold(
      body: Center(child: Text('Page not found: ${state.uri.path}')),
    ),
  );
}

Widget _materialsScreen(
  AuthController authController,
  MaterialInventoryGateway gateway,
) {
  final user = authController.user;
  return user == null
      ? const SizedBox.shrink()
      : MyMaterialsScreen(gateway: gateway, user: user);
}

Widget _detailsScreen(
  AuthController authController,
  MaterialInventoryGateway gateway,
  String listingId,
) {
  final user = authController.user;
  return user == null
      ? const SizedBox.shrink()
      : MaterialDetailsScreen(gateway: gateway, user: user, listingId: listingId);
}
Widget _sellerOnly(AuthController authController, Widget child) {
  final user = authController.user;
  if (user?.role == AppRole.seller) return child;
  return const Scaffold(
    body: Center(child: Text('Only seller accounts can manage material listings.')),
  );
}
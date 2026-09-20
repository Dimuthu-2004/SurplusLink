import 'package:mobile/screens/profile_screen.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/screens/recommended_matches_screen.dart';
import 'package:mobile/screens/match_details_screen.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/screens/my_requirements_screen.dart';
import 'package:mobile/screens/requirement_form_screen.dart';
import 'package:mobile/screens/requirement_details_screen.dart';
import 'package:mobile/screens/requirement_status_screen.dart';
import 'package:mobile/screens/requirement_history_screen.dart';
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
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/screens/my_offers_screen.dart';
import 'package:mobile/screens/register_screen.dart';
import 'package:mobile/screens/splash_screen.dart';

abstract final class AppRoutes {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const home = '/home';
  static const requirements = '/requirements';
  static const materials = '/materials';
  static const addMaterial = '/materials/new';
}

GoRouter createAppRouter({
  required AuthController authController,
  MaterialInventoryGateway? materialGateway,
  RequirementGateway? requirementGateway,
  MatchGateway? matchGateway,
  OfferGateway? offerGateway,
  RequirementLocationSource requirementLocation =
      const DeviceRequirementLocation(),
  AddressLookup? locationLookup,
  String initialLocation = AppRoutes.splash,
}) {
  String? pendingLocation;
  return GoRouter(
    initialLocation: initialLocation,
    refreshListenable: authController,
    redirect: (context, state) {
      final location = state.matchedLocation;
      final isAuthRoute =
          location == AppRoutes.login || location == AppRoutes.register;

      if (authController.status == AuthStatus.initializing) {
        if (!isAuthRoute && location != AppRoutes.splash) {
          pendingLocation = state.uri.toString();
        }
        return location == AppRoutes.splash ? null : AppRoutes.splash;
      }
      if (!authController.isAuthenticated) {
        return isAuthRoute ? null : AppRoutes.login;
      }
      if (isAuthRoute || location == AppRoutes.splash) {
        final destination = pendingLocation ?? AppRoutes.home;
        pendingLocation = null;
        return destination;
      }
      if ((location == AppRoutes.requirements ||
              location.startsWith('/requirements/')) &&
          authController.user?.hasRole(AppRole.buyer) != true) {
        return AppRoutes.home;
      }
      if ((location == AppRoutes.materials ||
              location.startsWith('${AppRoutes.materials}/')) &&
          authController.user?.hasRole(AppRole.seller) != true) {
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
        path: '/profile',
        builder: (_, _) => ProfileScreen(authController: authController),
      ),
      GoRoute(
        path: AppRoutes.home,
        builder: (context, state) => HomeScreen(
          authController: authController,
          onOpenRequirements: requirementGateway == null
              ? null
              : () => context.push(AppRoutes.requirements),
          onOpenMaterials: materialGateway == null
              ? null
              : () => context.push(AppRoutes.materials),
              onOpenOffers: offerGateway == null ? null : () => context.push('/offers'),
        ),
      ),
      if (matchGateway != null) ...[
        GoRoute(
          path: '/requirements/:id/matches',
          builder: (_, state) => RecommendedMatchesScreen(
            key: ValueKey(state.pathParameters['id']),
            gateway: matchGateway,
            requirementId: state.pathParameters['id']!,
          ),
        ),
        GoRoute(
          path: '/requirements/:id/matches/:matchId',
          builder: (_, state) => MatchDetailsScreen(
            key: ValueKey(
              '${state.pathParameters['id']}/${state.pathParameters['matchId']}',
            ),
            gateway: matchGateway,
            requirementId: state.pathParameters['id']!,
            matchId: state.pathParameters['matchId']!,
          ),
        ),
      ],
      if (requirementGateway != null) ...[
        GoRoute(
          path: AppRoutes.requirements,
          builder: (context, state) =>
              MyRequirementsScreen(gateway: requirementGateway),
        ),
        GoRoute(
          path: '/requirements/new',
          builder: (context, state) => RequirementFormScreen(
            gateway: requirementGateway,
            locationSource: requirementLocation,
            locationLookup: locationLookup,
          ),
        ),
        GoRoute(
          path: '/requirements/:id/edit',
          builder: (context, state) => RequirementFormScreen(
            gateway: requirementGateway,
            requirementId: state.pathParameters['id']!,
            locationSource: requirementLocation,
            locationLookup: locationLookup,
          ),
        ),
        GoRoute(
          path: '/requirements/:id/history',
          builder: (context, state) => RequirementHistoryScreen(
            gateway: requirementGateway,
            requirementId: state.pathParameters['id']!,
          ),
        ),
        GoRoute(
          path: '/requirements/:id/status',
          builder: (context, state) => RequirementStatusScreen(
            gateway: requirementGateway,
            showMatches: matchGateway != null,
            requirementId: state.pathParameters['id']!,
            workflowId: state.extra as String?,
          ),
        ),
        GoRoute(
          path: '/requirements/:id',
          builder: (context, state) => RequirementDetailsScreen(
            gateway: requirementGateway,
            showMatches: matchGateway != null,
            requirementId: state.pathParameters['id']!,
            locationLookup: locationLookup,
          ),
        ),
      ],
      if (materialGateway != null) ...[
        GoRoute(
          path: AppRoutes.materials,
          builder: (context, state) =>
              _materialsScreen(authController, materialGateway),
        ),
        GoRoute(
          path: AppRoutes.addMaterial,
          builder: (context, state) => _sellerOnly(
            authController,
            AddMaterialScreen(
              gateway: materialGateway,
              locationLookup: locationLookup,
            ),
          ),
        ),
        GoRoute(
          path: '${AppRoutes.materials}/:id/edit',
          builder: (context, state) => _sellerOnly(
            authController,
            EditMaterialScreen(
              gateway: materialGateway,
              listingId: state.pathParameters['id']!,
              locationLookup: locationLookup,
            ),
          ),
        ),
        GoRoute(
          path: '${AppRoutes.materials}/:id',
          builder: (context, state) => _detailsScreen(
            authController,
            materialGateway,
            state.pathParameters['id']!,
            locationLookup,
          ),
        ),
      ],
      if (offerGateway != null)
        GoRoute(path: '/offers', builder: (_, _) {
          final user = authController.user;
          return user == null ? const SizedBox.shrink() : MyOffersScreen(gateway: offerGateway, user: user);
        }),
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
  AddressLookup? locationLookup,
) {
  final user = authController.user;
  return user == null
      ? const SizedBox.shrink()
      : MaterialDetailsScreen(
          gateway: gateway,
          user: user,
          listingId: listingId,
          locationLookup: locationLookup,
        );
}

Widget _sellerOnly(AuthController authController, Widget child) {
  final user = authController.user;
  if (user?.hasRole(AppRole.seller) == true) return child;
  return const Scaffold(
    body: Center(
      child: Text('Only seller accounts can manage material listings.'),
    ),
  );
}

import 'dart:async';

import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/offers/offer_gateway.dart';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/location/location_lookup.dart';

class SurplusLinkApp extends StatefulWidget {
  const SurplusLinkApp({
    required this.authController,
    this.materialGateway,
    this.requirementGateway,
    this.matchGateway,
    this.offerGateway,
    this.requirementLocation = const DeviceRequirementLocation(),
    this.locationLookup,
    this.initialLocation = AppRoutes.splash,
    super.key,
  });

  final AuthController authController;
  final MaterialInventoryGateway? materialGateway;
  final RequirementGateway? requirementGateway;
  final MatchGateway? matchGateway;
  final OfferGateway? offerGateway;
  final RequirementLocationSource requirementLocation;
  final AddressLookup? locationLookup;
  final String initialLocation;

  @override
  State<SurplusLinkApp> createState() => _SurplusLinkAppState();
}

class _SurplusLinkAppState extends State<SurplusLinkApp> {
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    _router = createAppRouter(
      authController: widget.authController,
      materialGateway: widget.materialGateway,
      requirementGateway: widget.requirementGateway,
      matchGateway: widget.matchGateway,
      offerGateway: widget.offerGateway,
      requirementLocation: widget.requirementLocation,
      locationLookup: widget.locationLookup,
      initialLocation: widget.initialLocation,
    );
    unawaited(widget.authController.initialize());
  }

  @override
  void dispose() {
    _router.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => MaterialApp.router(
    title: 'SurplusLink',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF16794B)),
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
      ),
      useMaterial3: true,
    ),
    routerConfig: _router,
  );
}

import 'dart:async';

import 'package:mobile/widgets/startup_transition.dart';

import 'package:mobile/marketplace/marketplace_mode_controller.dart';

import 'package:mobile/handoff/mobile_handoff_gateway.dart';
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
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/l10n/app_localizations.dart';
import 'package:mobile/l10n/locale_controller.dart';

class SurplusLinkApp extends StatefulWidget {
  const SurplusLinkApp({
    required this.authController,
    this.materialGateway,
    this.requirementGateway,
    this.matchGateway,
    this.offerGateway,
    this.handoffGateway,
    this.requirementLocation = const DeviceRequirementLocation(),
    this.locationLookup,
    this.addressSearch,
    this.initialLocation = AppRoutes.splash,
    this.localeController,
    super.key,
  });

  final AuthController authController;
  final MaterialInventoryGateway? materialGateway;
  final RequirementGateway? requirementGateway;
  final MatchGateway? matchGateway;
  final OfferGateway? offerGateway;
  final MobileHandoffGateway? handoffGateway;
  final RequirementLocationSource requirementLocation;
  final AddressLookup? locationLookup;
  final AddressSearch? addressSearch;
  final String initialLocation;
  final LocaleController? localeController;

  @override
  State<SurplusLinkApp> createState() => _SurplusLinkAppState();
}

class _SurplusLinkAppState extends State<SurplusLinkApp> {
  late final GoRouter _router;
  late final LocaleController _localeController;
  late int _modeRevision;
  late String _routePath;

  @override
  void initState() {
    super.initState();
    _localeController = widget.localeController ?? LocaleController();
    if (widget.localeController == null) unawaited(_localeController.load());
    _router = createAppRouter(
      authController: widget.authController,
      materialGateway: widget.materialGateway,
      requirementGateway: widget.requirementGateway,
      matchGateway: widget.matchGateway,
      offerGateway: widget.offerGateway,
      handoffGateway: widget.handoffGateway,
      requirementLocation: widget.requirementLocation,
      locationLookup: widget.locationLookup,
      addressSearch: widget.addressSearch,
      initialLocation: widget.initialLocation,
    );
    _routePath = _router.routeInformationProvider.value.uri.path;
    _router.routeInformationProvider.addListener(_routeChanged);
    _modeRevision = widget.authController.marketplace.revision;
    widget.authController.marketplace.addListener(_modeChanged);
    unawaited(widget.authController.initialize());
  }

  void _modeChanged() {
    final revision = widget.authController.marketplace.revision;
    if (_modeRevision == revision) return;
    _modeRevision = revision;
    // Replace the whole stack, including pageless transaction/history pages.
    if (widget.authController.isAuthenticated) _router.go(AppRoutes.home);
  }

  void _routeChanged() {
    final next = _router.routeInformationProvider.value.uri.path;
    if (next != _routePath && mounted) setState(() => _routePath = next);
  }

  bool get _showLanguageSelector => {
    AppRoutes.login,
    AppRoutes.register,
    AppRoutes.verifyEmail,
    AppRoutes.forgotPassword,
    AppRoutes.home,
    '/profile',
  }.contains(_routePath);

  @override
  void dispose() {
    widget.authController.marketplace.removeListener(_modeChanged);
    _router.routeInformationProvider.removeListener(_routeChanged);
    _router.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: _localeController,
    builder: (context, _) => MaterialApp.router(
    title: 'SurplusLink',
    locale: _localeController.locale,
    supportedLocales: AppLocalizations.supportedLocales,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    debugShowCheckedModeBanner: false,
    theme: SurplusLinkTheme.light,
    routerConfig: _router,
    builder: (context, child) => MarketplaceModeScope(
      controller: widget.authController.marketplace,
      child: Stack(children: [
        ListenableBuilder(
          listenable: widget.authController,
          builder: (context, _) => StartupTransition(
            initializing: widget.authController.status == AuthStatus.initializing,
            child: child!,
          ),
        ),
        if (_showLanguageSelector)
          Positioned(top: 2, left: 2, child: SafeArea(child: LanguageSelector(controller: _localeController))),
      ]),
    ),
  ));
}

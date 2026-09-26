import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/widgets/marketplace_mode_switcher.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/home/home_dashboard_controller.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/role_navigation.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({
    required this.authController,
    this.requirementGateway,
    this.materialGateway,
    this.offerGateway,
    this.onOpenMaterials,
    this.onOpenRequirements,
    this.onOpenOffers,
    this.onCreateMaterial,
    this.onCreateRequirement,
    super.key,
  });
  final AuthController authController;
  final RequirementGateway? requirementGateway;
  final MaterialInventoryGateway? materialGateway;
  final OfferGateway? offerGateway;
  final VoidCallback? onOpenMaterials,
      onOpenRequirements,
      onOpenOffers,
      onCreateMaterial,
      onCreateRequirement;
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> with TickerProviderStateMixin {
  HomeDashboardController? _dashboard;
  late final AnimationController _entrance = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 650),
  )..forward();
  late final AnimationController _glow = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1400),
  )..forward();
  @override
  void dispose() {
    _dashboard?.dispose();
    _entrance.dispose();
    _glow.dispose();
    super.dispose();
  }

  HomeDashboardController _forUser(AppUser user) {
    final mode = widget.authController.marketplace.activeMode;
    if (_dashboard != null &&
        (_dashboard!.user.id != user.id || _dashboard!.mode != mode)) {
      _dashboard!.dispose();
      _dashboard = null;
    }
    return _dashboard ??= HomeDashboardController(
      user: user,
      mode: mode,
      requirements: widget.requirementGateway,
      materials: widget.materialGateway,
      offers: widget.offerGateway,
    )..load();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.authController,
    builder: (_, _) {
      final user = widget.authController.user;
      if (user == null) return const Scaffold(body: SizedBox());
      final dashboard = _forUser(user);
      final mode = dashboard.mode;
      return AnimatedBuilder(
        animation: dashboard,
        builder: (_, _) => Scaffold(
          body: AnimatedSwitcher(
            duration: const Duration(milliseconds: 220),
            // Only the selected experience is interactive during the fade.
            layoutBuilder: (child, _) => child ?? const SizedBox.shrink(),
            transitionBuilder: (child, animation) => FadeTransition(
              opacity: animation,
              child: SlideTransition(
                position: Tween<Offset>(
                  begin: Offset(
                    mode == MarketplaceMode.seller ? .025 : -.025,
                    0,
                  ),
                  end: Offset.zero,
                ).animate(animation),
                child: child,
              ),
            ),
            child: SafeArea(
              key: ValueKey(mode),
              child: RefreshIndicator(
                onRefresh: dashboard.load,
                child: ListView(
                  key: const Key('home-dashboard-scroll'),
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 112),
                  children: [
                    _Entrance(
                      index: 0,
                      controller: _entrance,
                      child: _Hero(
                        user: user,
                        mode: mode,
                        switcher: MarketplaceModeSwitcher(
                          controller: widget.authController.marketplace,
                        ),
                        glow: _glow,
                        onProfile: () => context.push('/profile'),
                        onLogout: widget.authController.logout,
                      ),
                    ),
                    const SizedBox(height: 20),
                    _Entrance(
                      index: 1,
                      controller: _entrance,
                      child: _Overview(
                        mode: mode,
                        data: dashboard.data,
                        loading: dashboard.isLoading,
                        buyerTap: widget.onOpenRequirements,
                        sellerTap: widget.onOpenMaterials,
                        offersTap: widget.onOpenOffers,
                      ),
                    ),
                    const SizedBox(height: 24),
                    _Entrance(
                      index: 2,
                      controller: _entrance,
                      child: _QuickActions(
                        mode: mode,
                        createMaterial: widget.onCreateMaterial,
                        createRequirement: widget.onCreateRequirement,
                        materials: widget.onOpenMaterials,
                        requirements: widget.onOpenRequirements,
                        offers: widget.onOpenOffers,
                        highlight: (dashboard.data.buyer?.pending ?? 0) > 0,
                      ),
                    ),
                    const SizedBox(height: 24),
                    _Entrance(
                      index: 3,
                      controller: _entrance,
                      child: _Activity(
                        rows: dashboard.data.activities,
                        loading: dashboard.isLoading,
                        error: dashboard.error,
                        onRetry: dashboard.load,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          bottomNavigationBar: RoleNavigation(
            user: user,
            mode: mode,
            current: '/home',
          ),
        ),
      );
    },
  );
}

class _Hero extends StatelessWidget {
  const _Hero({
    required this.user,
    required this.mode,
    required this.switcher,
    required this.glow,
    required this.onProfile,
    required this.onLogout,
  });
  final AppUser user;
  final MarketplaceMode? mode;
  final Widget switcher;
  final Animation<double> glow;
  final VoidCallback onProfile;
  final VoidCallback onLogout;
  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: glow,
    builder: (_, _) => Container(
      key: const Key('home-hero'),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        gradient: const LinearGradient(
          colors: [SurplusLinkTheme.slate900, SurplusLinkTheme.slate800],
        ),
        boxShadow: [
          BoxShadow(
            color: SurplusLinkTheme.slate900.withValues(alpha: .18),
            blurRadius: 22,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Stack(
        children: [
          Positioned(
            right: -22 + glow.value * 13,
            top: -30,
            child: Container(
              width: 124,
              height: 124,
              decoration: BoxDecoration(
                color: SurplusLinkTheme.amber.withValues(alpha: .2),
                shape: BoxShape.circle,
              ),
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Expanded(
                    child: FittedBox(
                      fit: BoxFit.scaleDown,
                      alignment: Alignment.centerLeft,
                      child: SurplusLinkLogo(
                        size: 30,
                        foregroundColor: Colors.white,
                      ),
                    ),
                  ),
                  Material(
                    color: Colors.white.withValues(alpha: .13),
                    borderRadius: BorderRadius.circular(14),
                    child: InkWell(
                      key: const Key('home-profile-button'),
                      onTap: onProfile,
                      borderRadius: BorderRadius.circular(14),
                      child: SizedBox(
                        width: 46,
                        height: 46,
                        child: Center(
                          child: Text(
                            _initial(user),
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),
              Text(
                'Good to see you,',
                style: Theme.of(context).textTheme.titleMedium
                    ?.copyWith(color: Colors.white70),
              ),
              const SizedBox(height: 3),
              Text(
                _name(user),
                key: const Key('home-user-name'),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  color: Colors.white,
                  fontWeight: FontWeight.w900,
                ),
              ),
              IconButton(
                key: const Key('logout-button'),
                tooltip: 'Sign out',
                onPressed: onLogout,
                color: Colors.white,
                icon: const Icon(Icons.logout),
              ),
              const SizedBox(height: 13),
              if (isDualMarketplaceUser(user))
                switcher
              else
                _RolePill(mode: mode),
            ],
          ),
        ],
      ),
    ),
  );
  static String _name(AppUser u) => u.fullName?.trim().isNotEmpty == true
      ? u.fullName!.trim()
      : u.email.split('@').first;
  static String _initial(AppUser u) =>
      _name(u).isEmpty ? '?' : _name(u)[0].toUpperCase();
}

class _RolePill extends StatelessWidget {
  const _RolePill({required this.mode});
  final MarketplaceMode? mode;
  @override
  Widget build(BuildContext context) {
    final label = mode == MarketplaceMode.seller
        ? 'Seller'
        : mode == MarketplaceMode.buyer
        ? 'Buyer'
        : 'Manager';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 7),
      decoration: BoxDecoration(
        color: SurplusLinkTheme.amber.withValues(alpha: .18),
        borderRadius: BorderRadius.circular(99),
        border: Border.all(
          color: SurplusLinkTheme.amber.withValues(alpha: .45),
        ),
      ),
      child: Text(
        label,
        key: const Key('home-role-label'),
        style: const TextStyle(
          color: Colors.white,
          fontWeight: FontWeight.w800,
          fontSize: 12,
        ),
      ),
    );
  }
}

class _Overview extends StatelessWidget {
  const _Overview({
    required this.mode,
    required this.data,
    required this.loading,
    this.buyerTap,
    this.sellerTap,
    this.offersTap,
  });
  final MarketplaceMode? mode;
  final HomeDashboardData data;
  final bool loading;
  final VoidCallback? buyerTap, sellerTap, offersTap;
  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _Title('Overview'),
        if (mode == MarketplaceMode.buyer) ...[
          _Grid(
            loading: loading,
            cards: [
              _Stat(
                'Active Requirements',
                data.buyer?.active,
                Icons.assignment_outlined,
                buyerTap,
              ),
              _Stat(
                'Matches Ready',
                data.buyer?.matches,
                Icons.auto_awesome_outlined,
                buyerTap,
              ),
              _Stat(
                'Pending Approval',
                data.buyer?.pending,
                Icons.hourglass_top_outlined,
                offersTap ?? buyerTap,
                highlight: (data.buyer?.pending ?? 0) > 0,
              ),
            ],
          ),
        ],
        if (mode == MarketplaceMode.seller) ...[
          _Grid(
            loading: loading,
            cards: [
              _Stat(
                'Active Listings',
                data.seller?.active,
                Icons.inventory_2_outlined,
                sellerTap,
              ),
              _Stat(
                'Reserved Listings',
                data.seller?.reserved,
                Icons.bookmark_added_outlined,
                sellerTap,
              ),
            ],
          ),
        ],
      ],
    );
  }
}

class _Stat {
  const _Stat(
    this.label,
    this.value,
    this.icon,
    this.onTap, {
    this.highlight = false,
  });
  final String label;
  final int? value;
  final IconData icon;
  final VoidCallback? onTap;
  final bool highlight;
}

class _Grid extends StatelessWidget {
  const _Grid({required this.cards, required this.loading});
  final List<_Stat> cards;
  final bool loading;
  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (_, c) {
      final wide = c.maxWidth >= 420;
      return Wrap(
        spacing: 12,
        runSpacing: 12,
        children: [
          for (final stat in cards)
            SizedBox(
              width: wide ? (c.maxWidth - 12) / 2 : c.maxWidth,
              child: _StatCard(stat: stat, loading: loading),
            ),
        ],
      );
    },
  );
}

class _StatCard extends StatefulWidget {
  const _StatCard({required this.stat, required this.loading});
  final _Stat stat;
  final bool loading;
  @override
  State<_StatCard> createState() => _StatCardState();
}

class _StatCardState extends State<_StatCard> {
  bool pressed = false;

  @override
  Widget build(BuildContext context) => AnimatedScale(
    scale: pressed ? .98 : 1,
    duration: const Duration(milliseconds: 110),
    child: Card(
      child: InkWell(
        onTap: widget.stat.onTap,
        onHighlightChanged: (v) => setState(() => pressed = v),
        borderRadius: BorderRadius.circular(16),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 220),
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: widget.stat.highlight
                ? Border.all(color: SurplusLinkTheme.amber, width: 1.5)
                : null,
          ),
          child: widget.loading && widget.stat.value == null
              ? const _Skeleton(58)
              : Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(9),
                      decoration: BoxDecoration(
                        color: SurplusLinkTheme.amberSoft,
                        borderRadius: BorderRadius.circular(11),
                      ),
                      child: Icon(
                        widget.stat.icon,
                        color: SurplusLinkTheme.amberDark,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _Count(widget.stat.value ?? 0),
                          const SizedBox(height: 2),
                          Text(
                            widget.stat.label,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: Theme.of(context).textTheme.labelMedium
                                ?.copyWith(
                                  color: SurplusLinkTheme.slate600,
                                  fontWeight: FontWeight.w700,
                                ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
        ),
      ),
    ),
  );
}

class _Count extends StatelessWidget {
  const _Count(this.value);
  final int value;
  @override
  Widget build(BuildContext context) => TweenAnimationBuilder<int>(
    tween: IntTween(begin: 0, end: value),
    duration: const Duration(milliseconds: 650),
    curve: Curves.easeOutCubic,
    builder: (_, value, _) => Text(
      '$value',
      style: Theme.of(context).textTheme.headlineSmall?.copyWith(
        fontWeight: FontWeight.w900,
        color: SurplusLinkTheme.slate900,
      ),
    ),
  );
}

class _QuickActions extends StatelessWidget {
  const _QuickActions({
    required this.mode,
    this.createMaterial,
    this.createRequirement,
    this.materials,
    this.requirements,
    this.offers,
    required this.highlight,
  });
  final MarketplaceMode? mode;
  final VoidCallback? createMaterial,
      createRequirement,
      materials,
      requirements,
      offers;
  final bool highlight;
  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const _Title('Quick actions'),
      const SizedBox(height: 10),
      Wrap(
        spacing: 10,
        runSpacing: 10,
        children: [
          if (mode == MarketplaceMode.seller) ...[
            _Action(
              const Key('home-add-material'),
              'Add Material',
              Icons.add_box_outlined,
              createMaterial,
            ),
            _Action(
              const Key('open-my-materials'),
              'My Materials',
              Icons.inventory_2_outlined,
              materials,
            ),
          ],
          if (mode == MarketplaceMode.buyer) ...[
            _Action(
              const Key('home-create-requirement'),
              'Create Requirement',
              Icons.post_add_outlined,
              createRequirement,
            ),
            _Action(
              const Key('open-my-requirements'),
              'My Requirements',
              Icons.assignment_outlined,
              requirements,
            ),
          ],
          if (mode == MarketplaceMode.buyer || mode == MarketplaceMode.seller)
            _Action(
              const Key('home-open-offers'),
              'My Offers',
              Icons.local_offer_outlined,
              offers,
              highlight: highlight,
            ),
        ],
      ),
    ],
  );
}

class _Action extends StatefulWidget {
  const _Action(
    this.keyValue,
    this.label,
    this.icon,
    this.onTap, {
    this.highlight = false,
  }) : super(key: keyValue);
  final Key keyValue;
  final String label;
  final IconData icon;
  final VoidCallback? onTap;
  final bool highlight;
  @override
  State<_Action> createState() => _ActionState();
}

class _ActionState extends State<_Action> {
  bool pressed = false;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: 164,
    child: AnimatedScale(
      scale: pressed ? .97 : 1,
      duration: const Duration(milliseconds: 110),
      child: Card(
        child: InkWell(
          onTap: widget.onTap,
          onHighlightChanged: (v) => setState(() => pressed = v),
          borderRadius: BorderRadius.circular(16),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 220),
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(16),
              border: widget.highlight
                  ? Border.all(color: SurplusLinkTheme.amber, width: 1.5)
                  : null,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(widget.icon, color: SurplusLinkTheme.amberDark),
                const SizedBox(height: 17),
                Text(
                  widget.label,
                  style: Theme.of(context).textTheme.labelLarge
                      ?.copyWith(fontWeight: FontWeight.w800),
                ),
              ],
            ),
          ),
        ),
      ),
    ),
  );
}

class _Activity extends StatelessWidget {
  const _Activity({
    required this.rows,
    required this.loading,
    this.error,
    required this.onRetry,
  });
  final List<DashboardActivity> rows;
  final bool loading;
  final String? error;
  final Future<void> Function() onRetry;
  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const _Title('Recent activity'),
      const SizedBox(height: 10),
      Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: loading && rows.isEmpty
              ? const Column(
                  children: [
                    _Skeleton(18),
                    SizedBox(height: 14),
                    _Skeleton(18),
                    SizedBox(height: 14),
                    _Skeleton(18),
                  ],
                )
              : rows.isEmpty
              ? Column(
                  children: [
                    const Icon(
                      Icons.inbox_outlined,
                      color: SurplusLinkTheme.slate600,
                      size: 32,
                    ),
                    const SizedBox(height: 8),
                    const Text('No recent workflow activity yet.'),
                    if (error != null)
                      TextButton(
                        onPressed: onRetry,
                        child: const Text('Retry'),
                      ),
                  ],
                )
              : Column(
                  children: [
                    for (final row in rows.take(4))
                      ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: Icon(
                          row.kind == 'transaction'
                              ? Icons.local_shipping_outlined
                              : row.kind == 'listing'
                              ? Icons.inventory_2_outlined
                              : Icons.assignment_outlined,
                          color: SurplusLinkTheme.amberDark,
                        ),
                        title: Text(
                          row.label,
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        subtitle: Text(_time(row.when)),
                      ),
                    if (error != null)
                      Align(
                        alignment: Alignment.centerLeft,
                        child: TextButton.icon(
                          onPressed: onRetry,
                          icon: const Icon(Icons.refresh),
                          label: const Text('Retry dashboard'),
                        ),
                      ),
                  ],
                ),
        ),
      ),
    ],
  );
}

class _Title extends StatelessWidget {
  const _Title(this.text);
  final String text;
  @override
  Widget build(BuildContext context) => Text(
    text,
    style: Theme.of(context).textTheme.titleLarge?.copyWith(
      fontWeight: FontWeight.w900,
      color: SurplusLinkTheme.slate900,
    ),
  );
}

class _Skeleton extends StatefulWidget {
  const _Skeleton(this.height);
  final double height;
  @override
  State<_Skeleton> createState() => _SkeletonState();
}

class _SkeletonState extends State<_Skeleton>
    with SingleTickerProviderStateMixin {
  late final AnimationController controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 900),
  )..repeat(reverse: true);
  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: controller,
    builder: (_, _) => Container(
      key: const Key('home-loading-skeleton'),
      height: widget.height,
      decoration: BoxDecoration(
        color: SurplusLinkTheme.slate200.withValues(
          alpha: .45 + controller.value * .3,
        ),
        borderRadius: BorderRadius.circular(8),
      ),
    ),
  );
}

class _Entrance extends StatelessWidget {
  const _Entrance({
    required this.index,
    required this.controller,
    required this.child,
  });
  final int index;
  final Animation<double> controller;
  final Widget child;
  @override
  Widget build(BuildContext context) {
    final a = CurvedAnimation(
      parent: controller,
      curve: Interval(
        index * .12,
        .65 + index * .1,
        curve: Curves.easeOutCubic,
      ),
    );
    return FadeTransition(
      opacity: a,
      child: SlideTransition(
        position: Tween<Offset>(
          begin: const Offset(0, .08),
          end: Offset.zero,
        ).animate(a),
        child: child,
      ),
    );
  }
}

String _time(DateTime value) {
  final d = DateTime.now().difference(value.toLocal());
  if (d.inMinutes < 1) return 'Just now';
  if (d.inHours < 1) return '${d.inMinutes}m ago';
  if (d.inDays < 1) return '${d.inHours}h ago';
  return '${d.inDays}d ago';
}

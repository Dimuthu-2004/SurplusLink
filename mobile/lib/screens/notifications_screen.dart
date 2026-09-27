import 'package:flutter/material.dart';
import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/notifications/notification_card.dart';
import 'package:mobile/notifications/notification_controller.dart';
import 'package:mobile/notifications/notification_models.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({
    this.controller,
    super.key,
  });

  final NotificationController? controller;

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final ctrl = widget.controller ?? NotificationScope.maybeOf(context);
      ctrl?.refreshUnreadCount();
      ctrl?.load(reset: true);
    });
  }

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller ?? NotificationScope.maybeOf(context);

    if (controller == null) {
      return const Scaffold(
        body: Center(child: Text('Notifications unavailable.')),
      );
    }

    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        final isDualRole = isDualMarketplaceUser(controller.user);
        final unreadCount = controller.unreadCount;

        final availableTabs = [
          NotificationFilterTab.all,
          NotificationFilterTab.unread,
          NotificationFilterTab.actionRequired,
          if (isDualRole) ...[
            NotificationFilterTab.buyer,
            NotificationFilterTab.seller,
          ],
        ];

        return Scaffold(
          backgroundColor: SurplusLinkTheme.background,
          appBar: AppBar(
            leading: const DashboardBackButton(fallback: '/home'),
            title: const Text(
              'Notifications',
              style: TextStyle(
                fontWeight: FontWeight.w900,
                fontSize: 20,
                color: SurplusLinkTheme.slate900,
              ),
            ),
            actions: [
              TextButton(
                key: const Key('notifications-mark-all-read'),
                onPressed: unreadCount > 0 ? () => controller.markAllRead() : null,
                style: TextButton.styleFrom(
                  foregroundColor: SurplusLinkTheme.amberDark,
                  disabledForegroundColor: SurplusLinkTheme.slate300,
                ),
                child: const Text(
                  'Mark all read',
                  style: TextStyle(fontWeight: FontWeight.w700),
                ),
              ),
              const SizedBox(width: 8),
            ],
          ),
          body: Column(
            children: [
              _FilterTabBar(
                tabs: availableTabs,
                activeTab: controller.activeTab,
                onTabSelected: controller.setFilterTab,
              ),
              Expanded(
                child: RefreshIndicator(
                  color: SurplusLinkTheme.amber,
                  onRefresh: () async {
                    await Future.wait([
                      controller.load(reset: true),
                      controller.refreshUnreadCount(),
                    ]);
                  },
                  child: _buildContent(context, controller, isDualRole),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildContent(
    BuildContext context,
    NotificationController controller,
    bool isDualRole,
  ) {
    if (controller.isLoading && controller.items.isEmpty) {
      return ListView(
        key: const Key('notifications-skeleton-list'),
        padding: const EdgeInsets.all(16),
        children: const [
          _NotificationSkeleton(),
          SizedBox(height: 12),
          _NotificationSkeleton(),
          SizedBox(height: 12),
          _NotificationSkeleton(),
        ],
      );
    }

    if (controller.error != null && controller.items.isEmpty) {
      return ListView(
        key: const Key('notifications-error-view'),
        padding: const EdgeInsets.all(32),
        children: [
          const SizedBox(height: 60),
          const Icon(
            Icons.cloud_off_outlined,
            size: 56,
            color: SurplusLinkTheme.slate600,
          ),
          const SizedBox(height: 16),
          const Center(
            child: Text(
              'Failed to load notifications',
              style: TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 16,
                color: SurplusLinkTheme.slate900,
              ),
            ),
          ),
          const SizedBox(height: 8),
          Center(
            child: Text(
              controller.error!,
              style: const TextStyle(color: SurplusLinkTheme.slate600),
              textAlign: TextAlign.center,
            ),
          ),
          const SizedBox(height: 20),
          Center(
            child: FilledButton.icon(
              key: const Key('notifications-retry-button'),
              onPressed: () => controller.load(reset: true),
              icon: const Icon(Icons.refresh),
              label: const Text('Retry'),
            ),
          ),
        ],
      );
    }

    if (controller.items.isEmpty) {
      return ListView(
        key: const Key('notifications-empty-view'),
        padding: const EdgeInsets.all(32),
        children: [
          const SizedBox(height: 80),
          Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              color: SurplusLinkTheme.amberSoft.withValues(alpha: 0.5),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.notifications_none_outlined,
              size: 36,
              color: SurplusLinkTheme.amberDark,
            ),
          ),
          const SizedBox(height: 16),
          Center(
            child: Text(
              controller.activeTab == NotificationFilterTab.all
                  ? 'No notifications yet'
                  : 'No ${controller.activeTab.label.toLowerCase()} notifications',
              style: const TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 18,
                color: SurplusLinkTheme.slate900,
              ),
            ),
          ),
          const SizedBox(height: 8),
          const Center(
            child: Text(
              "You're all caught up! Updates regarding matches, handovers, and transactions will appear here.",
              style: TextStyle(
                color: SurplusLinkTheme.slate600,
                fontSize: 14,
                height: 1.4,
              ),
              textAlign: TextAlign.center,
            ),
          ),
        ],
      );
    }

    return ListView.builder(
      key: const Key('notifications-list'),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 40),
      itemCount: controller.items.length + (controller.hasMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index == controller.items.length) {
          return Padding(
            padding: const EdgeInsets.symmetric(vertical: 16),
            child: Center(
              child: controller.isLoadingMore
                  ? const CircularProgressIndicator()
                  : OutlinedButton(
                      key: const Key('notifications-load-more'),
                      onPressed: controller.loadMore,
                      child: const Text('Load older notifications'),
                    ),
            ),
          );
        }

        final item = controller.items[index];
        return NotificationCard(
          notification: item,
          showContextBadge: isDualRole,
        );
      },
    );
  }
}

class _FilterTabBar extends StatelessWidget {
  const _FilterTabBar({
    required this.tabs,
    required this.activeTab,
    required this.onTabSelected,
  });

  final List<NotificationFilterTab> tabs;
  final NotificationFilterTab activeTab;
  final ValueChanged<NotificationFilterTab> onTabSelected;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 52,
      padding: const EdgeInsets.symmetric(vertical: 8),
      decoration: const BoxDecoration(
        color: SurplusLinkTheme.surface,
        border: Border(
          bottom: BorderSide(color: SurplusLinkTheme.slate200),
        ),
      ),
      child: ListView.separated(
        key: const Key('notifications-filter-tabs'),
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: tabs.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (context, index) {
          final tab = tabs[index];
          final isSelected = tab == activeTab;

          return InkWell(
            key: Key('notification-tab-${tab.name}'),
            borderRadius: BorderRadius.circular(20),
            onTap: () => onTabSelected(tab),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 180),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              decoration: BoxDecoration(
                color: isSelected
                    ? SurplusLinkTheme.amber
                    : SurplusLinkTheme.surfaceSoft,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: isSelected
                      ? SurplusLinkTheme.amber
                      : SurplusLinkTheme.slate200,
                ),
                boxShadow: isSelected
                    ? [
                        BoxShadow(
                          color: SurplusLinkTheme.amber.withValues(alpha: 0.3),
                          blurRadius: 6,
                          offset: const Offset(0, 2),
                        ),
                      ]
                    : null,
              ),
              child: Center(
                child: Text(
                  tab.label,
                  style: TextStyle(
                    color: isSelected ? Colors.white : SurplusLinkTheme.slate700,
                    fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                    fontSize: 13,
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

class _NotificationSkeleton extends StatefulWidget {
  const _NotificationSkeleton();

  @override
  State<_NotificationSkeleton> createState() => _NotificationSkeletonState();
}

class _NotificationSkeletonState extends State<_NotificationSkeleton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 900),
  )..repeat(reverse: true);

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      builder: (context, _) {
        final shimmer = SurplusLinkTheme.slate200.withValues(
          alpha: 0.45 + _controller.value * 0.35,
        );

        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: SurplusLinkTheme.surface,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: SurplusLinkTheme.slate200),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 38,
                    height: 38,
                    decoration: BoxDecoration(
                      color: shimmer,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 140,
                          height: 14,
                          decoration: BoxDecoration(
                            color: shimmer,
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                        const SizedBox(height: 6),
                        Container(
                          width: 70,
                          height: 10,
                          decoration: BoxDecoration(
                            color: shimmer,
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              Container(
                width: double.infinity,
                height: 12,
                decoration: BoxDecoration(
                  color: shimmer,
                  borderRadius: BorderRadius.circular(4),
                ),
              ),
              const SizedBox(height: 6),
              Container(
                width: 220,
                height: 12,
                decoration: BoxDecoration(
                  color: shimmer,
                  borderRadius: BorderRadius.circular(4),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

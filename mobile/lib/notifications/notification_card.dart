import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'notification_controller.dart';
import 'notification_models.dart';
import 'notification_navigation.dart';

class NotificationCard extends StatelessWidget {
  const NotificationCard({
    required this.notification,
    this.onTap,
    this.showContextBadge = false,
    super.key,
  });

  final NotificationItem notification;
  final VoidCallback? onTap;
  final bool showContextBadge;

  @override
  Widget build(BuildContext context) {
    final isUnread = !notification.isRead;
    final actionLabel = getNotificationActionLabel(notification);

    return AnimatedContainer(
      duration: const Duration(milliseconds: 220),
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: isUnread
            ? SurplusLinkTheme.amberSoft.withValues(alpha: 0.25)
            : SurplusLinkTheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isUnread
              ? SurplusLinkTheme.amber.withValues(alpha: 0.45)
              : SurplusLinkTheme.slate200,
          width: isUnread ? 1.5 : 1.0,
        ),
        boxShadow: [
          BoxShadow(
            color: SurplusLinkTheme.slate900.withValues(alpha: isUnread ? 0.05 : 0.02),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(16),
        child: InkWell(
          key: Key('notification-card-${notification.id}'),
          borderRadius: BorderRadius.circular(16),
          onTap: onTap ??
              () async {
                final controller = NotificationScope.maybeOf(context);
                if (controller != null && !notification.isRead) {
                  await controller.markRead(notification);
                }
                if (context.mounted) {
                  final route = resolveNotificationRoute(notification);
                  context.push(route);
                }
              },
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _IconBadge(
                      type: notification.type,
                      priority: notification.priority,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  notification.title,
                                  style: Theme.of(context)
                                      .textTheme
                                      .titleSmall
                                      ?.copyWith(
                                        color: SurplusLinkTheme.slate900,
                                        fontWeight: isUnread
                                            ? FontWeight.w900
                                            : FontWeight.w700,
                                      ),
                                ),
                              ),
                              if (isUnread) ...[
                                const SizedBox(width: 8),
                                Container(
                                  key: const Key('notification-card-unread-dot'),
                                  width: 8,
                                  height: 8,
                                  decoration: const BoxDecoration(
                                    color: SurplusLinkTheme.amber,
                                    shape: BoxShape.circle,
                                  ),
                                ),
                              ],
                            ],
                          ),
                          const SizedBox(height: 4),
                          Text(
                            formatRelativeTime(notification.createdAt),
                            style: Theme.of(context)
                                .textTheme
                                .bodySmall
                                ?.copyWith(
                                  color: SurplusLinkTheme.slate600,
                                  fontSize: 12,
                                ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Text(
                  notification.message,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: SurplusLinkTheme.slate700,
                        height: 1.4,
                      ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    _PriorityBadge(priority: notification.priority),
                    if (showContextBadge &&
                        notification.context != NotificationContext.system) ...[
                      const SizedBox(width: 8),
                      _ContextBadge(notificationContext: notification.context),
                    ],
                    const Spacer(),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 5,
                      ),
                      decoration: BoxDecoration(
                        color: SurplusLinkTheme.amberSoft,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            actionLabel,
                            style: const TextStyle(
                              color: SurplusLinkTheme.amberDark,
                              fontWeight: FontWeight.w800,
                              fontSize: 12,
                            ),
                          ),
                          const SizedBox(width: 4),
                          const Icon(
                            Icons.arrow_forward_ios_rounded,
                            size: 11,
                            color: SurplusLinkTheme.amberDark,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _IconBadge extends StatelessWidget {
  const _IconBadge({required this.type, required this.priority});

  final String type;
  final NotificationPriority priority;

  @override
  Widget build(BuildContext context) {
    final (icon, bg, fg) = _iconDetails(type, priority);

    return Container(
      width: 40,
      height: 40,
      decoration: BoxDecoration(
        color: bg,
        shape: BoxShape.circle,
      ),
      child: Center(
        child: Icon(icon, color: fg, size: 20),
      ),
    );
  }

  (IconData, Color, Color) _iconDetails(
    String type,
    NotificationPriority priority,
  ) {
    final t = type.toUpperCase();
    if (t.contains('SELLER_CONFIRMED_HANDOVER') ||
        t.contains('HANDOVER_CONFIRMATION_REQUIRED') ||
        t.contains('RECEIPT_CONFIRMATION_REQUIRED')) {
      return (
        Icons.local_shipping_outlined,
        SurplusLinkTheme.amberSoft,
        SurplusLinkTheme.amberDark,
      );
    }
    if (t.contains('MATCHES_READY')) {
      return (
        Icons.auto_awesome_outlined,
        const Color(0xFFEDE9FE),
        const Color(0xFF7C3AED),
      );
    }
    if (t.contains('LISTING_APPROVED') || t.contains('SELECTION_APPROVED')) {
      return (
        Icons.check_circle_outline,
        const Color(0xFFDCFCE7),
        const Color(0xFF16A34A),
      );
    }
    if (t.contains('TRANSACTION_COMPLETED') ||
        t.contains('MANAGER_CONFIRMED_COMPLETION')) {
      return (
        Icons.task_alt_outlined,
        const Color(0xFFDCFCE7),
        const Color(0xFF15803D),
      );
    }
    if (t.contains('OVERDUE') || t.contains('NO_HANDOVER')) {
      return (
        Icons.hourglass_top_outlined,
        const Color(0xFFFEF3C7),
        const Color(0xFFD97706),
      );
    }
    if (t.contains('ISSUE') || t.contains('TRANSACTION_NOT_COMPLETED')) {
      return (
        Icons.report_problem_outlined,
        const Color(0xFFFEE2E2),
        const Color(0xFFDC2626),
      );
    }
    if (t.contains('REJECTED')) {
      return (
        Icons.cancel_outlined,
        const Color(0xFFFEE2E2),
        const Color(0xFFDC2626),
      );
    }
    return (
      Icons.notifications_none_outlined,
      SurplusLinkTheme.slate200,
      SurplusLinkTheme.slate700,
    );
  }
}

class _PriorityBadge extends StatelessWidget {
  const _PriorityBadge({required this.priority});

  final NotificationPriority priority;

  @override
  Widget build(BuildContext context) {
    final (bg, fg, label) = switch (priority) {
      NotificationPriority.actionRequired => (
          SurplusLinkTheme.amberSoft,
          SurplusLinkTheme.amberDark,
          'Action Required',
        ),
      NotificationPriority.warning => (
          const Color(0xFFFEF3C7),
          const Color(0xFFB45309),
          'Warning',
        ),
      NotificationPriority.critical => (
          const Color(0xFFFEE2E2),
          const Color(0xFFB91C1C),
          'Critical',
        ),
      NotificationPriority.success => (
          const Color(0xFFDCFCE7),
          const Color(0xFF15803D),
          'Success',
        ),
      NotificationPriority.info => (
          SurplusLinkTheme.slate200.withValues(alpha: 0.6),
          SurplusLinkTheme.slate700,
          'Info',
        ),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: fg,
          fontSize: 10,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

class _ContextBadge extends StatelessWidget {
  const _ContextBadge({required this.notificationContext});

  final NotificationContext notificationContext;

  @override
  Widget build(BuildContext context) {
    final label = notificationContext == NotificationContext.buyer ? 'BUYER' : 'SELLER';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      decoration: BoxDecoration(
        color: SurplusLinkTheme.slate200,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: SurplusLinkTheme.slate800,
          fontSize: 10,
          fontWeight: FontWeight.w800,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}

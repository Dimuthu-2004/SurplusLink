import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'notification_controller.dart';

class NotificationBell extends StatelessWidget {
  const NotificationBell({
    this.controller,
    this.color,
    this.badgeColor,
    this.badgeTextColor,
    this.iconSize = 24,
    super.key,
  });

  final NotificationController? controller;
  final Color? color;
  final Color? badgeColor;
  final Color? badgeTextColor;
  final double iconSize;

  @override
  Widget build(BuildContext context) {
    final effectiveController = controller ?? NotificationScope.maybeOf(context);

    return ListenableBuilder(
      listenable: effectiveController ?? ValueNotifier<int>(0),
      builder: (context, _) {
        final count = effectiveController?.unreadCount ?? 0;
        final hasUnread = count > 0;
        final countLabel = count > 99 ? '99+' : count.toString();

        return Tooltip(
          message: hasUnread ? '$count unread notifications' : 'Notifications',
          child: InkWell(
            key: const Key('notification-bell'),
            borderRadius: BorderRadius.circular(12),
            onTap: () => context.push('/notifications'),
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: Stack(
                clipBehavior: Clip.none,
                alignment: Alignment.center,
                children: [
                  Icon(
                    hasUnread
                        ? Icons.notifications_active_outlined
                        : Icons.notifications_none_outlined,
                    color: color ?? SurplusLinkTheme.slate700,
                    size: iconSize,
                  ),
                  Positioned(
                    top: -4,
                    right: -6,
                    child: AnimatedScale(
                      scale: hasUnread ? 1.0 : 0.0,
                      duration: const Duration(milliseconds: 260),
                      curve: Curves.easeOutBack,
                      child: AnimatedOpacity(
                        opacity: hasUnread ? 1.0 : 0.0,
                        duration: const Duration(milliseconds: 200),
                        child: Container(
                          key: const Key('notification-unread-badge'),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 5,
                            vertical: 2,
                          ),
                          constraints: const BoxConstraints(
                            minWidth: 18,
                            minHeight: 18,
                          ),
                          decoration: BoxDecoration(
                            color: badgeColor ?? SurplusLinkTheme.amber,
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(
                              color: SurplusLinkTheme.surface,
                              width: 1.5,
                            ),
                            boxShadow: [
                              BoxShadow(
                                color: (badgeColor ?? SurplusLinkTheme.amber)
                                    .withValues(alpha: 0.35),
                                blurRadius: 4,
                                offset: const Offset(0, 1),
                              ),
                            ],
                          ),
                          child: Center(
                            child: Text(
                              countLabel,
                              style: TextStyle(
                                color: badgeTextColor ?? Colors.white,
                                fontSize: 10,
                                fontWeight: FontWeight.w900,
                                height: 1.1,
                              ),
                              textAlign: TextAlign.center,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}

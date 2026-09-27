import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'notification_controller.dart';
import 'notification_models.dart';

class NotificationActionBanner extends StatelessWidget {
  const NotificationActionBanner({
    this.controller,
    super.key,
  });

  final NotificationController? controller;

  @override
  Widget build(BuildContext context) {
    final effectiveController = controller ?? NotificationScope.maybeOf(context);

    if (effectiveController == null) return const SizedBox.shrink();

    return ListenableBuilder(
      listenable: effectiveController,
      builder: (context, _) {
        final actionItems = effectiveController.items
            .where((item) => !item.isRead && item.priority == NotificationPriority.actionRequired)
            .toList();

        if (actionItems.isEmpty) return const SizedBox.shrink();

        final firstAction = actionItems.first;
        final hasMultiple = actionItems.length > 1;

        final title = hasMultiple
            ? '${actionItems.length} Actions Required'
            : (firstAction.type.contains('SELLER_CONFIRMED_HANDOVER')
                ? 'Receipt confirmation required'
                : (firstAction.type.contains('HANDOVER_CONFIRMATION_REQUIRED')
                    ? 'Handover confirmation required'
                    : firstAction.title));

        final message = hasMultiple
            ? 'You have multiple pending actions across your transactions and requirements.'
            : firstAction.message;

        return Container(
          key: const Key('dashboard-notification-banner'),
          margin: const EdgeInsets.only(bottom: 18),
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: SurplusLinkTheme.amberSoft,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: SurplusLinkTheme.amber.withValues(alpha: 0.5),
              width: 1.5,
            ),
            boxShadow: [
              BoxShadow(
                color: SurplusLinkTheme.amber.withValues(alpha: 0.15),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: SurplusLinkTheme.amber,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.priority_high_rounded,
                  color: Colors.white,
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontWeight: FontWeight.w900,
                        fontSize: 14,
                        color: SurplusLinkTheme.slate900,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      message,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 12,
                        color: SurplusLinkTheme.slate700,
                        height: 1.3,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        InkWell(
                          key: const Key('dashboard-banner-action-button'),
                          onTap: () {
                            if (hasMultiple) {
                              context.push('/notifications');
                            } else {
                              effectiveController.markRead(firstAction);
                              context.push('/offers');
                            }
                          },
                          borderRadius: BorderRadius.circular(6),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 2, horizontal: 4),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  hasMultiple ? 'View all in Notifications' : 'Review in My Offers',
                                  style: const TextStyle(
                                    color: SurplusLinkTheme.amberDark,
                                    fontWeight: FontWeight.w900,
                                    fontSize: 12,
                                  ),
                                ),
                                const SizedBox(width: 4),
                                const Icon(
                                  Icons.arrow_forward_rounded,
                                  size: 14,
                                  color: SurplusLinkTheme.amberDark,
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

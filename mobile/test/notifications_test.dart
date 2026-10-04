import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/app.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/notifications/notification_banner.dart';
import 'package:mobile/notifications/notification_bell.dart';
import 'package:mobile/notifications/notification_card.dart';
import 'package:mobile/notifications/notification_controller.dart';
import 'package:mobile/notifications/notification_gateway.dart';
import 'package:mobile/notifications/notification_models.dart';
import 'package:mobile/notifications/notification_navigation.dart';
import 'package:mobile/screens/notifications_screen.dart';

import 'support/fakes.dart';

class FakeNotificationGateway implements NotificationGateway {
  FakeNotificationGateway({
    List<NotificationItem>? initialItems,
    int? initialUnread,
  })  : items = List.from(initialItems ?? []),
        unread = initialUnread ?? (initialItems?.where((i) => !i.isRead).length ?? 0);

  List<NotificationItem> items;
  int unread;
  int unreadCountCalls = 0;
  int listCalls = 0;
  int markReadCalls = 0;
  int markAllReadCalls = 0;
  NotificationQuery? lastQuery;

  @override
  Future<NotificationPage> list(NotificationQuery query) async {
    listCalls++;
    lastQuery = query;

    var filtered = List<NotificationItem>.from(items);
    if (query.unread == true) {
      filtered = filtered.where((i) => !i.isRead).toList();
    }
    if (query.context != null) {
      filtered = filtered.where((i) => i.context == query.context).toList();
    }
    if (query.priority != null) {
      filtered = filtered.where((i) => i.priority == query.priority).toList();
    }

    return NotificationPage(
      items: filtered,
      total: filtered.length,
      page: query.page,
      pageSize: query.pageSize,
      totalPages: (filtered.length / query.pageSize).ceil().clamp(1, 999),
    );
  }

  @override
  Future<int> unreadCount() async {
    unreadCountCalls++;
    return unread;
  }

  @override
  Future<NotificationItem> markRead(String id) async {
    markReadCalls++;
    final index = items.indexWhere((i) => i.id == id);
    if (index != -1) {
      final updated = items[index].copyWith(isRead: true, readAt: DateTime.now());
      items[index] = updated;
      if (unread > 0) unread--;
      return updated;
    }
    throw Exception('Not found');
  }

  @override
  Future<void> markAllRead() async {
    markAllReadCalls++;
    items = items.map((i) => i.copyWith(isRead: true, readAt: DateTime.now())).toList();
    unread = 0;
  }
}

class FailingReadNotificationGateway extends FakeNotificationGateway {
  FailingReadNotificationGateway({required super.initialItems, super.initialUnread});

  @override
  Future<NotificationItem> markRead(String id) async {
    markReadCalls++;
    throw StateError('The notification service is unavailable.');
  }
}

void main() {
  group('Notification Models & Navigation', () {
    test('NotificationItem.fromJson parses correctly', () {
      final json = {
        'id': 'notif-1',
        'type': 'SELLER_CONFIRMED_HANDOVER',
        'title': 'Seller confirmed handover',
        'message': 'ABC Materials marked 20 bags of Cement as handed over. Please confirm receipt.',
        'context': 'BUYER',
        'priority': 'ACTION_REQUIRED',
        'entityType': 'Transaction',
        'entityId': 'tx-100',
        'isRead': false,
        'createdAt': '2026-09-28T00:00:00Z',
      };

      final item = NotificationItem.fromJson(json);
      expect(item.id, 'notif-1');
      expect(item.type, 'SELLER_CONFIRMED_HANDOVER');
      expect(item.title, 'Seller confirmed handover');
      expect(item.context, NotificationContext.buyer);
      expect(item.priority, NotificationPriority.actionRequired);
      expect(item.entityType, 'Transaction');
      expect(item.entityId, 'tx-100');
      expect(item.isRead, isFalse);
    });

    test('NotificationItem.fromJson accepts numeric legacy identifiers', () {
      final item = NotificationItem.fromJson({
        'id': 70,
        'type': 'MATCHES_READY',
        'title': 'Matches ready',
        'message': 'A notification stored with a numeric identifier.',
        'context': 'BUYER',
        'priority': 'INFO',
        'entityId': 23,
        'isRead': false,
        'createdAt': '2026-10-04T00:00:00Z',
      });

      expect(item.id, '70');
      expect(item.entityId, '23');
      expect(item.title, 'Matches ready');
    });

    test('resolveNotificationRoute routes correctly to existing screens', () {
      final matchesNotif = NotificationItem(
        id: '1',
        type: 'MATCHES_READY',
        title: 'Matches Ready',
        message: 'Matches found',
        context: NotificationContext.buyer,
        priority: NotificationPriority.info,
        entityType: 'MaterialRequest',
        entityId: 'req-456',
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(resolveNotificationRoute(matchesNotif), '/requirements/req-456/matches');

      final listingNotif = NotificationItem(
        id: '2',
        type: 'LISTING_APPROVED',
        title: 'Listing Approved',
        message: 'Your listing is active',
        context: NotificationContext.seller,
        priority: NotificationPriority.success,
        entityType: 'Listing',
        entityId: 'mat-789',
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(resolveNotificationRoute(listingNotif), '/materials/mat-789');

      final handoverNotif = NotificationItem(
        id: '3',
        type: 'HANDOVER_CONFIRMATION_REQUIRED',
        title: 'Handover confirmation required',
        message: 'Confirm handover',
        context: NotificationContext.seller,
        priority: NotificationPriority.actionRequired,
        entityType: 'Transaction',
        entityId: 'tx-1',
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(resolveNotificationRoute(handoverNotif), '/offers');

      final receiptNotif = NotificationItem(
        id: '4',
        type: 'SELLER_CONFIRMED_HANDOVER',
        title: 'Seller confirmed handover',
        message: 'Confirm receipt',
        context: NotificationContext.buyer,
        priority: NotificationPriority.actionRequired,
        entityType: 'Transaction',
        entityId: 'tx-2',
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(resolveNotificationRoute(receiptNotif), '/offers');

      final completedNotif = NotificationItem(
        id: '5',
        type: 'TRANSACTION_COMPLETED',
        title: 'Transaction completed',
        message: 'All completed',
        context: NotificationContext.buyer,
        priority: NotificationPriority.success,
        entityType: 'Transaction',
        entityId: 'tx-3',
        isRead: true,
        createdAt: DateTime.now(),
      );
      expect(resolveNotificationRoute(completedNotif), '/offers');
    });

    test('resolveNotificationRoute ignores web-only action routes', () {
      final notification = NotificationItem(
        id: 'manager-1',
        type: 'SELECTION_AVAILABILITY_CHANGED',
        title: 'Selection changed',
        message: 'Review the selection.',
        context: NotificationContext.manager,
        priority: NotificationPriority.warning,
        entityType: 'AgentWorkflow',
        entityId: 'workflow-1',
        actionRoute: '/app/manager/workflows/workflow-1',
        isRead: false,
        createdAt: DateTime.now(),
      );

      expect(resolveNotificationRoute(notification), '/offers');
    });

    test('getNotificationActionLabel provides context-specific action labels', () {
      final receiptNotif = NotificationItem(
        id: '1',
        type: 'SELLER_CONFIRMED_HANDOVER',
        title: 'Seller confirmed handover',
        message: 'Confirm receipt',
        context: NotificationContext.buyer,
        priority: NotificationPriority.actionRequired,
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(getNotificationActionLabel(receiptNotif), 'Confirm receipt');

      final handoverNotif = NotificationItem(
        id: '2',
        type: 'HANDOVER_CONFIRMATION_REQUIRED',
        title: 'Handover confirmation required',
        message: 'Confirm handover',
        context: NotificationContext.seller,
        priority: NotificationPriority.actionRequired,
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(getNotificationActionLabel(handoverNotif), 'Confirm handover');

      final matchesNotif = NotificationItem(
        id: '3',
        type: 'MATCHES_READY',
        title: 'Matches Ready',
        message: 'Matches',
        context: NotificationContext.buyer,
        priority: NotificationPriority.info,
        isRead: false,
        createdAt: DateTime.now(),
      );
      expect(getNotificationActionLabel(matchesNotif), 'View matches');
    });

    test('formatRelativeTime produces friendly time labels', () {
      final now = DateTime.now();
      expect(formatRelativeTime(now.subtract(const Duration(seconds: 30))), 'Just now');
      expect(formatRelativeTime(now.subtract(const Duration(minutes: 5))), '5 min ago');
      expect(formatRelativeTime(now.subtract(const Duration(hours: 3))), '3 hours ago');
      expect(formatRelativeTime(now.subtract(const Duration(days: 2))), '2 days ago');
    });
  });

  group('NotificationController', () {
    test('bindUser manages polling lifecycle and fetches unread count', () async {
      final gateway = FakeNotificationGateway(initialUnread: 3);
      final controller = NotificationController(
        gateway: gateway,
        pollInterval: const Duration(milliseconds: 50),
      );

      const user = AppUser(
        id: 'user-1',
        email: 'user@example.com',
        roles: [AppRole.buyer],
      );

      controller.bindUser(user);
      await Future.delayed(const Duration(milliseconds: 10));
      expect(controller.unreadCount, 3);
      expect(gateway.unreadCountCalls, greaterThanOrEqualTo(1));

      // Logout clears state and stops polling
      controller.bindUser(null);
      expect(controller.unreadCount, 0);
      expect(controller.items, isEmpty);

      controller.dispose();
    });

    test('refreshUnreadCount deduplicates in-flight calls', () async {
      final gateway = FakeNotificationGateway(initialUnread: 5);
      final controller = NotificationController(gateway: gateway);

      final f1 = controller.refreshUnreadCount();
      final f2 = controller.refreshUnreadCount();
      expect(identical(f1, f2), isTrue);

      await Future.wait([f1, f2]);
      expect(gateway.unreadCountCalls, 1);
      expect(controller.unreadCount, 5);

      controller.dispose();
    });

    test('markRead updates the item after backend persistence succeeds', () async {
      final item = NotificationItem(
        id: 'n-1',
        type: 'SELLER_CONFIRMED_HANDOVER',
        title: 'Handover',
        message: 'Msg',
        context: NotificationContext.buyer,
        priority: NotificationPriority.actionRequired,
        isRead: false,
        createdAt: DateTime.now(),
      );

      final gateway = FakeNotificationGateway(initialItems: [item], initialUnread: 1);
      final controller = NotificationController(gateway: gateway);
      await controller.load(reset: true);
      await controller.refreshUnreadCount();

      expect(controller.unreadCount, 1);
      expect(controller.items.first.isRead, isFalse);

      await controller.markRead(controller.items.first);
      expect(controller.unreadCount, 0);
      expect(controller.items.first.isRead, isTrue);
      expect(gateway.markReadCalls, 1);

      controller.dispose();
    });

    test('markRead keeps the persisted unread state when the API rejects it', () async {
      final item = NotificationItem(
        id: 'n-failed-read',
        type: 'MATCHES_READY',
        title: 'Matches ready',
        message: 'Message',
        context: NotificationContext.buyer,
        priority: NotificationPriority.info,
        isRead: false,
        createdAt: DateTime.now(),
      );
      final gateway = FailingReadNotificationGateway(initialItems: [item], initialUnread: 1);
      final controller = NotificationController(gateway: gateway);
      await controller.load();
      await controller.refreshUnreadCount();

      await controller.markRead(item);

      expect(controller.items.single.isRead, isFalse);
      expect(controller.unreadCount, 1);
      expect(gateway.markReadCalls, 1);
      controller.dispose();
    });

    test('optimistic markAllRead marks all items read and zeroes unread count', () async {
      final items = [
        NotificationItem(
          id: 'n-1',
          type: 'SELLER_CONFIRMED_HANDOVER',
          title: 'Handover',
          message: 'Msg',
          context: NotificationContext.buyer,
          priority: NotificationPriority.actionRequired,
          isRead: false,
          createdAt: DateTime.now(),
        ),
        NotificationItem(
          id: 'n-2',
          type: 'MATCHES_READY',
          title: 'Matches',
          message: 'Msg',
          context: NotificationContext.buyer,
          priority: NotificationPriority.info,
          isRead: false,
          createdAt: DateTime.now(),
        ),
      ];

      final gateway = FakeNotificationGateway(initialItems: items, initialUnread: 2);
      final controller = NotificationController(gateway: gateway);
      await controller.load(reset: true);
      await controller.refreshUnreadCount();

      expect(controller.unreadCount, 2);
      await controller.markAllRead();

      expect(controller.unreadCount, 0);
      expect(controller.items.every((i) => i.isRead), isTrue);
      expect(gateway.markAllReadCalls, 1);

      controller.dispose();
    });

    test('filter tabs build correct queries', () async {
      final gateway = FakeNotificationGateway();
      final controller = NotificationController(gateway: gateway);

      controller.setFilterTab(NotificationFilterTab.unread);
      await controller.load(reset: true);
      expect(gateway.lastQuery?.unread, isTrue);

      controller.setFilterTab(NotificationFilterTab.actionRequired);
      await controller.load(reset: true);
      expect(gateway.lastQuery?.priority, NotificationPriority.actionRequired);

      controller.setFilterTab(NotificationFilterTab.buyer);
      await controller.load(reset: true);
      expect(gateway.lastQuery?.context, NotificationContext.buyer);

      controller.setFilterTab(NotificationFilterTab.seller);
      await controller.load(reset: true);
      expect(gateway.lastQuery?.context, NotificationContext.seller);

      controller.dispose();
    });
  });

  group('Widget Tests', () {
    testWidgets('the application bell route loads the authenticated user notifications', (tester) async {
      final item = NotificationItem(
        id: 'persisted-notification',
        type: 'MATCHES_READY',
        title: 'Matches ready',
        message: 'A real API response is rendered by this shared screen.',
        context: NotificationContext.buyer,
        priority: NotificationPriority.info,
        isRead: false,
        createdAt: DateTime.now(),
      );
      final notifications = FakeNotificationGateway(initialItems: [item], initialUnread: 1);
      final auth = AuthController(FakeAuthGateway()..restoredUser = buyerUser);

      await tester.pumpWidget(SurplusLinkApp(
        authController: auth,
        notificationGateway: notifications,
        initialLocation: '/notifications',
      ));
      await tester.pumpAndSettle();

      expect(find.text('Notifications'), findsOneWidget);
      expect(find.text('Matches ready'), findsOneWidget);
      expect(find.text('Page not found: /notifications'), findsNothing);
      expect(notifications.lastQuery?.page, 1);

      await tester.pumpWidget(const SizedBox());
      auth.dispose();
    });

    testWidgets('NotificationBell renders unread badge and responds to taps', (tester) async {
      final gateway = FakeNotificationGateway(initialUnread: 4);
      final controller = NotificationController(gateway: gateway);
      await controller.refreshUnreadCount();

      bool navigated = false;
      final router = GoRouter(
        initialLocation: '/home',
        routes: [
          GoRoute(
            path: '/home',
            builder: (_, _) => Scaffold(
              appBar: AppBar(
                actions: [
                  NotificationBell(controller: controller),
                ],
              ),
            ),
          ),
          GoRoute(
            path: '/notifications',
            builder: (_, _) {
              navigated = true;
              return const Scaffold(body: Text('Notifications Page'));
            },
          ),
        ],
      );

      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('notification-bell')), findsOneWidget);
      expect(find.byKey(const Key('notification-unread-badge')), findsOneWidget);
      expect(find.text('4'), findsOneWidget);

      await tester.tap(find.byKey(const Key('notification-bell')));
      await tester.pumpAndSettle();

      expect(navigated, isTrue);
      expect(find.text('Notifications Page'), findsOneWidget);

      controller.dispose();
    });

    testWidgets('NotificationCard displays info, badges, and triggers markRead on tap', (tester) async {
      final item = NotificationItem(
        id: 'n-123',
        type: 'SELLER_CONFIRMED_HANDOVER',
        title: 'Seller confirmed handover',
        message: 'ABC Materials marked 20 bags of Cement as handed over.',
        context: NotificationContext.buyer,
        priority: NotificationPriority.actionRequired,
        entityType: 'Transaction',
        entityId: 'tx-999',
        isRead: false,
        createdAt: DateTime.now().subtract(const Duration(minutes: 5)),
      );

      final gateway = FakeNotificationGateway(initialItems: [item], initialUnread: 1);
      final controller = NotificationController(gateway: gateway);
      await controller.load(reset: true);
      await controller.refreshUnreadCount();

      bool navigatedToOffers = false;
      final router = GoRouter(
        initialLocation: '/test',
        routes: [
          GoRoute(
            path: '/test',
            builder: (ctx, _) => NotificationScope(
              controller: controller,
              child: Scaffold(
                body: NotificationCard(
                  notification: controller.items.first,
                  showContextBadge: true,
                ),
              ),
            ),
          ),
          GoRoute(
            path: '/offers',
            builder: (_, _) {
              navigatedToOffers = true;
              return const Scaffold(body: Text('My Offers Screen'));
            },
          ),
        ],
      );

      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
      await tester.pumpAndSettle();

      expect(find.text('Seller confirmed handover'), findsOneWidget);
      expect(find.text('ABC Materials marked 20 bags of Cement as handed over.'), findsOneWidget);
      expect(find.text('5 min ago'), findsOneWidget);
      expect(find.text('Action Required'), findsOneWidget);
      expect(find.text('BUYER'), findsOneWidget);
      expect(find.text('Confirm receipt'), findsOneWidget);
      expect(find.byKey(const Key('notification-card-unread-dot')), findsOneWidget);

      // Tap card
      await tester.tap(find.byKey(const Key('notification-card-n-123')));
      await tester.pumpAndSettle();

      expect(controller.unreadCount, 0);
      expect(navigatedToOffers, isTrue);

      controller.dispose();
    });

    testWidgets('NotificationsScreen displays dual-role tabs, empty state, and mark all read', (tester) async {
      final item = NotificationItem(
        id: 'n-1',
        type: 'MATCHES_READY',
        title: 'Matches ready',
        message: 'New matches found',
        context: NotificationContext.buyer,
        priority: NotificationPriority.info,
        isRead: false,
        createdAt: DateTime.now(),
      );

      final gateway = FakeNotificationGateway(initialItems: [item], initialUnread: 1);
      final controller = NotificationController(gateway: gateway);
      const dualUser = AppUser(
        id: 'u-1',
        email: 'dual@example.com',
        roles: [AppRole.buyer, AppRole.seller],
      );
      controller.bindUser(dualUser);
      await controller.load(reset: true);
      await controller.refreshUnreadCount();

      await tester.pumpWidget(
        MaterialApp(
          home: NotificationsScreen(controller: controller),
        ),
      );
      await tester.pumpAndSettle();

      // Check all 5 tabs rendered for dual-role user
      expect(find.byKey(const Key('notification-tab-all')), findsOneWidget);
      expect(find.byKey(const Key('notification-tab-unread')), findsOneWidget);
      expect(find.byKey(const Key('notification-tab-actionRequired')), findsOneWidget);
      expect(find.byKey(const Key('notification-tab-buyer')), findsOneWidget);
      expect(find.byKey(const Key('notification-tab-seller')), findsOneWidget);

      // Card is visible
      expect(find.text('Matches ready'), findsOneWidget);

      // Tap Mark all read
      await tester.tap(find.byKey(const Key('notifications-mark-all-read')));
      await tester.pumpAndSettle();

      expect(controller.unreadCount, 0);

      // Switch to an empty tab (e.g. Action Required)
      await tester.tap(find.byKey(const Key('notification-tab-actionRequired')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('notifications-empty-view')), findsOneWidget);
      expect(find.text("No action required notifications"), findsOneWidget);

      controller.dispose();
    });

    testWidgets('NotificationActionBanner displays non-blocking pending action without modal dialogs', (tester) async {
      final actionItem = NotificationItem(
        id: 'n-action',
        type: 'SELLER_CONFIRMED_HANDOVER',
        title: 'Seller confirmed handover',
        message: 'Please confirm receipt.',
        context: NotificationContext.buyer,
        priority: NotificationPriority.actionRequired,
        entityType: 'Transaction',
        entityId: 'tx-5',
        isRead: false,
        createdAt: DateTime.now(),
      );

      final gateway = FakeNotificationGateway(initialItems: [actionItem], initialUnread: 1);
      final controller = NotificationController(gateway: gateway);
      await controller.load(reset: true);
      await controller.refreshUnreadCount();

      bool openedOffers = false;
      final router = GoRouter(
        initialLocation: '/home',
        routes: [
          GoRoute(
            path: '/home',
            builder: (_, _) => NotificationScope(
              controller: controller,
              child: const Scaffold(
                body: NotificationActionBanner(),
              ),
            ),
          ),
          GoRoute(
            path: '/offers',
            builder: (_, _) {
              openedOffers = true;
              return const Scaffold(body: Text('Offers Screen'));
            },
          ),
        ],
      );

      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('dashboard-notification-banner')), findsOneWidget);
      expect(find.text('Receipt confirmation required'), findsOneWidget);

      // Tapping action navigates to /offers
      await tester.tap(find.byKey(const Key('dashboard-banner-action-button')));
      await tester.pumpAndSettle();

      expect(openedOffers, isTrue);

      controller.dispose();
    });
  });
}

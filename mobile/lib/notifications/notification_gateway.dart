import 'notification_models.dart';

abstract class NotificationGateway {
  Future<NotificationPage> list(NotificationQuery query);
  Future<int> unreadCount();
  Future<NotificationItem> markRead(String id);
  Future<void> markAllRead();
}

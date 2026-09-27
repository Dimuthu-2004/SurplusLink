import 'package:mobile/core/api_client.dart';
import 'notification_gateway.dart';
import 'notification_models.dart';

class NotificationRepository implements NotificationGateway {
  const NotificationRepository(this._apiClient);

  final ApiClient _apiClient;

  @override
  Future<NotificationPage> list(NotificationQuery query) async {
    final uri = Uri(
      path: '/api/notifications',
      queryParameters: query.toQueryParameters(),
    );
    final json = await _apiClient.getJson(uri.toString(), authenticated: true);
    return NotificationPage.fromJson(json);
  }

  @override
  Future<int> unreadCount() async {
    final json = await _apiClient.getJson(
      '/api/notifications/unread-count',
      authenticated: true,
    );
    return json['count'] as int? ?? 0;
  }

  @override
  Future<NotificationItem> markRead(String id) async {
    final encodedId = Uri.encodeComponent(id);
    final json = await _apiClient.postJson(
      '/api/notifications/$encodedId/read',
      const <String, dynamic>{},
      authenticated: true,
    );
    return NotificationItem.fromJson(json);
  }

  @override
  Future<void> markAllRead() async {
    await _apiClient.postJson(
      '/api/notifications/read-all',
      const <String, dynamic>{},
      authenticated: true,
    );
  }
}

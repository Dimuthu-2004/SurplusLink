enum NotificationContext {
  buyer,
  seller,
  manager,
  system;

  String get apiValue => name.toUpperCase();

  static NotificationContext fromApi(String value) => switch (value.toUpperCase()) {
    'BUYER' => NotificationContext.buyer,
    'SELLER' => NotificationContext.seller,
    'MANAGER' => NotificationContext.manager,
    'SYSTEM' => NotificationContext.system,
    _ => NotificationContext.system,
  };
}

enum NotificationPriority {
  info,
  success,
  actionRequired,
  warning,
  critical;

  String get apiValue => switch (this) {
    NotificationPriority.info => 'INFO',
    NotificationPriority.success => 'SUCCESS',
    NotificationPriority.actionRequired => 'ACTION_REQUIRED',
    NotificationPriority.warning => 'WARNING',
    NotificationPriority.critical => 'CRITICAL',
  };

  static NotificationPriority fromApi(String value) => switch (value.toUpperCase()) {
    'INFO' => NotificationPriority.info,
    'SUCCESS' => NotificationPriority.success,
    'ACTION_REQUIRED' => NotificationPriority.actionRequired,
    'WARNING' => NotificationPriority.warning,
    'CRITICAL' => NotificationPriority.critical,
    _ => NotificationPriority.info,
  };
}

class NotificationItem {
  const NotificationItem({
    required this.id,
    required this.type,
    required this.title,
    required this.message,
    required this.context,
    required this.priority,
    this.entityType,
    this.entityId,
    this.actionRoute,
    required this.isRead,
    required this.createdAt,
    this.readAt,
  });

  factory NotificationItem.fromJson(Map<String, dynamic> json) {
    return NotificationItem(
      id: json['id'] as String? ?? '',
      type: json['type'] as String? ?? '',
      title: json['title'] as String? ?? '',
      message: json['message'] as String? ?? '',
      context: NotificationContext.fromApi(json['context'] as String? ?? 'SYSTEM'),
      priority: NotificationPriority.fromApi(json['priority'] as String? ?? 'INFO'),
      entityType: json['entityType'] as String?,
      entityId: json['entityId'] as String?,
      actionRoute: json['actionRoute'] as String?,
      isRead: json['isRead'] as bool? ?? false,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'] as String)?.toLocal() ?? DateTime.now()
          : DateTime.now(),
      readAt: json['readAt'] != null
          ? DateTime.tryParse(json['readAt'] as String)?.toLocal()
          : null,
    );
  }

  final String id;
  final String type;
  final String title;
  final String message;
  final NotificationContext context;
  final NotificationPriority priority;
  final String? entityType;
  final String? entityId;
  final String? actionRoute;
  final bool isRead;
  final DateTime createdAt;
  final DateTime? readAt;

  NotificationItem copyWith({
    String? id,
    String? type,
    String? title,
    String? message,
    NotificationContext? context,
    NotificationPriority? priority,
    String? entityType,
    String? entityId,
    String? actionRoute,
    bool? isRead,
    DateTime? createdAt,
    DateTime? readAt,
  }) {
    return NotificationItem(
      id: id ?? this.id,
      type: type ?? this.type,
      title: title ?? this.title,
      message: message ?? this.message,
      context: context ?? this.context,
      priority: priority ?? this.priority,
      entityType: entityType ?? this.entityType,
      entityId: entityId ?? this.entityId,
      actionRoute: actionRoute ?? this.actionRoute,
      isRead: isRead ?? this.isRead,
      createdAt: createdAt ?? this.createdAt,
      readAt: readAt ?? this.readAt,
    );
  }
}

class NotificationPage {
  const NotificationPage({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
    required this.totalPages,
  });

  factory NotificationPage.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? [];
    return NotificationPage(
      items: rawItems
          .whereType<Map<String, dynamic>>()
          .map(NotificationItem.fromJson)
          .toList(),
      total: json['total'] as int? ?? 0,
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 20,
      totalPages: json['totalPages'] as int? ?? 1,
    );
  }

  final List<NotificationItem> items;
  final int total;
  final int page;
  final int pageSize;
  final int totalPages;
}

class NotificationQuery {
  const NotificationQuery({
    this.page = 1,
    this.pageSize = 20,
    this.unread,
    this.context,
    this.priority,
  });

  final int page;
  final int pageSize;
  final bool? unread;
  final NotificationContext? context;
  final NotificationPriority? priority;

  Map<String, String> toQueryParameters() {
    final params = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };
    if (unread != null) {
      params['unread'] = unread.toString();
    }
    if (context != null) {
      params['context'] = context!.apiValue;
    }
    if (priority != null) {
      params['priority'] = priority!.apiValue;
    }
    return params;
  }
}

enum NotificationFilterTab {
  all,
  unread,
  actionRequired,
  buyer,
  seller;

  String get label => switch (this) {
    NotificationFilterTab.all => 'All',
    NotificationFilterTab.unread => 'Unread',
    NotificationFilterTab.actionRequired => 'Action Required',
    NotificationFilterTab.buyer => 'Buyer',
    NotificationFilterTab.seller => 'Seller',
  };
}

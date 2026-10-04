import 'dart:async';
import 'package:flutter/widgets.dart';
import 'package:mobile/auth/auth_models.dart';
import 'notification_gateway.dart';
import 'notification_models.dart';

class NotificationController extends ChangeNotifier {
  NotificationController({
    required this.gateway,
    this.pollInterval = const Duration(seconds: 25),
  }) {
    _lifecycleListener = AppLifecycleListener(
      onResume: () {
        if (_user != null) {
          unawaited(refreshUnreadCount());
        }
      },
    );
  }

  final NotificationGateway gateway;
  final Duration pollInterval;

  late final AppLifecycleListener _lifecycleListener;
  Timer? _pollTimer;
  AppUser? _user;
  bool _disposed = false;

  int _unreadCount = 0;
  List<NotificationItem> _items = const [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  String? _error;
  NotificationFilterTab _activeTab = NotificationFilterTab.all;
  int _page = 1;
  final int _pageSize = 20;
  int _total = 0;
  int _totalPages = 1;

  Future<int>? _inFlightUnread;
  Future<void>? _inFlightLoad;

  int get unreadCount => _unreadCount;
  List<NotificationItem> get items => _items;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  String? get error => _error;
  NotificationFilterTab get activeTab => _activeTab;
  int get page => _page;
  int get total => _total;
  int get totalPages => _totalPages;
  bool get hasMore => _page < _totalPages;
  AppUser? get user => _user;

  int get actionRequiredCount => _items
      .where((item) => !item.isRead && item.priority == NotificationPriority.actionRequired)
      .length;

  void bindUser(AppUser? newUser) {
    if (_user?.id == newUser?.id && (_user != null) == (newUser != null)) {
      _user = newUser;
      return;
    }
    _user = newUser;
    _stopPolling();

    if (newUser != null) {
      _startPolling();
      unawaited(refreshUnreadCount());
      unawaited(load(reset: true));
    } else {
      _unreadCount = 0;
      _items = const [];
      _isLoading = false;
      _error = null;
      _activeTab = NotificationFilterTab.all;
      _notify();
    }
  }

  void _startPolling() {
    _stopPolling();
    _pollTimer = Timer.periodic(pollInterval, (_) {
      if (_user != null && !_disposed) {
        unawaited(refreshUnreadCount());
      }
    });
  }

  void _stopPolling() {
    _pollTimer?.cancel();
    _pollTimer = null;
  }

  Future<int> refreshUnreadCount() {
    if (_inFlightUnread != null) return _inFlightUnread!;
    final future = () async {
      try {
        final count = await gateway.unreadCount();
        if (!_disposed) {
          _unreadCount = count;
          _notify();
        }
        return count;
      } catch (_) {
        return _unreadCount;
      } finally {
        _inFlightUnread = null;
      }
    }();
    _inFlightUnread = future;
    return future;
  }

  void setFilterTab(NotificationFilterTab tab) {
    if (_activeTab == tab) return;
    _activeTab = tab;
    _notify();
    unawaited(load(reset: true));
  }

  NotificationQuery _buildQuery({required int queryPage}) {
    return NotificationQuery(
      page: queryPage,
      pageSize: _pageSize,
      unread: _activeTab == NotificationFilterTab.unread ? true : null,
      context: switch (_activeTab) {
        NotificationFilterTab.buyer => NotificationContext.buyer,
        NotificationFilterTab.seller => NotificationContext.seller,
        _ => null,
      },
      priority: _activeTab == NotificationFilterTab.actionRequired
          ? NotificationPriority.actionRequired
          : null,
    );
  }

  Future<void> load({bool reset = true}) async {
    if (_inFlightLoad != null) return _inFlightLoad!;
    final future = () async {
      if (reset) {
        _isLoading = true;
        _error = null;
        _page = 1;
        _notify();
      }
      try {
        final query = _buildQuery(queryPage: _page);
        final result = await gateway.list(query);
        if (!_disposed) {
          _items = result.items;
          _total = result.total;
          _totalPages = result.totalPages;
          _isLoading = false;
          _error = null;
          _notify();
        }
      } catch (err) {
        if (!_disposed) {
          _isLoading = false;
          _error = err.toString();
          _notify();
        }
      } finally {
        _inFlightLoad = null;
      }
    }();
    _inFlightLoad = future;
    return future;
  }

  Future<void> loadMore() async {
    if (_isLoading || _isLoadingMore || !hasMore) return;
    _isLoadingMore = true;
    _notify();

    try {
      final nextPage = _page + 1;
      final query = _buildQuery(queryPage: nextPage);
      final result = await gateway.list(query);
      if (!_disposed) {
        _page = nextPage;
        _items = [..._items, ...result.items];
        _total = result.total;
        _totalPages = result.totalPages;
        _isLoadingMore = false;
        _notify();
      }
    } catch (_) {
      if (!_disposed) {
        _isLoadingMore = false;
        _notify();
      }
    }
  }

  Future<void> markRead(NotificationItem item) async {
    if (item.isRead) return;
    try {
      final persisted = await gateway.markRead(item.id);
      if (!_disposed) {
        final index = _items.indexWhere((it) => it.id == item.id);
        if (index != -1) {
          final updatedList = List<NotificationItem>.from(_items);
          updatedList[index] = persisted;
          _items = updatedList;
          _notify();
        }
      }
      await refreshUnreadCount();
    } catch (_) {
      // Keep the screen unchanged when persistence fails, then refresh the
      // server-owned count in case another device changed it.
      unawaited(refreshUnreadCount());
    }
  }

  Future<void> markAllRead() async {
    if (_unreadCount == 0 && _items.every((it) => it.isRead)) return;
    try {
      await gateway.markAllRead();
      if (!_disposed) {
        final readAt = DateTime.now();
        _items = _items
            .map((it) => it.isRead ? it : it.copyWith(isRead: true, readAt: readAt))
            .toList();
        _notify();
      }
      await refreshUnreadCount();
    } catch (_) {
      unawaited(refreshUnreadCount());
      unawaited(load(reset: true));
    }
  }

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _stopPolling();
    _lifecycleListener.dispose();
    super.dispose();
  }
}

class NotificationScope extends InheritedNotifier<NotificationController> {
  const NotificationScope({
    required NotificationController controller,
    required super.child,
    super.key,
  }) : super(notifier: controller);

  static NotificationController? maybeOf(BuildContext context) => context
      .dependOnInheritedWidgetOfExactType<NotificationScope>()
      ?.notifier;

  static NotificationController of(BuildContext context) {
    final controller = maybeOf(context);
    assert(controller != null, 'No NotificationScope found in context');
    return controller!;
  }
}

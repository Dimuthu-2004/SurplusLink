import 'notification_models.dart';

String resolveNotificationRoute(NotificationItem notification) {
  // If specific action route was provided by backend
  if (notification.actionRoute != null && notification.actionRoute!.isNotEmpty) {
    final route = notification.actionRoute!;
    final cleaned = route.replaceAll('/app/', '/');
    if (cleaned.startsWith('/')) {
      return cleaned;
    }
  }

  final type = notification.type.toUpperCase();
  final entityType = notification.entityType;
  final entityId = notification.entityId;

  // 1. Matches Ready
  if (type == 'MATCHES_READY') {
    if (entityId != null && entityId.isNotEmpty) {
      return '/requirements/$entityId/matches';
    }
    return '/requirements';
  }

  // 2. Listing related (approved, awaiting, rejected)
  if (type == 'LISTING_APPROVED' ||
      type == 'LISTING_AWAITING_APPROVAL' ||
      type == 'LISTING_REJECTED' ||
      entityType == 'Listing') {
    if (entityId != null && entityId.isNotEmpty) {
      return '/materials/$entityId';
    }
    return '/materials';
  }

  // 3. Handover and Transactions (Scenario 1-8 notifications)
  if (type == 'HANDOVER_CONFIRMATION_REQUIRED' ||
      type == 'SELLER_CONFIRMED_HANDOVER' ||
      type == 'BUYER_CONFIRMATION_OVERDUE' ||
      type == 'NO_HANDOVER_CONFIRMATION_RECEIVED' ||
      type == 'TRANSACTION_COMPLETED' ||
      type == 'MANAGER_CONFIRMED_COMPLETION' ||
      type == 'TRANSACTION_NOT_COMPLETED' ||
      type == 'HANDOVER_ISSUE_REPORTED' ||
      type == 'RECEIPT_CONFIRMATION_REQUIRED' ||
      type.contains('HANDOVER') ||
      type.contains('TRANSACTION') ||
      entityType == 'Transaction' ||
      entityType == 'Offer') {
    return '/offers';
  }

  // 4. Requirements & Selections
  if (type == 'REQUIREMENT_SUBMITTED' ||
      type == 'NO_SUITABLE_MATCHES') {
    if (entityId != null && entityId.isNotEmpty) {
      return '/requirements/$entityId';
    }
    return '/requirements';
  }

  if (type == 'BUYER_SELECTION_AWAITING_APPROVAL' ||
      type == 'SELECTION_APPROVED' ||
      type == 'STOCK_RESERVED_FOR_BUYER' ||
      type == 'SELECTION_REJECTED' ||
      type == 'SELECTION_AVAILABILITY_CHANGED') {
    return '/offers';
  }

  if (type == 'SELECTION_REVISION_REQUESTED') {
    if (entityId != null && entityId.isNotEmpty) {
      return '/requirements/$entityId';
    }
    return '/offers';
  }

  // 5. Manager approvals (if any reaches mobile)
  if (type == 'APPROVAL_REQUIRED') {
    return '/home';
  }

  // Default fallback
  if (entityType == 'MaterialRequest') {
    if (entityId != null && entityId.isNotEmpty) {
      return '/requirements/$entityId';
    }
    return '/requirements';
  }

  return '/home';
}

String getNotificationActionLabel(NotificationItem notification) {
  final type = notification.type.toUpperCase();
  final title = notification.title.toLowerCase();

  if (type.contains('SELLER_CONFIRMED_HANDOVER') || title.contains('seller confirmed handover')) {
    return 'Confirm receipt';
  }
  if (type.contains('HANDOVER_CONFIRMATION_REQUIRED') || title.contains('handover confirmation required')) {
    return 'Confirm handover';
  }
  if (type.contains('MATCHES_READY') || title.contains('match')) {
    return 'View matches';
  }
  if (type.contains('LISTING_APPROVED') || title.contains('listing')) {
    return 'View listing';
  }
  if (type.contains('TRANSACTION_COMPLETED') || title.contains('completed')) {
    return 'View transaction';
  }
  if (type.contains('OVERDUE') || type.contains('NO_HANDOVER')) {
    return 'Review status';
  }
  if (type.contains('ISSUE') || title.contains('issue')) {
    return 'Review issue';
  }
  if (notification.priority == NotificationPriority.actionRequired) {
    return 'Take action';
  }
  return 'View details';
}

String formatRelativeTime(DateTime dateTime) {
  final now = DateTime.now();
  final difference = now.difference(dateTime.toLocal());

  if (difference.isNegative || difference.inSeconds < 60) {
    return 'Just now';
  }
  if (difference.inMinutes < 60) {
    final m = difference.inMinutes;
    return '$m min ago';
  }
  if (difference.inHours < 24) {
    final h = difference.inHours;
    return '$h hour${h == 1 ? '' : 's'} ago';
  }
  if (difference.inDays < 7) {
    final d = difference.inDays;
    return '$d day${d == 1 ? '' : 's'} ago';
  }
  return '${dateTime.month}/${dateTime.day}/${dateTime.year}';
}

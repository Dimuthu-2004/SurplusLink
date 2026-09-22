import 'match_models.dart';

String readableRejectionReason(String? code) => switch (code) {
  'LISTING_EXPIRES_BEFORE_DELIVERY' =>
    'This material listing expires before your required delivery date.',
  'INSUFFICIENT_QUANTITY' =>
    'The seller does not have enough available quantity.',
  'BUDGET_EXCEEDED' => 'The material cost exceeds your maximum budget.',
  'TOTAL_COST_EXCEEDS_BUDGET' => 'The total cost exceeds your maximum budget.',
  'CATEGORY_MISMATCH' => 'This material does not match your required category.',
  'UNIT_MISMATCH' =>
    'The material unit is not compatible with your requirement.',
  'SELF_MATCH_NOT_ALLOWED' => 'You cannot match your own material listing.',
  'LISTING_INACTIVE' ||
  'LISTING_NOT_ACTIVE' => 'This material listing is no longer active.',
  'LISTING_EXPIRED' => 'This material listing has expired.',
  'ROUTE_UNAVAILABLE' => 'Delivery route information is currently unavailable.',
  'DELIVERY_DEADLINE_EXCEEDED' =>
    'The estimated delivery time exceeds your required deadline.',
  'TRANSPORT_OVER_BUDGET' => 'The transport cost exceeds your budget.',
  _ => 'Unable to use this match.',
};

String formatCurrency(num? value, {bool decimal = true}) {
  if (value == null || !value.isFinite) return 'Not available';
  final parts = value.toStringAsFixed(decimal ? 2 : 0).split('.');
  final whole = parts.first.replaceAllMapped(
    RegExp(r'(\d)(?=(\d{3})+(?!\d))'),
    (m) => '${m[1]},',
  );
  return 'LKR $whole${decimal ? '.${parts.last}' : ''}';
}

String formatQuantity(num? value) {
  if (value == null || !value.isFinite) return 'Not available';
  return value == value.roundToDouble()
      ? value.toStringAsFixed(0)
      : value.toString();
}

String formatUnit(String? value) => switch (value?.trim().toLowerCase()) {
  'pcs' => 'Pcs',
  'kg' => 'kg',
  'm2' => 'm²',
  'pkts' => 'pkts',
  'l' => 'L',
  'tons' => 'Tons',
  _ => value ?? '',
};

const _months = [
  'Jan',
  'Feb',
  'Mar',
  'Apr',
  'May',
  'Jun',
  'Jul',
  'Aug',
  'Sep',
  'Oct',
  'Nov',
  'Dec',
];
// Date-only availability/deadline values retain the supplied calendar date.
String formatMatchDate(DateTime? value) => value == null
    ? 'Not available'
    : '${value.day} ${_months[value.month - 1]} ${value.year}';
String formatMatchDateTime(DateTime? value) {
  if (value == null) return 'Not available';
  final local = value.toLocal();
  final hour = local.hour % 12 == 0 ? 12 : local.hour % 12;
  return '${formatMatchDate(local)}, $hour:${local.minute.toString().padLeft(2, '0')} ${local.hour < 12 ? 'AM' : 'PM'}';
}

String readableHistoryAction(String action, {String? outcome}) =>
    switch (action.toUpperCase()) {
      'GENERATE' => 'Match candidate created',
      'RANK' => 'Match ranked',
      'ROUTE' =>
        outcome?.toUpperCase() == 'FAILED'
            ? 'Delivery route failed'
            : 'Delivery route evaluated',
      'ROUTE_SUCCEEDED' => 'Delivery route evaluated',
      'ROUTE_FAILED' => 'Delivery route failed',
      'REJECT' => 'Match rejected',
      'VALIDATE' => 'Match validated',
      _ => 'Match updated',
    };
String? historyActionExplanation(String action, {String? outcome}) =>
    action.toUpperCase() == 'ROUTE_FAILED' ||
        (action.toUpperCase() == 'ROUTE' && outcome?.toUpperCase() == 'FAILED')
    ? 'Delivery route could not be calculated. Please try again later.'
    : null;

String readableMatchStatus(String status) => switch (status) {
  'GENERATED' => 'Candidate created',
  'RANKED' => 'Ranked',
  'ROUTED' => 'Delivery route evaluated',
  'ROUTE_FAILED' => 'Delivery route failed',
  'REJECTED' => 'Rejected',
  _ => 'Evaluation status unavailable',
};

enum RoutingUiState { notEvaluated, inProgress, available, failed }

RoutingUiState getRoutingState(RecommendedMatch match) {
  if (match.status == 'ROUTE_FAILED' ||
      match.rejectionReason == 'ROUTE_UNAVAILABLE') {
    return RoutingUiState.failed;
  }
  // The API has no persisted in-progress route state. Rejected matches may
  // retain a successful route (for example, after a total-cost rejection).
  if (match.status == 'ROUTED' ||
      match.distance != null ||
      match.durationMinutes != null) {
    return RoutingUiState.available;
  }
  return RoutingUiState.notEvaluated;
}

String routingSummary(RecommendedMatch match) => switch (getRoutingState(
  match,
)) {
  RoutingUiState.notEvaluated => 'Delivery route has not been evaluated yet.',
  RoutingUiState.inProgress => 'Calculating delivery route...',
  RoutingUiState.failed =>
    'Delivery route could not be calculated. Please try again later.',
  RoutingUiState.available => [
    if (match.distance != null) '${formatQuantity(match.distance)} km',
    if (match.durationMinutes != null)
      '${formatQuantity(match.durationMinutes)} min',
    if (match.distance == null && match.durationMinutes == null)
      'Delivery route evaluated',
  ].join(' · '),
};

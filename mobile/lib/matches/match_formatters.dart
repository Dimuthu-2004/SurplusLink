import 'package:flutter/widgets.dart';
import 'match_models.dart';

String localizedRoutingText(BuildContext context, String key) {
  final language = Localizations.localeOf(context).languageCode;
  const values = {
    'si': {
      'retryRoute': 'මාර්ගය නැවත උත්සාහ කරන්න',
      'cannotSelect': 'මෙම ගැළපීම තෝරාගත නොහැක.',
      'MISSING_SELLER_LOCATION': 'විකුණුම්කරුගේ ලැයිස්තු ස්ථානය නොමැත.',
      'MISSING_BUYER_LOCATION': 'ගැනුම්කරුගේ බෙදාහැරීමේ ස්ථානය නොමැත.',
      'INVALID_SELLER_COORDINATES': 'විකුණුම්කරුගේ ස්ථාන දත්ත වලංගු නොවේ.',
      'INVALID_BUYER_COORDINATES': 'ගැනුම්කරුගේ ස්ථාන දත්ත වලංගු නොවේ.',
      'ROUTING_TIMEOUT': 'මාර්ග සේවාව ප්‍රතිචාර දැක්වීමට වැඩි කාලයක් ගත්තේය.',
      'NO_ROUTE_FOUND': 'මෙම ස්ථාන අතර මාර්ගයක් හමු නොවීය.',
      'ROUTING_PROVIDER_ERROR': 'මාර්ග තොරතුරු දැනට ලබාගත නොහැක.',
      'ROUTE_UNAVAILABLE': 'මාර්ග තොරතුරු දැනට ලබාගත නොහැක.',
      'MALFORMED_ROUTING_RESPONSE': 'මාර්ග සේවාව වලංගු ප්‍රතිචාරයක් ලබා දුන්නේ නැත.',
    },
    'ta': {
      'retryRoute': 'வழியை மீண்டும் முயற்சிக்கவும்',
      'cannotSelect': 'இந்த பொருத்தத்தை தற்போது தேர்ந்தெடுக்க முடியாது.',
      'MISSING_SELLER_LOCATION': 'விற்பனையாளர் பட்டியல் இடம் இல்லை.',
      'MISSING_BUYER_LOCATION': 'வாங்குபவர் விநியோக இடம் இல்லை.',
      'INVALID_SELLER_COORDINATES': 'விற்பனையாளர் இடத் தரவு செல்லுபடியாகாது.',
      'INVALID_BUYER_COORDINATES': 'வாங்குபவர் இடத் தரவு செல்லுபடியாகாது.',
      'ROUTING_TIMEOUT': 'வழிசெலுத்தல் சேவை பதிலளிக்க அதிக நேரம் எடுத்தது.',
      'NO_ROUTE_FOUND': 'இந்த இடங்களுக்கு இடையில் வழி கிடைக்கவில்லை.',
      'ROUTING_PROVIDER_ERROR': 'வழித் தகவல் தற்போது கிடைக்கவில்லை.',
      'ROUTE_UNAVAILABLE': 'வழித் தகவல் தற்போது கிடைக்கவில்லை.',
      'MALFORMED_ROUTING_RESPONSE': 'வழிசெலுத்தல் சேவை செல்லுபடியாகும் பதிலை வழங்கவில்லை.',
    },
  };
  const english = {
    'retryRoute': 'Retry route', 'cannotSelect': 'This match cannot currently be selected.',
    'MISSING_SELLER_LOCATION': 'Seller listing location is missing.',
    'MISSING_BUYER_LOCATION': 'Buyer delivery location is missing.',
    'INVALID_SELLER_COORDINATES': 'Seller listing coordinates are invalid.',
    'INVALID_BUYER_COORDINATES': 'Buyer delivery coordinates are invalid.',
    'ROUTING_TIMEOUT': 'The routing service timed out.', 'NO_ROUTE_FOUND': 'No route was found between these locations.',
    'ROUTING_PROVIDER_ERROR': 'Route information is currently unavailable.',
    'ROUTE_UNAVAILABLE': 'Route information is currently unavailable.',
    'MALFORMED_ROUTING_RESPONSE': 'The routing service returned an invalid response.',
  };
  return values[language]?[key] ?? english[key] ?? readableRejectionReason(key);
}

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

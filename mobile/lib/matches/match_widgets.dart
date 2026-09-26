import 'package:flutter/material.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/quantity_format.dart' as quantities;
import 'package:mobile/matches/match_formatters.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

String matchError(Object error) => switch (error) {
  ApiException(statusCode: 403) => 'You do not have access to these matches.',
  ApiException(statusCode: 404) =>
    'These matches are not available. Return to your requirements or retry.',
  ApiException(statusCode: 401) =>
    'Your session has expired. Please sign in again.',
  ApiException(statusCode: 429) => 'Too many requests. Please wait and retry.',
  ApiException(:final message) => message,
  FormatException() => 'The server returned invalid match data. Please retry.',
  _ => 'Unable to load matches. Please retry.',
};

String matchDetailsError(Object error) {
  if (error is ApiException) {
    if (error.statusCode == 403) return 'You do not have access to this match.';
    if (error.statusCode == 404) return 'Match not found.';
    if (error.statusCode == 401) {
      return 'Your session has expired. Please sign in again.';
    }
    if (error.statusCode == 429) {
      return 'Too many requests. Please wait and retry.';
    }
    return 'Unable to load match details.';
  }
  if (error is FormatException) {
    return 'The server returned invalid match data. Please retry.';
  }
  return 'Unable to load match details.';
}

class MatchErrorBox extends StatelessWidget {
  const MatchErrorBox(this.message, {required this.onRetry, super.key});
  final String message;
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      Text(message, textAlign: TextAlign.center),
      OutlinedButton(onPressed: onRetry, child: const Text('Retry')),
    ],
  );
}

class MatchPagination extends StatelessWidget {
  const MatchPagination({
    required this.page,
    required this.totalPages,
    required this.busy,
    required this.onPage,
    super.key,
  });
  final int page, totalPages;
  final bool busy;
  final ValueChanged<int> onPage;
  @override
  Widget build(BuildContext context) => Wrap(
    alignment: WrapAlignment.center,
    crossAxisAlignment: WrapCrossAlignment.center,
    spacing: 12,
    children: [
      TextButton(
        onPressed: !busy && page > 1 ? () => onPage(page - 1) : null,
        child: const Text('Previous'),
      ),
      Text('Page $page of ${totalPages == 0 ? 1 : totalPages}'),
      TextButton(
        onPressed: !busy && page < totalPages ? () => onPage(page + 1) : null,
        child: const Text('Next'),
      ),
    ],
  );
}

// ---------------------------------------------------------------------------
// Compact match list card
// ---------------------------------------------------------------------------

/// Compact card shown in the Recommended Matches list.
///
/// Shows ONLY essential summary info: AI Recommended badge, score %, material name,
/// seller/business name, location short label, distance ("27.5 km away"),
/// status chip (Valid / Rejected / Route failed), and chevron affordance.
/// Full details are available in Match Details.
class MatchListCard extends StatelessWidget {
  const MatchListCard({
    required this.match,
    required this.onTap,
    this.isSelected = false,
    this.onToggleSelect,
    this.selectedQuantity,
    this.onQuantityChanged,
    this.maximumQuantity,
    this.onRemove,
    this.quantityError,
    super.key,
  });

  final RecommendedMatch match;
  final VoidCallback onTap;
  final bool isSelected;
  final ValueChanged<bool>? onToggleSelect;
  final double? selectedQuantity;
  final ValueChanged<double>? onQuantityChanged;
  final double? maximumQuantity;
  final VoidCallback? onRemove;
  final String? quantityError;

  @override
  Widget build(BuildContext context) {
    final isRejected = match.isRejected;
    final isRouteFailed = match.status == 'ROUTE_FAILED';
    final isFailed = isRejected || isRouteFailed;

    // Location short label: prefer address, then coordinates
    final locationLabel = match.sellerAddress != null && match.sellerAddress!.trim().isNotEmpty
        ? match.sellerAddress!.trim()
        : (match.latitude != null && match.longitude != null
              ? '${match.latitude!.toStringAsFixed(3)}, ${match.longitude!.toStringAsFixed(3)}'
              : null);

    // Distance: max 1 decimal, e.g. "27.5 km away"
    final distanceLabel = match.distance != null
        ? '${match.distance!.toStringAsFixed(1)} km away'
        : null;

    // Score: max 1 decimal
    final scoreLabel = '${(match.score * 100).toStringAsFixed(1)}%';

    // Status chip data
    final (statusText, statusColor, statusIcon) = _statusData(
      context,
      match,
      isFailed,
      isRouteFailed,
    );

    // Seller display name
    final sellerLabel =
        match.sellerBusinessName ?? match.sellerName ?? match.sellerId;

    return RecommendedMatchCard(
      recommended: match.aiRecommended,
      child: InkWell(
        key: Key('match-${match.id}'),
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top row: AI badge (if applicable) + score % + chevron
              Row(
                children: [
                  if (match.aiRecommended) ...[
                    Chip(
                      key: const Key('ai-recommended-badge'),
                      avatar: const Icon(
                        Icons.auto_awesome,
                        size: 14,
                        color: SurplusLinkTheme.amber,
                      ),
                      label: const Text(
                        'AI Recommended',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                          color: SurplusLinkTheme.slate900,
                        ),
                      ),
                      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      visualDensity: VisualDensity.compact,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 4,
                        vertical: 0,
                      ),
                      backgroundColor: SurplusLinkTheme.amber.withValues(alpha: 0.15),
                      side: BorderSide(
                        color: SurplusLinkTheme.amber.withValues(alpha: 0.4),
                      ),
                    ),
                    const SizedBox(width: 8),
                  ],
                  Text(
                    scoreLabel,
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                      color: SurplusLinkTheme.amberDark,
                    ),
                  ),
                  const Spacer(),
                  const Icon(
                    Icons.chevron_right,
                    size: 20,
                    color: SurplusLinkTheme.slate600,
                  ),
                ],
              ),
              const SizedBox(height: 4),
              // Material name
              Text(
                match.materialTitle ?? 'Material not recorded',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                  color: SurplusLinkTheme.slate900,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              // Seller / Business name
              if (sellerLabel != null) ...[
                const SizedBox(height: 2),
                Text(
                  sellerLabel,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: SurplusLinkTheme.slate700,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
              const SizedBox(height: 4),
              // Location + Distance row
              Wrap(
                spacing: 12,
                runSpacing: 2,
                children: [
                  if (locationLabel != null)
                    _iconLabel(
                      context,
                      Icons.location_on_outlined,
                      locationLabel,
                    ),
                  if (distanceLabel != null)
                    _iconLabel(
                      context,
                      Icons.near_me_outlined,
                      distanceLabel,
                    ),
                ],
              ),
              const SizedBox(height: 6),
              // Status chip (Valid / Rejected / Route failed)
              _StatusChip(
                label: statusText,
                color: statusColor,
                icon: statusIcon,
              ),
              if (match.isPartial && !match.isRejected) ...[
                const SizedBox(height: 6),
                Container(
                  key: Key('partial-warning-${match.id}'),
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEF3C7),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: const Color(0xFFF59E0B)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.info_outline, size: 16, color: Color(0xFFB45309)),
                      const SizedBox(width: 6),
                      Expanded(
                        child: Text(
                          match.partialWarning(),
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF92400E),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              if (onToggleSelect != null && match.isSelectable) ...[
                const SizedBox(height: 8),
                const Divider(height: 1),
                const SizedBox(height: 4),
                GestureDetector(
                  behavior: HitTestBehavior.opaque,
                  onTap: () {},
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Checkbox(
                            key: Key('select-checkbox-${match.id}'),
                            value: isSelected,
                            onChanged: (val) => onToggleSelect!(val ?? false),
                          ),
                          Expanded(
                            child: InkWell(
                              onTap: () => onToggleSelect!(!isSelected),
                              child: Text(
                                isSelected ? 'Selected for fulfillment' : 'Select this seller',
                                style: TextStyle(
                                  fontWeight:
                                      isSelected ? FontWeight.bold : FontWeight.normal,
                                  color: isSelected
                                      ? SurplusLinkTheme.amberDark
                                      : SurplusLinkTheme.slate900,
                                ),
                              ),
                            ),
                          ),
                          TextButton.icon(
                            key: Key('view-details-${match.id}'),
                            onPressed: onTap,
                            icon: const Icon(Icons.info_outline, size: 16),
                            label: const Text('Details'),
                          ),
                        ],
                      ),
                      if (isSelected)
                        Container(
                          key: Key('allocation-panel-${match.id}'),
                          margin: const EdgeInsets.only(top: 4, bottom: 4),
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: SurplusLinkTheme.surfaceSoft,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: quantityError != null
                                  ? Theme.of(context).colorScheme.error
                                  : SurplusLinkTheme.slate300,
                            ),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(
                                      'Quantity to take (${match.unit ?? ''}):',
                                      style: const TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ),
                                  Text(
                                    'Avail: ${quantities.formatQuantity(match.availableQuantity ?? 0, match.unit ?? '')} ${match.unit ?? ''}',
                                    style: const TextStyle(
                                      fontSize: 11,
                                      color: SurplusLinkTheme.slate600,
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 6),
                              Row(
                                children: [
                                  IconButton(
                                    key: Key('qty-decrement-${match.id}'),
                                    icon: const Icon(Icons.remove_circle_outline, size: 20),
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(
                                      minWidth: 32,
                                      minHeight: 32,
                                    ),
                                    onPressed: (selectedQuantity ?? 1) > (quantities.isDiscreteUnit(match.unit ?? '') ? 1 : 0.1)
                                        ? () => onQuantityChanged!(
                                            (selectedQuantity ?? 1) - (quantities.isDiscreteUnit(match.unit ?? '') ? 1 : 0.1),
                                          )
                                        : null,
                                  ),
                                  SizedBox(
                                    width: 90,
                                    child: TextFormField(
                                      key: Key('quantity-input-${match.id}'),
                                      initialValue: quantities.formatQuantity(
                                        selectedQuantity ?? 0,
                                        match.unit ?? '',
                                      ),
                                      keyboardType: const TextInputType.numberWithOptions(
                                        decimal: true,
                                      ),
                                      textAlign: TextAlign.center,
                                      style: const TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.w600,
                                      ),
                                      decoration: InputDecoration(
                                        contentPadding: const EdgeInsets.symmetric(
                                          horizontal: 6,
                                          vertical: 6,
                                        ),
                                        isDense: true,
                                        border: OutlineInputBorder(
                                          borderRadius: BorderRadius.circular(6),
                                        ),
                                      ),
                                      onChanged: (text) {
                                        final parsed = double.tryParse(text.trim());
                                        if (parsed != null) {
                                          onQuantityChanged!(parsed);
                                        }
                                      },
                                    ),
                                  ),
                                  IconButton(
                                    key: Key('qty-increment-${match.id}'),
                                    icon: const Icon(Icons.add_circle_outline, size: 20),
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(
                                      minWidth: 32,
                                      minHeight: 32,
                                    ),
                                    onPressed: ((selectedQuantity ?? 0) <
                                            (maximumQuantity ?? match.availableQuantity ?? double.infinity))
                                        ? () => onQuantityChanged!(
                                            ((selectedQuantity ?? 0) + (quantities.isDiscreteUnit(match.unit ?? '') ? 1 : 0.1))
                                                .clamp(0.0, maximumQuantity ?? match.availableQuantity ?? double.infinity)
                                                .toDouble(),
                                          )
                                        : null,
                                  ),
                                  const Spacer(),
                                  TextButton(
                                    key: Key('qty-max-${match.id}'),
                                    onPressed: () => onQuantityChanged!(
                                      maximumQuantity ?? match.availableQuantity ?? 0,
                                    ),
                                    child: const Text('Max', style: TextStyle(fontSize: 12)),
                                  ),
                                  TextButton(
                                    key: Key('qty-remove-${match.id}'),
                                    onPressed: onRemove,
                                    child: const Text('Remove', style: TextStyle(fontSize: 12)),
                                  ),
                                ],
                              ),
                              if (quantityError != null)
                                Padding(
                                  padding: const EdgeInsets.only(top: 4),
                                  child: Text(
                                    quantityError!,
                                    style: TextStyle(
                                      fontSize: 11,
                                      color: Theme.of(context).colorScheme.error,
                                    ),
                                  ),
                                ),
                              if (selectedQuantity != null && match.unitPrice != null) ...[
                                const SizedBox(height: 4),
                                Text(
                                  'Est: ${formatCurrency((selectedQuantity ?? 0) * (match.unitPrice ?? 0))} + ${formatCurrency(match.estimatedTransportCost ?? 0)} transport',
                                  style: const TextStyle(
                                    fontSize: 11,
                                    color: SurplusLinkTheme.slate600,
                                  ),
                                ),
                              ],
                              Text(
                                'Seller available: ${quantities.formatQuantity(match.availableQuantity ?? 0, match.unit ?? '')} ${match.unit ?? ''} · Allocated: ${quantities.formatQuantity(selectedQuantity ?? 0, match.unit ?? '')} ${match.unit ?? ''}',
                                style: const TextStyle(fontSize: 11, color: SurplusLinkTheme.slate600),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  static (String, Color, IconData) _statusData(
    BuildContext context,
    RecommendedMatch match,
    bool isFailed,
    bool isRouteFailed,
  ) {
    if (isRouteFailed) {
      return (
        'Route failed',
        Theme.of(context).colorScheme.error,
        Icons.alt_route,
      );
    }
    if (match.isRejected) {
      return (
        'Rejected',
        Theme.of(context).colorScheme.error,
        Icons.cancel_outlined,
      );
    }
    if (match.isSelectable) {
      return ('Valid', const Color(0xFF15803D), Icons.check_circle_outline);
    }
    return (
      readableMatchStatus(match.status),
      SurplusLinkTheme.slate600,
      Icons.hourglass_empty,
    );
  }

  Widget _iconLabel(
    BuildContext context,
    IconData icon,
    String text,
  ) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Icon(icon, size: 14, color: SurplusLinkTheme.slate600),
      const SizedBox(width: 3),
      Flexible(
        child: Text(
          text,
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: SurplusLinkTheme.slate600),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
      ),
    ],
  );
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({
    required this.label,
    required this.color,
    required this.icon,
  });
  final String label;
  final Color color;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
    decoration: BoxDecoration(
      color: color.withValues(alpha: 0.1),
      borderRadius: BorderRadius.circular(99),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: color),
        const SizedBox(width: 4),
        Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w700,
            color: color,
          ),
        ),
      ],
    ),
  );
}

// ---------------------------------------------------------------------------
// Animated recommended-card wrapper
// ---------------------------------------------------------------------------

/// Wraps a match card with an animated neon/glow border when [recommended].
///
/// Exactly one card per list can be recommended per recommendation ID.
/// Uses SurplusLink navy + orange with a subtle secondary glow.
class RecommendedMatchCard extends StatefulWidget {
  const RecommendedMatchCard({
    required this.recommended,
    required this.child,
    super.key,
  });
  final bool recommended;
  final Widget child;

  @override
  State<RecommendedMatchCard> createState() => _RecommendedMatchCardState();
}

class _RecommendedMatchCardState extends State<RecommendedMatchCard>
    with SingleTickerProviderStateMixin {
  late final AnimationController _glow = AnimationController(
    vsync: this,
    duration: const Duration(seconds: 3),
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _updateAnimation();
  }

  @override
  void didUpdateWidget(RecommendedMatchCard oldWidget) {
    super.didUpdateWidget(oldWidget);
    _updateAnimation();
  }

  void _updateAnimation() {
    if (widget.recommended && !MediaQuery.disableAnimationsOf(context)) {
      if (!_glow.isAnimating) _glow.repeat(reverse: true);
    } else {
      _glow.stop();
      _glow.value = 0;
    }
  }

  @override
  void dispose() {
    _glow.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: _glow,
    builder: (context, child) {
      if (!widget.recommended) {
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          elevation: 1,
          color: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
            side: const BorderSide(color: Color(0xFFE2E8F0)),
          ),
          clipBehavior: Clip.antiAlias,
          child: child,
        );
      }

      // Animated glow progress [0..1]
      final t = Curves.easeInOut.transform(_glow.value);

      // SurplusLink navy + orange glowing colors
      const orangeGlow = SurplusLinkTheme.amber;
      const navyGlow = SurplusLinkTheme.slate900;

      final borderOpacity = 0.55 + 0.35 * t;
      final shadowOpacity = 0.12 + 0.12 * t;
      final blurRadius = 8.0 + 6.0 * t;

      return Card(
        key: const Key('ai-recommended-card'),
        margin: const EdgeInsets.only(bottom: 12),
        clipBehavior: Clip.antiAlias,
        elevation: 2.0 + 3.0 * t,
        shadowColor: orangeGlow.withValues(alpha: shadowOpacity),
        // Animating shape border color/width satisfies unit tests asserting shape changes
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(
            color: orangeGlow.withValues(alpha: borderOpacity),
            width: 1.5 + 0.5 * t,
          ),
        ),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: Colors.white,
            // Subtle moving gradient light effect along the card background
            gradient: LinearGradient(
              begin: Alignment(-1.0 + 0.6 * t, -0.5),
              end: Alignment(1.0 - 0.6 * t, 0.5),
              colors: [
                navyGlow.withValues(alpha: 0.03),
                orangeGlow.withValues(alpha: 0.05 + 0.03 * t),
                navyGlow.withValues(alpha: 0.02),
              ],
            ),
          ),
          child: DecoratedBox(
            position: DecorationPosition.foreground,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(12),
              boxShadow: [
                BoxShadow(
                  color: orangeGlow.withValues(alpha: shadowOpacity),
                  blurRadius: blurRadius,
                  spreadRadius: 0,
                ),
              ],
            ),
            child: child,
          ),
        ),
      );
    },
    child: widget.child,
  );
}

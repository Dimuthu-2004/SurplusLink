import 'package:flutter/material.dart';
import 'package:mobile/matches/match_formatters.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_widgets.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';

class MatchDetailsScreen extends StatefulWidget {
  const MatchDetailsScreen({
    required this.gateway,
    required this.requirementId,
    required this.matchId,
    super.key,
  });
  final MatchGateway gateway;
  final String requirementId, matchId;
  @override
  State<MatchDetailsScreen> createState() => _MatchDetailsScreenState();
}

class _MatchDetailsScreenState extends State<MatchDetailsScreen> {
  RecommendedMatch? _match;
  MatchPage<MatchHistoryEntry>? _history;
  String? _error, _historyError;
  bool _loading = true, _historyLoading = false;
  int _historyPage = 1;
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(MatchDetailsScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.matchId != widget.matchId ||
        oldWidget.requirementId != widget.requirementId ||
        oldWidget.gateway != widget.gateway) {
      _load();
    }
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final match = await widget.gateway.get(
        widget.requirementId,
        widget.matchId,
      );
      if (!mounted) return;
      setState(() {
        _match = match;
        _loading = false;
      });
      await _loadHistory(1);
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _error = matchDetailsError(error);
          _loading = false;
        });
      }
    }
  }

  Future<void> _loadHistory(int page) async {
    setState(() {
      _historyLoading = true;
      _historyError = null;
      _historyPage = page;
    });
    try {
      final history = await widget.gateway.history(widget.matchId, page: page);
      if (mounted) setState(() => _history = history);
    } on Object catch (error) {
      if (mounted) setState(() => _historyError = matchError(error));
    } finally {
      if (mounted) setState(() => _historyLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Match Details'),
      leading: DashboardBackButton(
        fallback: '/requirements/${widget.requirementId}/matches',
      ),
      actions: [
        IconButton(
          tooltip: 'Refresh match',
          onPressed: _loading || _historyLoading ? null : _load,
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    body: _loading
        ? const Center(child: CircularProgressIndicator())
        : ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (_error != null)
                MatchErrorBox(_error!, onRetry: _load)
              else if (_match case final match?) ...[
                _card('Material Summary', [
                  Text(
                    match.materialTitle ?? 'Material recommendation',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  if (match.categoryName != null)
                    Text('Category: ${match.categoryName}'),
                  Text(
                    'Required quantity: ${formatQuantity(match.quantity)} ${formatUnit(match.unit)}',
                  ),
                  Text('Unit price: ${formatCurrency(match.unitPrice)}'),
                  Text(
                    'Estimated material cost: ${formatCurrency(match.estimatedMaterialCost)}',
                  ),
                ]),
                _card('Match Status', [
                  Text('Score ${(match.score * 100).toStringAsFixed(1)}%'),
                  Text('Status: ${readableMatchStatus(match.status)}'),
                  if (match.isRejected) ...[
                    const Text(
                      'Why this match was rejected',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    Text(readableRejectionReason(match.rejectionReason)),
                    if (match.rejectionReason ==
                            'LISTING_EXPIRES_BEFORE_DELIVERY' &&
                        match.availableUntil != null &&
                        match.requiredBy != null) ...[
                      Text(
                        'Available until: ${formatMatchDate(match.availableUntil)}',
                      ),
                      Text('Required by: ${formatMatchDate(match.requiredBy)}'),
                    ],
                  ],
                  if (match.requirementStatus == 'PENDING_APPROVAL')
                    const Text('Waiting for manager approval'),
                  if (match.requirementStatus == 'MATCHING')
                    const Text('Matching and evaluation in progress.'),
                  if (match.requirementStatus == 'APPROVED')
                    const Text('Requirement approved.'),
                ]),
                _card('Match Evaluation', [
                  const Text('Quantity'),
                  Text(
                    match.availableQuantity != null && match.quantity != null
                        ? '${formatQuantity(match.availableQuantity)} available / ${formatQuantity(match.quantity)} required'
                        : '${formatQuantity(match.quantity)} ${formatUnit(match.unit)} required',
                  ),
                  const Text('Budget'),
                  Text(
                    match.estimatedMaterialCost != null &&
                            match.maximumBudget != null
                        ? '${formatCurrency(match.estimatedMaterialCost)} / ${formatCurrency(match.maximumBudget)}'
                        : 'Not available',
                  ),
                  const Text('Availability'),
                  if (match.availableUntil != null)
                    Text(
                      'Available until ${formatMatchDate(match.availableUntil)}',
                    ),
                  if (match.requiredBy != null)
                    Text('Required by ${formatMatchDate(match.requiredBy)}'),
                  if (match.availableUntil == null && match.requiredBy == null)
                    const Text('Not available'),
                ]),
                _card('Delivery', [
                  Text(routingSummary(match)),
                  if (getRoutingState(match) == RoutingUiState.available) ...[
                    const Text('Transport estimate'),
                    Text(formatCurrency(match.estimatedTransportCost)),
                  ],
                ]),
                ExpansionTile(
                  key: const Key('technical-details-tile'),
                  title: const Text('Technical details'),
                  expandedCrossAxisAlignment: CrossAxisAlignment.start,
                  childrenPadding: const EdgeInsets.all(16),
                  children: [
                    SelectableText('Match ID: ${match.id}'),
                    SelectableText('Material listing: ${match.listingId}'),
                    SelectableText('Requirement ID: ${match.requirementId}'),
                    if (match.sellerId != null)
                      SelectableText('Seller ID: ${match.sellerId}'),
                    SelectableText('Status: ${match.status}'),
                    if (match.rejectionReason != null)
                      SelectableText('Reason code: ${match.rejectionReason}'),
                    Text('Created: ${formatMatchDateTime(match.createdAt)}'),
                  ],
                ),
                const SizedBox(height: 16),
                const Text(
                  'Read-only recommendation. This screen does not approve matches or reserve materials.',
                ),
                const SizedBox(height: 16),
                Text(
                  'Match history',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                _buildHistory(),
              ],
            ],
          ),
  );

  Widget _card(String title, List<Widget> children) => Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          ...children,
        ],
      ),
    ),
  );

  Widget _buildHistory() {
    if (_historyLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_historyError != null) {
      return MatchErrorBox(
        _historyError!,
        onRetry: () => _loadHistory(_historyPage),
      );
    }
    final history = _history;
    if (history == null) return const SizedBox.shrink();
    return Column(
      children: [
        if (history.items.isEmpty) const Text('No match history yet.'),
        for (final entry in history.items)
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(
              readableHistoryAction(entry.action, outcome: entry.outcome),
            ),
            subtitle: Text(
              [
                formatMatchDateTime(entry.createdAt),
                ?historyActionExplanation(entry.action, outcome: entry.outcome),
              ].join('\n'),
            ),
          ),
        MatchPagination(
          page: history.page,
          totalPages: history.totalPages,
          busy: _historyLoading,
          onPage: _loadHistory,
        ),
      ],
    );
  }
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/materials/quantity_format.dart' as quantities;
import 'package:mobile/matches/match_formatters.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_widgets.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';

class RecommendedMatchesScreen extends StatefulWidget {
  const RecommendedMatchesScreen({
    required this.gateway,
    required this.requirementId,
    super.key,
  });
  final MatchGateway gateway;
  final String requirementId;
  @override
  State<RecommendedMatchesScreen> createState() =>
      _RecommendedMatchesScreenState();
}

class _RecommendedMatchesScreenState extends State<RecommendedMatchesScreen> {
  MatchPage<RecommendedMatch>? _data;
  bool _loading = true, _selecting = false;
  String? _error, _status;
  String _eligibility = 'all', _sort = 'score', _direction = 'desc';
  int _page = 1, _request = 0;

  final Map<String, double> _selectedQuantities = {};
  final Map<String, RecommendedMatch> _selectedMatches = {};
  bool _submittingMultiMatch = false;
  String? _multiMatchError;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({int? page}) async {
    final request = ++_request;
    setState(() {
      _loading = true;
      _error = null;
      _page = page ?? _page;
    });
    try {
      final data = await widget.gateway.list(
        widget.requirementId,
        MatchQuery(
          valid: _eligibility == 'valid' ? true : null,
          rejected: _eligibility == 'rejected' ? true : null,
          status: _status,
          sortBy: _sort,
          sortDir: _direction,
          page: _page,
        ),
      );
      if (mounted && request == _request) setState(() => _data = data);
    } on Object catch (error) {
      if (mounted && request == _request) {
        setState(() => _error = matchError(error));
      }
    } finally {
      if (mounted && request == _request) setState(() => _loading = false);
    }
  }

  Future<void> _changeSelection() async {
    if (_selecting) return;
    setState(() {
      _selecting = true;
      _error = null;
      _selectedQuantities.clear();
      _selectedMatches.clear();
    });
    try {
      await widget.gateway.cancelPendingApproval(widget.requirementId);
      await _load(page: 1);
    } on Object catch (error) {
      if (mounted) setState(() => _error = matchError(error));
    } finally {
      if (mounted) setState(() => _selecting = false);
    }
  }

  void _toggleSelectMatch(
    RecommendedMatch match,
    bool selected,
    double requestedQuantity,
  ) {
    setState(() {
      if (selected) {
        _selectedMatches[match.id] = match;
        final currentTotal =
            _selectedQuantities.values.fold(0.0, (s, q) => s + q);
        final remainingNeeded = requestedQuantity - currentTotal;
        final avail = match.availableQuantity ?? requestedQuantity;
        final defaultQty = (remainingNeeded > 0 && remainingNeeded <= avail)
            ? remainingNeeded
            : (avail > 0 ? avail : 1.0);
        _selectedQuantities[match.id] = defaultQty > 0 ? defaultQty : 1.0;
      } else {
        _selectedQuantities.remove(match.id);
        _selectedMatches.remove(match.id);
      }
    });
  }

  void _updateMatchQuantity(String matchId, double quantity) {
    setState(() {
      _selectedQuantities[matchId] = quantity;
    });
  }

  double _maximumFor(RecommendedMatch match, double requestedQuantity) {
    final otherTotal = _selectedQuantities.entries
        .where((entry) => entry.key != match.id)
        .fold(0.0, (total, entry) => total + entry.value);
    final remaining = (requestedQuantity - otherTotal).clamp(0.0, double.infinity);
    return (match.availableQuantity ?? 0).clamp(0.0, remaining).toDouble();
  }

  String? _getQuantityError(RecommendedMatch match) {
    final qty = _selectedQuantities[match.id];
    if (qty == null) return null;
    if (qty <= 0) return 'Quantity must be greater than 0.';
    if (quantities.isDiscreteUnit(match.unit ?? '') && qty != qty.roundToDouble()) {
      return 'This unit must use a whole quantity.';
    }
    final avail = match.availableQuantity ?? 0;
    if (qty > avail) {
      return 'Exceeds seller available stock (${quantities.formatQuantity(avail, match.unit ?? '')}).';
    }
    return null;
  }

  Widget _buildBottomSummaryBar(double requestedQuantity, String unit) {
    final selectedTotal =
        _selectedQuantities.values.fold(0.0, (sum, q) => sum + q);
    final remaining = requestedQuantity - selectedTotal;
    final isOver = remaining < 0;
    final isComplete = remaining == 0;

    final totalMaterialCost =
        _selectedQuantities.entries.fold(0.0, (sum, entry) {
      final match = _selectedMatches[entry.key];
      return sum + (entry.value * (match?.unitPrice ?? 0.0));
    });

    final totalTransportCost =
        _selectedQuantities.entries.fold(0.0, (sum, entry) {
      final match = _selectedMatches[entry.key];
      return sum + (match?.estimatedTransportCost ?? 0.0);
    });

    final totalCost = totalMaterialCost + totalTransportCost;

    bool hasErrors = false;
    String? errorMsg;
    if (selectedTotal <= 0) {
      hasErrors = true;
      errorMsg = 'Total selected quantity must be greater than 0.';
    } else if (isOver) {
      hasErrors = true;
      errorMsg =
          'Total selected exceeds requirement by ${quantities.formatQuantity(-remaining, unit)} $unit.';
    } else {
      for (final entry in _selectedQuantities.entries) {
        final match = _selectedMatches[entry.key];
        final avail = match?.availableQuantity ?? 0;
        if (entry.value <= 0) {
          hasErrors = true;
          errorMsg = 'Allocated quantity must be greater than 0.';
          break;
        }
        if (entry.value > avail) {
          hasErrors = true;
          errorMsg =
              'Allocated quantity for ${match?.sellerName ?? "seller"} exceeds available stock ($avail).';
          break;
        }
      }
    }

    return Container(
      key: const Key('multi-match-summary-bar'),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.08),
            blurRadius: 8,
            offset: const Offset(0, -3),
          ),
        ],
        border: const Border(
          top: BorderSide(color: Color(0xFFE2E8F0)),
        ),
      ),
      child: SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_multiMatchError != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Text(
                  _multiMatchError!,
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.error,
                    fontSize: 12,
                  ),
                ),
              ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Need: ${quantities.formatQuantity(requestedQuantity, unit)} $unit',
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                    color: SurplusLinkTheme.slate600,
                  ),
                ),
                Text(
                  'Selected: ${quantities.formatQuantity(selectedTotal, unit)} $unit',
                  key: const Key('summary-selected-total'),
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: isOver
                        ? Theme.of(context).colorScheme.error
                        : (isComplete
                            ? const Color(0xFF15803D)
                            : SurplusLinkTheme.amberDark),
                  ),
                ),
                Text(
                  isOver
                      ? 'Over: ${quantities.formatQuantity(-remaining, unit)} $unit'
                      : 'Remaining: ${quantities.formatQuantity(remaining, unit)} $unit',
                  key: const Key('summary-remaining-quantity'),
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: isOver
                        ? Theme.of(context).colorScheme.error
                        : SurplusLinkTheme.slate700,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            ..._selectedQuantities.entries.map((entry) {
              final match = _selectedMatches[entry.key]!;
              return Padding(
                padding: const EdgeInsets.only(top: 2),
                child: Text(
                  '${match.sellerBusinessName ?? match.sellerName ?? 'Seller'} — Available: ${quantities.formatQuantity(match.availableQuantity ?? 0, unit)} $unit · Allocated: ${quantities.formatQuantity(entry.value, unit)} $unit',
                  style: const TextStyle(fontSize: 11, color: SurplusLinkTheme.slate700),
                ),
              );
            }),
            const SizedBox(height: 4),
            Text(
              isComplete ? 'Requirement fully covered' : 'Partial fulfillment — ${quantities.formatQuantity(remaining, unit)} $unit will remain unfulfilled.',
              key: const Key('fulfillment-status'),
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: isComplete ? const Color(0xFF15803D) : SurplusLinkTheme.amberDark,
              ),
            ),
            const SizedBox(height: 6),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Estimated Total:',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: SurplusLinkTheme.slate900,
                  ),
                ),
                Text(
                  formatCurrency(totalCost),
                  key: const Key('summary-total-cost'),
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: SurplusLinkTheme.slate900,
                  ),
                ),
              ],
            ),
            if (errorMsg != null)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  errorMsg,
                  key: const Key('summary-validation-error'),
                  style: TextStyle(
                    fontSize: 11,
                    color: Theme.of(context).colorScheme.error,
                  ),
                ),
              ),
            const SizedBox(height: 8),
            FilledButton.icon(
              key: const Key('submit-selections'),
              onPressed: (hasErrors || _submittingMultiMatch)
                  ? null
                  : () => _submitMultiSelections(
                      requestedQuantity,
                      unit,
                      totalCost,
                    ),
              icon: _submittingMultiMatch
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.check_circle_outline),
              label: Text(
                _submittingMultiMatch
                    ? 'Submitting...'
                    : 'Submit ${_selectedQuantities.length} Selection${_selectedQuantities.length > 1 ? 's' : ''}',
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submitMultiSelections(
    double requestedQuantity,
    String unit,
    double totalCost,
  ) async {
    if (_submittingMultiMatch || _selectedQuantities.isEmpty) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(
          'Submit ${_selectedQuantities.length} Match Selection${_selectedQuantities.length > 1 ? 's' : ''}?',
        ),
        content: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              if (requestedQuantity - _selectedQuantities.values.fold(0.0, (sum, quantity) => sum + quantity) > 0)
                Text(
                  'Partial fulfillment: you requested ${quantities.formatQuantity(requestedQuantity, unit)} $unit and selected ${quantities.formatQuantity(_selectedQuantities.values.fold(0.0, (sum, quantity) => sum + quantity), unit)} $unit. ${quantities.formatQuantity(requestedQuantity - _selectedQuantities.values.fold(0.0, (sum, quantity) => sum + quantity), unit)} $unit will remain unfulfilled.',
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              if (requestedQuantity - _selectedQuantities.values.fold(0.0, (sum, quantity) => sum + quantity) > 0)
                const SizedBox(height: 12),
              const Text(
                'Your selections will be sent for manager approval. No stock is reserved until manager approval.',
              ),
              const SizedBox(height: 12),
              const Text(
                'Selected Sellers:',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 6),
              for (final entry in _selectedQuantities.entries) ...[
                Builder(
                  builder: (context) {
                    final match = _selectedMatches[entry.key];
                    final seller =
                        match?.sellerBusinessName ?? match?.sellerName ?? 'Seller';
                    final qty = quantities.formatQuantity(entry.value, unit);
                    final itemCost = entry.value * (match?.unitPrice ?? 0.0);
                    return Padding(
                      padding: const EdgeInsets.symmetric(vertical: 2),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: Text('• $seller: $qty $unit'),
                          ),
                          Text(formatCurrency(itemCost)),
                        ],
                      ),
                    );
                  },
                ),
              ],
              const Divider(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Total Estimated Cost:',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  Text(
                    formatCurrency(totalCost),
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            key: const Key('confirm-multi-selection'),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Confirm & Submit'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() {
      _submittingMultiMatch = true;
      _multiMatchError = null;
    });

    try {
      final allocations = _selectedQuantities.entries
          .map((e) => MatchAllocation(matchId: e.key, quantity: e.value))
          .toList();
      await widget.gateway.selectMatches(widget.requirementId, allocations);
      if (mounted) {
        await _load(page: 1);
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Selections saved and sent for manager approval.')),
        );
        context.go('/requirements/${widget.requirementId}');
      }
    } on Object catch (error) {
      if (mounted) {
        setState(() => _multiMatchError = matchError(error));
      }
    } finally {
      if (mounted) {
        setState(() => _submittingMultiMatch = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final requestedQuantity = (_data != null && _data!.items.isNotEmpty)
        ? (_data!.items.first.quantity ?? 0.0)
        : 0.0;
    final unit = (_data != null && _data!.items.isNotEmpty)
        ? (_data!.items.first.unit ?? '')
        : '';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Recommended Matches'),
        leading: DashboardBackButton(
          fallback: '/requirements/${widget.requirementId}',
        ),
      ),
      bottomNavigationBar: (_data != null && _selectedQuantities.isNotEmpty)
          ? _buildBottomSummaryBar(requestedQuantity, unit)
          : null,
      body: RefreshIndicator(
        onRefresh: () => _load(),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          children: [
            const Text(
              'AI highlights a recommendation, but only your one selected valid match is sent for manager approval. No material is reserved by this choice.',
            ),
            const SizedBox(height: 8),
            _select('Eligibility', 'match-eligibility', _eligibility, const {
              'all': 'All matches',
              'valid': 'Not rejected',
              'rejected': 'Rejected',
            }, (value) => _eligibility = value),
            _select('Status', 'match-status', _status ?? 'all', const {
              'all': 'Any status',
              'GENERATED': 'Generated',
              'RANKED': 'Ranked',
              'ROUTED': 'Routed',
              'ROUTE_FAILED': 'Route failed',
              'REJECTED': 'Rejected',
            }, (value) => _status = value == 'all' ? null : value),
            _select('Sort by', 'match-sort', _sort, const {
              'score': 'Score',
              'distance': 'Distance',
              'estimatedTransportCost': 'Transport estimate',
              'createdAt': 'Created date',
            }, (value) => _sort = value),
            _select('Direction', 'match-direction', _direction, const {
              'desc': 'Descending',
              'asc': 'Ascending',
            }, (value) => _direction = value),
            const SizedBox(height: 10),
            if (_loading)
              const Center(child: CircularProgressIndicator())
            else if (_error != null)
              MatchErrorBox(_error!, onRetry: () => _load())
            else if (_data case final data?) ...[
              Text('${data.total} matches found'),
              if (data.items.any(
                (match) => match.requirementStatus == 'PENDING_APPROVAL',
              ))
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: OutlinedButton.icon(
                    onPressed: _selecting ? null : _changeSelection,
                    icon: const Icon(Icons.swap_horiz),
                    label: const Text('Change selection / refresh matches'),
                  ),
                ),
              if (data.items.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Text(
                    'No matches found. Try other filters or check again after matching runs.',
                  ),
                ),
              const SizedBox(height: 6),
              for (final match in data.items)
                MatchListCard(
                  match: match,
                  isSelected: _selectedQuantities.containsKey(match.id),
                  onToggleSelect: match.requirementStatus == 'MATCH_FOUND' &&
                          match.isSelectable
                      ? (selected) => _toggleSelectMatch(
                          match,
                          selected,
                          requestedQuantity,
                        )
                      : null,
                  selectedQuantity: _selectedQuantities[match.id],
                  onQuantityChanged: (qty) =>
                      _updateMatchQuantity(match.id, qty),
                  maximumQuantity: _maximumFor(match, requestedQuantity),
                  onRemove: () => _toggleSelectMatch(match, false, requestedQuantity),
                  quantityError: _getQuantityError(match),
                  onTap: () => context.push(
                    '/requirements/${widget.requirementId}/matches/${match.id}',
                  ),
                ),
              MatchPagination(
                page: _page,
                totalPages: data.totalPages,
                busy: _loading,
                onPage: (page) => _load(page: page),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _select(
    String label,
    String key,
    String value,
    Map<String, String> choices,
    ValueChanged<String> change,
  ) => Padding(
    padding: const EdgeInsets.only(bottom: 8),
    child: DropdownButtonFormField<String>(
      key: Key(key),
      initialValue: value,
      isExpanded: true,
      decoration: InputDecoration(labelText: label),
      items: choices.entries
          .map(
            (entry) =>
                DropdownMenuItem(value: entry.key, child: Text(entry.value)),
          )
          .toList(),
      onChanged: (value) {
        if (value != null) {
          change(value);
          _load(page: 1);
        }
      },
    ),
  );
}

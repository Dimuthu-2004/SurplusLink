import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_widgets.dart';
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

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Recommended Matches'),
      leading: DashboardBackButton(
        fallback: '/requirements/${widget.requirementId}',
      ),
    ),
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

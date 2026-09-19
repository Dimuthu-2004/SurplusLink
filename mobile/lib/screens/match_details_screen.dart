import 'package:flutter/material.dart';
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
          _error = matchError(error);
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
            padding: const EdgeInsets.all(20),
            children: [
              if (_error != null)
                MatchErrorBox(_error!, onRetry: _load)
              else if (_match case final match?) ...[
                Text(
                  'Score ${(match.score * 100).toStringAsFixed(1)}%',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                Text('Status: ${match.status.replaceAll('_', ' ')}'),
                const SizedBox(height: 16),
                Text(
                  'Distance: ${match.distance == null ? 'Unavailable' : '${match.distance!.toStringAsFixed(2)} km'}',
                ),
                Text(
                  'Transport estimate: ${match.estimatedTransportCost == null ? 'Unavailable' : 'LKR ${match.estimatedTransportCost!.toStringAsFixed(2)}'}',
                ),
                if (match.rejectionReason != null)
                  Text('Rejection reason: ${match.rejectionReason}'),
                Text('Created: ${match.createdAt.toLocal()}'),
                SelectableText('Material listing: ${match.listingId}'),
                const SizedBox(height: 16),
                const Text(
                  'Read-only recommendation. This screen does not approve matches or reserve materials.',
                ),
                const SizedBox(height: 24),
                Text(
                  'Match history',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                if (_historyLoading)
                  const Center(child: CircularProgressIndicator())
                else if (_historyError != null)
                  MatchErrorBox(
                    _historyError!,
                    onRetry: () => _loadHistory(_historyPage),
                  )
                else if (_history case final history?) ...[
                  if (history.items.isEmpty)
                    const Text('No history entries yet.'),
                  for (final entry in history.items)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(
                        '${entry.action}${entry.outcome == null ? '' : ' · ${entry.outcome}'}',
                      ),
                      subtitle: Text('${entry.createdAt.toLocal()}'),
                    ),
                  MatchPagination(
                    page: _historyPage,
                    totalPages: history.totalPages,
                    busy: _historyLoading,
                    onPage: _loadHistory,
                  ),
                ],
              ],
            ],
          ),
  );
}

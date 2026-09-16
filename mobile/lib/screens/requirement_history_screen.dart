import 'package:flutter/material.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';

class RequirementHistoryScreen extends StatefulWidget {
  const RequirementHistoryScreen({
    required this.gateway,
    required this.requirementId,
    super.key,
  });
  final RequirementGateway gateway;
  final String requirementId;
  @override
  State<RequirementHistoryScreen> createState() =>
      _RequirementHistoryScreenState();
}

class _RequirementHistoryScreenState extends State<RequirementHistoryScreen> {
  RequirementPage<RequirementHistoryEntry>? _data;
  String? _error;
  bool _loading = true;
  int _generation = 0, _page = 1;
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load([int page = 1]) async {
    final generation = ++_generation;
    setState(() {
      _loading = true;
      _error = null;
      _page = page;
      _data = null;
    });
    try {
      final result = await widget.gateway.history(
        widget.requirementId,
        page: page,
      );
      if (mounted && generation == _generation) setState(() => _data = result);
    } on Object catch (error) {
      if (mounted && generation == _generation) {
        setState(() => _error = requirementError(error));
      }
    } finally {
      if (mounted && generation == _generation) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Requirement History'),
      leading: RequirementBackButton(
        fallback: '/requirements/${widget.requirementId}',
      ),
      actions: [
        IconButton(
          tooltip: 'Refresh history',
          onPressed: _loading ? null : () => _load(_page),
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    body: _loading
        ? const Center(child: CircularProgressIndicator())
        : Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 720),
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (_error != null)
                    RequirementErrorBox(_error!, onRetry: () => _load(_page)),
                  if (_data case final data?) ...[
                    Text('${data.total} events · Oldest first'),
                    if (data.items.isEmpty)
                      const Padding(
                        padding: EdgeInsets.all(24),
                        child: Text('No history events on this page.'),
                      ),
                    ...data.items.map(
                      (entry) => Card(
                        child: ListTile(
                          leading: Icon(
                            entry.action == 'STATUS_CHANGED'
                                ? Icons.swap_horiz
                                : Icons.history,
                          ),
                          title: Text(
                            entry.fromStatus != null && entry.toStatus != null
                                ? '${statusLabel(entry.fromStatus!)} → ${statusLabel(entry.toStatus!)}'
                                : statusLabel(entry.action),
                          ),
                          subtitle: Text(
                            '${requirementDate(entry.createdAt)}\n${entry.actorUserId == null ? 'System action' : 'Account action'}',
                          ),
                          isThreeLine: true,
                        ),
                      ),
                    ),
                    RequirementPager(
                      page: data.page,
                      totalPages: data.totalPages,
                      onPage: _load,
                    ),
                  ],
                ],
              ),
            ),
          ),
  );
}

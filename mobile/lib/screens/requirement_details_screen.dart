import 'package:mobile/widgets/location_card.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';

class RequirementDetailsScreen extends StatefulWidget {
  const RequirementDetailsScreen({
    required this.gateway,
    required this.requirementId,
    this.locationLookup,
    this.showMatches = false,
    super.key,
  });
  final RequirementGateway gateway;
  final String requirementId;
  final AddressLookup? locationLookup;
  final bool showMatches;
  @override
  State<RequirementDetailsScreen> createState() =>
      _RequirementDetailsScreenState();
}

class _RequirementDetailsScreenState extends State<RequirementDetailsScreen> {
  BuyerRequirement? _row;
  String? _error, _workflowId, _categoryName;
  bool _loading = true, _busy = false, _sending = false;

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
      final row = await widget.gateway.get(widget.requirementId);
      // Category labels enrich details; a category lookup failure must not hide a readable request.
      String? name;
      try {
        name = (await widget.gateway.categories())
            .where((x) => x.id == row.categoryId)
            .firstOrNull
            ?.name;
      } on Object {
        /* The requirement remains readable without a category label. */
      }
      if (mounted) {
        setState(() {
          _row = row;
          _categoryName = name;
        });
      }
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _error = requirementError(error);
          _row = null;
        });
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _action(String action) async {
    if (_busy || _row == null) return;
    setState(() => _busy = true);
    final (title, message) = switch (action) {
      'submit' => (
        'Submit requirement?',
        'Submitting opens this requirement. You will no longer be able to edit or delete it.',
      ),
      'start' => (
        'Start matching?',
        'Look for materials that meet this requirement. Starting matching does not reserve any material.',
      ),
      'cancel' => (
        'Cancel requirement?',
        'This requirement will be cancelled. This action cannot be undone.',
      ),
      _ => ('Delete draft?', 'This draft will be permanently deleted.'),
    };
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Go back'),
          ),
          FilledButton(
            key: const Key('confirm-requirement-action'),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Confirm'),
          ),
        ],
      ),
    );
    if (!mounted) return;
    if (confirmed != true) {
      setState(() => _busy = false);
      return;
    }
    setState(() {
      _error = null;
      _sending = true;
    });
    try {
      switch (action) {
        case 'submit':
          final row = await widget.gateway.submit(widget.requirementId);
          if (mounted) setState(() => _row = row);
        case 'start':
          final result = await widget.gateway.startMatching(
            widget.requirementId,
          );
          if (!mounted) return;
          setState(() {
            _row = result.requirement;
            _workflowId = result.workflowId;
            _sending = false;
          });
          await context.push(
            '/requirements/${widget.requirementId}/status',
            extra: _workflowId,
          );
          if (mounted) await _load();
        case 'cancel':
          final row = await widget.gateway.cancel(widget.requirementId);
          if (mounted) setState(() => _row = row);
        case 'delete':
          await widget.gateway.delete(widget.requirementId);
          if (mounted) context.go('/requirements');
      }
    } on Object catch (error) {
      if (error is ApiException &&
          (error.statusCode == 409 || error.statusCode == 503)) {
        try {
          final row = await widget.gateway.get(widget.requirementId);
          if (mounted) setState(() => _row = row);
        } on Object {
          /* Keep the last confirmed state and original action error. */
        }
      }
      if (mounted) setState(() => _error = requirementError(error));
    } finally {
      if (mounted) {
        setState(() {
          _busy = false;
          _sending = false;
        });
      }
    }
  }

  Future<void> _edit() async {
    await context.push('/requirements/${widget.requirementId}/edit');
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Requirement Details'),
      leading: const RequirementBackButton(),
      actions: [
        IconButton(
          tooltip: 'Refresh details',
          onPressed: _busy || _loading ? null : _load,
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
                padding: const EdgeInsets.all(20),
                children: [
                  if (_error != null)
                    RequirementErrorBox(
                      _error!,
                      onRetry: _row == null ? _load : null,
                    ),
                  if (_row case final row?) ...[
                    Text(
                      _categoryName ?? 'Requirement',
                      style: Theme.of(context).textTheme.headlineSmall,
                    ),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: RequirementStatusChip(row.status),
                    ),
                    if (row.workflowStatus == 'REVISION_REQUESTED')
                      Card(
                        color: Theme.of(context).colorScheme.errorContainer,
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Text('Manager requested changes${row.decisionNote == null || row.decisionNote!.isEmpty ? '.' : ': ${row.decisionNote}'}'),
                        ),
                      ),
                    _value(
                      'Required quantity',
                      '${row.requiredQuantity} ${row.unit}',
                    ),
                    _value('Maximum budget', row.maximumBudget.toString()),
                    _value(
                      'Deadline (local time)',
                      requirementDate(row.deadline),
                    ),
                    if (!row.deadline.isAfter(DateTime.now()) && row.canCancel)
                      const Text(
                        'The deadline has passed. Edit a draft to choose a future date, or cancel this requirement.',
                      ),
                    _value(
                      'Notes',
                      row.notes.isEmpty ? 'No notes added.' : row.notes,
                    ),
                    if (row.latitude != null && row.longitude != null)
                      LocationCard(
                        latitude: row.latitude!,
                        longitude: row.longitude!,
                        lookup: widget.locationLookup ?? unavailableAddress,
                      )
                    else
                      const Text('Location was not recorded.'),
                    _value('Created', requirementDate(row.createdAt)),
                    _value('Updated', requirementDate(row.updatedAt)),
                    if (_sending) const LinearProgressIndicator(),
                    Wrap(
                      spacing: 10,
                      runSpacing: 10,
                      children: [
                        if (row.canEdit)
                          OutlinedButton(
                            key: const Key('requirement-edit'),
                            onPressed: _busy ? null : _edit,
                            child: const Text('Edit'),
                          ),
                        if (row.status == 'DRAFT')
                          FilledButton(
                            key: const Key('requirement-submit'),
                            onPressed: _busy || !row.canSubmit
                                ? null
                                : () => _action('submit'),
                            child: const Text('Submit'),
                          ),
                        if (row.status == 'OPEN')
                          FilledButton(
                            key: const Key('requirement-start'),
                            onPressed: _busy || !row.canStart
                                ? null
                                : () => _action('start'),
                            child: Text(row.status == 'MATCH_FOUND' ? 'Refresh matches' : 'Start matching'),
                          ),
                        if (row.canCancel && row.status != 'DRAFT')
                          OutlinedButton(
                            key: const Key('requirement-cancel'),
                            onPressed: _busy ? null : () => _action('cancel'),
                            child: const Text('Cancel requirement'),
                          ),
                      ],
                    ),
                    if (row.status != 'DRAFT') ...[
                      const SizedBox(height: 16),
                      if (widget.showMatches)
                        OutlinedButton.icon(
                        key: const Key('open-recommended-matches'),
                        onPressed: _busy
                            ? null
                            : () => context.push(
                                '/requirements/${row.id}/matches',
                              ),
                        icon: const Icon(Icons.recommend_outlined),
                        label: const Text('Recommended Matches'),
                        ),
                      OutlinedButton.icon(
                      onPressed: _busy
                          ? null
                          : () async {
                              await context.push(
                                '/requirements/${row.id}/status',
                                extra: _workflowId,
                              );
                              if (mounted) _load();
                            },
                      icon: const Icon(Icons.track_changes),
                      label: const Text('Workflow / recommendation status'),
                      ),
                      OutlinedButton.icon(
                      onPressed: _busy
                          ? null
                          : () =>
                                context.push('/requirements/${row.id}/history'),
                      icon: const Icon(Icons.history),
                      label: const Text('History'),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          ),
  );
  Widget _value(String label, String value) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 10),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(fontWeight: FontWeight.bold)),
        Text(value),
      ],
    ),
  );
}

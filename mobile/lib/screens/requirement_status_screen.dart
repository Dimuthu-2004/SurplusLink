import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';

class RequirementStatusScreen extends StatefulWidget {
  const RequirementStatusScreen({
    required this.gateway,
    required this.requirementId,
    this.workflowId,
    this.showMatches = false,
    super.key,
  });
  final RequirementGateway gateway;
  final String requirementId;
  final String? workflowId;
  final bool showMatches;
  @override
  State<RequirementStatusScreen> createState() =>
      _RequirementStatusScreenState();
}

class _RequirementStatusScreenState extends State<RequirementStatusScreen> {
  BuyerRequirement? _row;
  String? _error;
  bool _loading = true;
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
      if (mounted) setState(() => _row = row);
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

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Matching Status'),
      leading: RequirementBackButton(
        fallback: '/requirements/${widget.requirementId}',
      ),
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
                    RequirementErrorBox(_error!, onRetry: _load),
                  if (_row case final row?) ...[
                    const Icon(Icons.track_changes, size: 56),
                    Center(child: RequirementStatusChip(row.status)),
                    Text(_description(row.status), textAlign: TextAlign.center),
                    const SizedBox(height: 24),
                    const Text(
                      'Workflow',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    Text(
                      widget.workflowId == null
                          ? 'Workflow details are not available.'
                          : 'Workflow ID: ${widget.workflowId!}',
                    ),
                    const SizedBox(height: 24),
                    const Text(
                      'Recommendations',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    if (widget.showMatches)
                      OutlinedButton.icon(
                        onPressed: () =>
                            context.push('/requirements/${row.id}/matches'),
                        icon: const Icon(Icons.recommend_outlined),
                        label: const Text('Recommended Matches'),
                      )
                    else
                      const Text(
                        'Recommendation details are not available in the app yet. Refresh to check the latest requirement status.',
                      ),
                    const SizedBox(height: 12),
                    Text('Last updated: ${requirementDate(row.updatedAt)}'),
                    OutlinedButton.icon(
                      onPressed: _load,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Refresh status'),
                    ),
                  ],
                ],
              ),
            ),
          ),
  );
  String _description(String status) => switch (status) {
    'DRAFT' => 'Review and submit your draft before starting matching.',
    'OPEN' =>
      'Your requirement is open. You can request matching from its details.',
    'MATCHING' => 'Matching has started. Check back for a status update.',
    'MATCH_FOUND' => 'A match has been found for your requirement.',
    'PENDING_APPROVAL' => 'Your match is waiting for approval.',
    'APPROVED' => 'Your match has been approved.',
    'REJECTED' => 'Your match was rejected.',
    'COMPLETED' => 'This requirement has been completed.',
    'CANCELLED' => 'This requirement has been cancelled.',
    _ => 'Refresh to check the latest requirement status.',
  };
}

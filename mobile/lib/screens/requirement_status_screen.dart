import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';
import 'package:mobile/l10n/app_localizations.dart';
import 'package:mobile/widgets/submission_animation_overlays.dart';

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
  bool _fetching = false;
  Timer? _poll;
  Timer? _longRunningNotice;
  bool _isLongRunning = false;

  bool get _workflowRunning {
    final row = _row;
    if (row == null ||
        const {'FAILED', 'COMPLETED'}.contains(row.workflowStatus)) {
      return false;
    }
    return row.status == 'MATCHING' || row.workflowStatus == 'RUNNING';
  }

  @override
  void dispose() {
    _poll?.cancel();
    _longRunningNotice?.cancel();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    if (_fetching || !mounted) return;
    _fetching = true;
    _poll?.cancel();
    setState(() {
      _loading = _row == null;
      _error = null;
    });
    try {
      final row = await widget.gateway.get(widget.requirementId);
      if (mounted) {
        setState(() => _row = row);
        if (_workflowRunning && _longRunningNotice == null) {
          _longRunningNotice = Timer(const Duration(seconds: 12), () {
            if (mounted && _workflowRunning) {
              setState(() => _isLongRunning = true);
            }
          });
        }
        if (!_workflowRunning) {
          _longRunningNotice?.cancel();
          _longRunningNotice = null;
          _isLongRunning = false;
          if (row.status == 'MATCH_FOUND' && widget.showMatches) {
            context.go('/requirements/${widget.requirementId}/matches');
            return;
          }
        }
      }
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _error = requirementError(error);
        });
      }
    } finally {
      _fetching = false;
      if (mounted) {
        setState(() => _loading = false);
        if (_workflowRunning) {
          _poll = Timer(const Duration(seconds: 4), _load);
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final text = AppLocalizations.of(context);
    final workflowRunning = _workflowRunning && _error == null;
    return PopScope(
      canPop: !workflowRunning,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Matching Status'),
          leading: RequirementBackButton(
            fallback: '/requirements/${widget.requirementId}',
          ),
        ),
        body: Stack(
          children: [
            _loading
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
                            if (row.workflowStatus != null)
                              Text(
                                'Workflow: ${row.workflowStatus!.replaceAll('_', ' ')}',
                              ),
                            if (row.workflowStatus == 'FAILED') ...[
                              Text(
                                text?.matchingCouldNotComplete ??
                                    'Matching could not be completed.',
                              ),
                              const SizedBox(height: 12),
                              FilledButton(
                                onPressed: () => context.go(
                                  '/requirements/${widget.requirementId}',
                                ),
                                child: Text(
                                  text?.retryMatching ?? 'Retry Matching',
                                ),
                              ),
                            ],
                            if (row.decisionNote?.isNotEmpty == true)
                              Text('Manager note: ${row.decisionNote}'),
                            Text(
                              _description(row.status),
                              textAlign: TextAlign.center,
                            ),
                            const SizedBox(height: 24),
                            const Text(
                              'Workflow',
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            Text(
                              (row.workflowId ?? widget.workflowId) == null
                                  ? 'Workflow details are not available.'
                                  : 'Workflow ID: ${row.workflowId ?? widget.workflowId}',
                            ),
                            const SizedBox(height: 24),
                            const Text(
                              'Recommendations',
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            if (widget.showMatches)
                              OutlinedButton.icon(
                                onPressed: () => context.push(
                                  '/requirements/${row.id}/matches',
                                ),
                                icon: const Icon(Icons.recommend_outlined),
                                label: const Text('Recommended Matches'),
                              )
                            else
                              const Text(
                                'Recommendation details are not available in the app yet. Refresh to check the latest requirement status.',
                              ),
                            const SizedBox(height: 12),
                            Text(
                              'Last updated: ${requirementDate(row.updatedAt)}',
                            ),
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
            if (workflowRunning)
              Positioned.fill(
                child: WorkflowRunningOverlay(
                  title:
                      text?.findingSuitableMatches ??
                      'Finding suitable matches...',
                  message: _isLongRunning
                      ? (text?.stillWorkingOnMatches ??
                            'Still working on your matches...')
                      : (text?.matchingWorkflowMessage ??
                            'Our matching workflow is checking available sellers.'),
                ),
              ),
          ],
        ),
      ),
    );
  }

  String _description(String status) => switch (status) {
    'DRAFT' => 'Review and submit your draft before starting matching.',
    'OPEN' =>
      'Your requirement is open. You can request matching from its details.',
    'MATCHING' => 'Matching is running. Status updates automatically.',
    'MATCH_FOUND' => 'A match has been found for your requirement.',
    'PENDING_APPROVAL' => 'Your match is waiting for approval.',
    'APPROVED' => 'Your match has been approved.',
    'REJECTED' => 'Your match was rejected.',
    'COMPLETED' => 'This requirement has been completed.',
    'CANCELLED' => 'This requirement has been cancelled.',
    _ => 'Refresh to check the latest requirement status.',
  };
}

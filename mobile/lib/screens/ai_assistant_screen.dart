import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:lottie/lottie.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';

import '../models/ai_assistant_models.dart';

final class AiAssistantService {
  const AiAssistantService({required this.apiClient});

  final ApiClient apiClient;

  Future<AiChatMessage> sendMessage({
    String? conversationId,
    required String message,
    Map<String, dynamic>? structuredLocation,
  }) async {
    final payload = <String, dynamic>{'message': message.trim()};
    if (conversationId != null && conversationId.isNotEmpty) {
      payload['conversationId'] = conversationId;
    }
    if (structuredLocation != null) {
      payload['structured_location'] = structuredLocation;
    }

    final json = await apiClient.postJson(
      '/api/ai/chat',
      payload,
      authenticated: true,
    );
    final citations = switch (json['citations']) {
      final List values =>
        values
            .whereType<Map>()
            .map(
              (value) => AiCitation.fromJson(Map<String, dynamic>.from(value)),
            )
            .toList(),
      _ => <AiCitation>[],
    };
    final actions = switch (json['suggested_actions']) {
      final List values => values.map((value) => value.toString()).toList(),
      _ => <String>[],
    };
    final draftJson = json['requirement_draft'];
    final clientActionJson = json['client_action'];

    return AiChatMessage(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      conversationId:
          json['conversation_id'] as String? ?? conversationId ?? '',
      role: ChatRole.assistant,
      content: json['message'] as String? ?? '',
      intent: json['intent'] as String?,
      citations: citations,
      draft: draftJson is Map
          ? AiRequirementDraft.fromJson(Map<String, dynamic>.from(draftJson))
          : null,
      clientAction: clientActionJson is Map
          ? AiClientAction.fromJson(Map<String, dynamic>.from(clientActionJson))
          : null,
      suggestedActions: actions,
      timestamp: DateTime.now(),
    );
  }

  Future<Map<String, dynamic>> confirmRequirement(
    Map<String, dynamic> requirement,
  ) => apiClient.postJson('/api/ai/chat/confirm-requirement', {
    'requirement': requirement,
  }, authenticated: true);

  Future<AiWorkflowProgress> workflowProgress({
    required String requirementId,
    required String workflowId,
  }) async {
    final path = Uri(
      path: '/api/requirements/$requirementId/workflow-progress',
      queryParameters: {'workflowId': workflowId},
    ).toString();
    return AiWorkflowProgress.fromJson(
      await apiClient.getJson(path, authenticated: true),
    );
  }
}

class AiAssistantScreen extends StatefulWidget {
  const AiAssistantScreen({
    super.key,
    required this.apiClient,
    this.requirementGateway,
  });

  final ApiClient apiClient;
  final RequirementGateway? requirementGateway;

  @override
  State<AiAssistantScreen> createState() => _AiAssistantScreenState();
}

class _AiAssistantScreenState extends State<AiAssistantScreen> {
  late final AiAssistantService _assistantService;
  final TextEditingController _inputController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  final List<AiChatMessage> _messages = [];

  String? _conversationId;
  bool _isLoading = false;
  String? _activeRequirementId;
  String? _activeWorkflowId;
  AiWorkflowProgress? _workflowProgress;
  String? _workflowProgressError;
  Timer? _workflowPoll;
  bool _isPollingWorkflow = false;
  bool _isConfirmingRequirement = false;

  final List<String> _quickPrompts = const [
    "Find materials",
    "Create a requirement",
    "How does matching work?",
    "My transactions",
    "How do I list a material?",
  ];

  @override
  void initState() {
    super.initState();
    _assistantService = AiAssistantService(apiClient: widget.apiClient);

    _messages.add(
      AiChatMessage(
        id: 'welcome',
        conversationId: '',
        role: ChatRole.assistant,
        content: "Hello! I'm your SurplusLink AI Assistant.\nI can help you find construction materials, answer Sri Lanka material storage questions, create requirement drafts, or check your active transactions!",
        suggestedActions: _quickPrompts,
        timestamp: DateTime.now(),
      ),
    );
  }

  @override
  void dispose() {
    _inputController.dispose();
    _scrollController.dispose();
    _workflowPoll?.cancel();
    super.dispose();
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _handleSendMessage([String? textOverride]) async {
    final text = textOverride ?? _inputController.text.trim();
    if (text.isEmpty || _isLoading) return;

    if (textOverride == null) {
      _inputController.clear();
    }

    final userMsg = AiChatMessage(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      conversationId: _conversationId ?? '',
      role: ChatRole.user,
      content: text,
      timestamp: DateTime.now(),
    );

    setState(() {
      _messages.add(userMsg);
      _isLoading = true;
    });
    _scrollToBottom();

    try {
      final assistantMsg = await _assistantService.sendMessage(
        conversationId: _conversationId,
        message: text,
      );

      if (mounted) {
        setState(() {
          _conversationId = assistantMsg.conversationId;
          _messages.add(assistantMsg);
          _isLoading = false;
        });
        _scrollToBottom();

        if (assistantMsg.clientAction?.type == 'REQUEST_DEVICE_LOCATION') {
          _handleDeviceLocationRequest();
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _messages.add(
            AiChatMessage(
              id: DateTime.now().millisecondsSinceEpoch.toString(),
              conversationId: _conversationId ?? '',
              role: ChatRole.assistant,
              content: "I couldn't complete that request right now. Please check your connection and try again.",
              timestamp: DateTime.now(),
              isError: true,
            ),
          );
          _isLoading = false;
        });
        _scrollToBottom();
      }
    }
  }

  Future<void> _handleDeviceLocationRequest() async {
    try {
      const locationSource = DeviceRequirementLocation();
      final loc = await locationSource.capture();
      String? address;
      try {
        final lookup = ApiLocationLookup(widget.apiClient);
        address = await lookup.lookup(loc.latitude, loc.longitude);
      } catch (_) {
        // Retain coordinates even if reverse geocode fails!
      }

      final structuredLoc = <String, dynamic>{
        'latitude': loc.latitude,
        'longitude': loc.longitude,
        'resolved_address': address,
        'location_source': 'CURRENT_DEVICE_LOCATION',
      };

      if (!mounted) return;
      setState(() => _isLoading = true);

      final assistantMsg = await _assistantService.sendMessage(
        conversationId: _conversationId,
        message: 'Location acquired',
        structuredLocation: structuredLoc,
      );

      if (mounted) {
        setState(() {
          _conversationId = assistantMsg.conversationId;
          _messages.add(assistantMsg);
          _isLoading = false;
        });
        _scrollToBottom();
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _handleConfirmRequirement(AiRequirementDraft draft) async {
    if (_activeWorkflowId != null || _isConfirmingRequirement) return;
    if (draft.categoryId == null ||
        draft.maximumBudget == null ||
        draft.deadline == null ||
        draft.latitude == null ||
        draft.longitude == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Please provide the remaining delivery, budget, and deadline details in chat.',
          ),
        ),
      );
      return;
    }
    try {
      setState(() {
        _isLoading = true;
        _isConfirmingRequirement = true;
      });
      final response = await _assistantService.confirmRequirement({
        'categoryId': draft.categoryId,
        if (draft.templateId != null && draft.templateId != 'CUSTOM_ITEM')
          'constructionItemTemplateId': draft.templateId,
        'requiredQuantity': draft.normalizedQuantity,
        'unit': draft.normalizedBaseUnit,
        'maximumBudget': draft.maximumBudget,
        'deadline': DateTime.parse(draft.deadline!).toUtc().toIso8601String(),
        'latitude': draft.latitude,
        'longitude': draft.longitude,
        'notes': draft.notes ?? '',
        'deliveryRequired': draft.deliveryRequired,
        'inputMode': draft.inputMode,
        'enteredQuantity': draft.enteredQuantity,
        'enteredUnit': draft.enteredUnit,
        if (draft.packageSize != null)
          'preferredPackageSize': draft.packageSize,
        if (draft.packageUnit != null) 'packageBaseUnit': draft.packageUnit,
      });
      if (!mounted) return;
      final requirement = response['requirement'] as Map<String, dynamic>;
      final requirementId = requirement['id'] as String?;
      final workflowId = response['workflowId'] as String?;
      if (requirementId == null || workflowId == null) {
        throw const FormatException(
          'The workflow confirmation response was incomplete.',
        );
      }
      setState(() {
        _isLoading = false;
        _isConfirmingRequirement = false;
        _activeRequirementId = requirementId;
        _activeWorkflowId = workflowId;
        _workflowProgress = null;
        _workflowProgressError = null;
        _messages.add(
          AiChatMessage(
            id: 'workflow-$workflowId',
            conversationId: _conversationId ?? '',
            role: ChatRole.assistant,
            content: 'Your requirement is confirmed. I’m checking the real workflow status here.',
            timestamp: DateTime.now(),
          ),
        );
      });
      _scrollToBottom();
      await _loadWorkflowProgress();
    } catch (_) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _isConfirmingRequirement = false;
        });
        _messages.add(
          AiChatMessage(
            id: DateTime.now().millisecondsSinceEpoch.toString(),
            conversationId: _conversationId ?? '',
            role: ChatRole.assistant,
            content: 'I could not verify that matching started. Check your requirements before trying again to avoid creating a duplicate.',
            timestamp: DateTime.now(),
            isError: true,
          ),
        );
        _scrollToBottom();
      }
    }
  }

  Future<void> _loadWorkflowProgress() async {
    final requirementId = _activeRequirementId;
    final workflowId = _activeWorkflowId;
    if (_isPollingWorkflow || requirementId == null || workflowId == null) {
      return;
    }
    _isPollingWorkflow = true;
    _workflowPoll?.cancel();
    try {
      final progress = await _assistantService.workflowProgress(
        requirementId: requirementId,
        workflowId: workflowId,
      );
      if (!mounted) return;
      setState(() {
        _workflowProgress = progress;
        _workflowProgressError = null;
      });
      if (progress.matchResultsReady) {
        _workflowPoll?.cancel();
        context.go('/requirements/$requirementId/matches');
        return;
      }
      if (progress.hasFailed || !progress.isRunning) {
        _workflowPoll?.cancel();
        return;
      }
      _workflowPoll = Timer(const Duration(seconds: 3), _loadWorkflowProgress);
    } catch (_) {
      if (mounted) {
        setState(
          () => _workflowProgressError = 'Workflow status is temporarily unavailable. Retry to check the backend status.',
        );
      }
    } finally {
      _isPollingWorkflow = false;
    }
  }

  Widget _buildWorkflowProgress(ThemeData theme) {
    final progress = _workflowProgress;
    final isRunning = progress?.isRunning ?? progress == null;
    final workflowTitle = progress == null
        ? 'Checking workflow status...'
        : progress.hasFailed
        ? 'Matching could not complete safely'
        : progress.currentStage == 'QUEUED'
        ? 'Waiting for workflow execution...'
        : progress.isRunning
        ? 'Finding the best options...'
        : 'Workflow status';
    final stages = const [
      ('PLANNER', 'Requirement Planner', 'Preparing the confirmed requirement'),
      ('MATCHING', 'Material Matching', 'Checking actual seller listings'),
      ('LOGISTICS', 'Logistics', 'Checking delivery feasibility'),
      ('VALIDATION', 'Validation', 'Applying stock and business rules'),
    ];

    return Card(
      key: const Key('chat-workflow-progress'),
      margin: const EdgeInsets.only(left: 40, right: 8, bottom: 16),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                if (isRunning && !MediaQuery.disableAnimationsOf(context))
                  SizedBox(
                    width: 38,
                    height: 38,
                    child: Lottie.asset(
                      'assets/animations/workflow-running.json',
                      repeat: true,
                    ),
                  )
                else
                  Icon(
                    progress?.hasFailed == true
                        ? Icons.error_outline
                        : Icons.track_changes,
                    color: progress?.hasFailed == true
                        ? theme.colorScheme.error
                        : theme.colorScheme.primary,
                  ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    workflowTitle,
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            if (progress != null) ...[
              const SizedBox(height: 8),
              Text(
                'Workflow ${progress.workflowId}',
                style: theme.textTheme.labelSmall,
              ),
              const SizedBox(height: 8),
              ...stages.map((stage) {
                AiWorkflowStepProgress? step;
                for (final item in progress.steps) {
                  if (item.stage == stage.$1) {
                    step = item;
                    break;
                  }
                }
                final state = _workflowStageState(progress, stage.$1, step);
                final icon = switch (state) {
                  'COMPLETED' => Icons.check_circle,
                  'RUNNING' => Icons.hourglass_top,
                  'FAILED' => Icons.error,
                  _ => Icons.radio_button_unchecked,
                };
                final color = switch (state) {
                  'COMPLETED' => Colors.green,
                  'FAILED' => theme.colorScheme.error,
                  'RUNNING' => theme.colorScheme.primary,
                  _ => theme.colorScheme.outline,
                };
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(icon, size: 18, color: color),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(stage.$2),
                            Text(
                              _workflowStageDetail(state, step, stage.$3),
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: theme.colorScheme.onSurfaceVariant,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              }),
            ],
            if (_workflowProgressError != null) ...[
              const SizedBox(height: 8),
              Text(
                _workflowProgressError!,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.error,
                ),
              ),
              TextButton.icon(
                onPressed: _isPollingWorkflow ? null : _loadWorkflowProgress,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry status'),
              ),
            ],
            if (progress?.hasFailed == true) ...[
              const SizedBox(height: 8),
              Text(
                progress?.errorCode == 'WORKFLOW_TIMEOUT'
                    ? 'The workflow timed out and could not complete safely.'
                    : 'The workflow failed. No match results are being reported as ready.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.error,
                ),
              ),
              TextButton(
                onPressed: () => context.go(
                  '/requirements/${progress!.requirementId}/status',
                  extra: progress.workflowId,
                ),
                child: const Text('View requirement status'),
              ),
            ],
            if (progress == null && _workflowProgressError == null)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: LinearProgressIndicator(),
              ),
          ],
        ),
      ),
    );
  }

  static String _workflowStageState(
    AiWorkflowProgress progress,
    String stage,
    AiWorkflowStepProgress? step,
  ) {
    if (step?.status == 'COMPLETED') return 'COMPLETED';
    if (step?.status == 'FAILED' || step?.errorCode != null) return 'FAILED';
    if (stage == progress.currentStage && progress.isRunning) return 'RUNNING';
    return 'WAITING';
  }

  static String _workflowStageDetail(
    String state,
    AiWorkflowStepProgress? step,
    String waitingDescription,
  ) {
    if (state == 'FAILED') return 'Could not complete safely';
    if (state == 'WAITING') return 'Waiting';
    if (state == 'RUNNING') return waitingDescription;
    final details = <String>['Completed'];
    if (step != null && step.toolCallCount > 0) {
      details.add('${step.toolCallCount} tool calls');
    }
    final retries = step?.retryCount ?? 0;
    if (retries > 0) {
      details.add('$retries retries');
    }
    final duration = step?.durationMilliseconds;
    if (duration != null) {
      details.add('${(duration / 1000).toStringAsFixed(1)} s');
    }
    return details.join(' · ');
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const SurplusLinkLogo(size: 28),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'SurplusLink AI Assistant',
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    'Sri Lanka Construction & Reuse Assistant',
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        elevation: 1,
      ),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              padding: const EdgeInsets.all(16),
              itemCount:
                  _messages.length +
                  (_isLoading ? 1 : 0) +
                  (_activeWorkflowId == null ? 0 : 1),
              itemBuilder: (context, index) {
                if (index < _messages.length) {
                  return _buildMessageBubble(_messages[index], theme);
                }
                if (_isLoading && index == _messages.length) {
                  return _buildTypingIndicator(theme);
                }
                if (_activeWorkflowId != null) {
                  return _buildWorkflowProgress(theme);
                }
                return const SizedBox.shrink();
              },
            ),
          ),
          if (!_isLoading &&
              _messages.isNotEmpty &&
              _messages.last.suggestedActions.isNotEmpty)
            _buildQuickSuggestions(_messages.last.suggestedActions, theme),
          _buildInputBar(theme),
        ],
      ),
    );
  }

  Widget _buildTypingIndicator(ThemeData theme) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          CircleAvatar(
            backgroundColor: theme.colorScheme.primaryContainer,
            radius: 16,
            child: Icon(
              Icons.smart_toy,
              size: 18,
              color: theme.colorScheme.primary,
            ),
          ),
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(16),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const SizedBox(
                  width: 14,
                  height: 14,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
                const SizedBox(width: 10),
                Text(
                  'SurplusLink AI is thinking...',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMessageBubble(AiChatMessage msg, ThemeData theme) {
    final isUser = msg.role == ChatRole.user;

    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: isUser
            ? CrossAxisAlignment.end
            : CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: isUser
                ? MainAxisAlignment.end
                : MainAxisAlignment.start,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (!isUser) ...[
                CircleAvatar(
                  backgroundColor: theme.colorScheme.primaryContainer,
                  radius: 16,
                  child: Icon(
                    Icons.smart_toy,
                    size: 18,
                    color: theme.colorScheme.primary,
                  ),
                ),
                const SizedBox(width: 8),
              ],
              Flexible(
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: isUser
                        ? theme.colorScheme.primary
                        : msg.isError
                        ? theme.colorScheme.errorContainer
                        : theme.colorScheme.surfaceContainerHighest,
                    borderRadius: BorderRadius.only(
                      topLeft: const Radius.circular(16),
                      topRight: const Radius.circular(16),
                      bottomLeft: Radius.circular(isUser ? 16 : 4),
                      bottomRight: Radius.circular(isUser ? 4 : 16),
                    ),
                  ),
                  child: SelectableText(
                    _formatDisplayContent(msg.content),
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: isUser
                          ? theme.colorScheme.onPrimary
                          : msg.isError
                          ? theme.colorScheme.onErrorContainer
                          : theme.colorScheme.onSurface,
                    ),
                  ),
                ),
              ),
            ],
          ),
          if (!isUser && msg.citations.isNotEmpty)
            _buildCitationsCard(msg.citations, theme),
          if (!isUser && msg.draft != null)
            _CollapsibleRequirementDraftCard(
              draft: msg.draft!,
              theme: theme,
              onReview: () => _handleConfirmRequirement(msg.draft!),
            ),
        ],
      ),
    );
  }

  static String _formatDisplayContent(String raw) {
    var text = raw.replaceAll(RegExp(r'\*\*|\*|`'), '');
    text = text.replaceAll(RegExp(r'^\s*#+\s*', multiLine: true), '');
    return text.trim();
  }

  Widget _buildCitationsCard(List<AiCitation> citations, ThemeData theme) {
    return Container(
      margin: const EdgeInsets.only(left: 40, top: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        border: Border.all(color: theme.colorScheme.outlineVariant),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                Icons.menu_book,
                size: 16,
                color: theme.colorScheme.secondary,
              ),
              const SizedBox(width: 6),
              Text(
                'Sources & Guidance (${citations.length})',
                style: theme.textTheme.labelMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.secondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          ...citations.map(
            (c) => Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Row(
                children: [
                  const Icon(Icons.circle, size: 6, color: Colors.grey),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      '${c.title} — ${c.section} (${c.source})',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQuickSuggestions(List<String> suggestions, ThemeData theme) {
    return Container(
      height: 44,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        itemCount: suggestions.length,
        itemBuilder: (context, index) {
          final s = suggestions[index];
          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ActionChip(
              label: Text(s),
              onPressed: () => _handleSendMessage(s),
              visualDensity: VisualDensity.compact,
            ),
          );
        },
      ),
    );
  }

  Widget _buildInputBar(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        border: Border(
          top: BorderSide(color: theme.colorScheme.outlineVariant),
        ),
      ),
      child: SafeArea(
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: _inputController,
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => _handleSendMessage(),
                decoration: const InputDecoration(
                  hintText:
                      'Ask about materials, requirements, or transactions...',
                  border: InputBorder.none,
                  contentPadding: EdgeInsets.symmetric(horizontal: 12),
                ),
              ),
            ),
            IconButton(
              onPressed: _isLoading ? null : () => _handleSendMessage(),
              icon: const Icon(Icons.send_rounded),
              color: theme.colorScheme.primary,
            ),
          ],
        ),
      ),
    );
  }
}

class _CollapsibleRequirementDraftCard extends StatefulWidget {
  const _CollapsibleRequirementDraftCard({
    required this.draft,
    required this.theme,
    required this.onReview,
  });

  final AiRequirementDraft draft;
  final ThemeData theme;
  final VoidCallback onReview;

  @override
  State<_CollapsibleRequirementDraftCard> createState() =>
      __CollapsibleRequirementDraftCardState();
}

class __CollapsibleRequirementDraftCardState
    extends State<_CollapsibleRequirementDraftCard> {
  bool _isExpanded = true;

  @override
  Widget build(BuildContext context) {
    final draft = widget.draft;
    final theme = widget.theme;

    return Container(
      margin: const EdgeInsets.only(left: 40, top: 10),
      decoration: BoxDecoration(
        color: theme.colorScheme.primaryContainer.withValues(alpha: 0.3),
        border: Border.all(
          color: theme.colorScheme.primary.withValues(alpha: 0.5),
        ),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: () => setState(() => _isExpanded = !_isExpanded),
            borderRadius: BorderRadius.circular(14),
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Icon(
                    Icons.assignment_outlined,
                    color: theme.colorScheme.primary,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Requirement Draft: ${draft.itemName}',
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: theme.colorScheme.primary,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Chip(
                    label: Text(
                      draft.readyForReview ? 'Ready' : 'Incomplete',
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: draft.readyForReview
                            ? Colors.green.shade900
                            : Colors.orange.shade900,
                      ),
                    ),
                    backgroundColor: draft.readyForReview
                        ? Colors.green.shade100
                        : Colors.orange.shade100,
                    visualDensity: VisualDensity.compact,
                  ),
                  const SizedBox(width: 4),
                  Icon(
                    _isExpanded
                        ? Icons.keyboard_arrow_up
                        : Icons.keyboard_arrow_down,
                    color: theme.colorScheme.primary,
                  ),
                ],
              ),
            ),
          ),
          if (_isExpanded) ...[
            const Divider(height: 1),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildDraftRow('Material Item', draft.itemName, theme),
                  _buildDraftRow(
                    'Quantity',
                    draft.inputMode == 'PACKAGE'
                        ? '${draft.enteredQuantity.toInt()} ${draft.enteredUnit} (Total: ${draft.normalizedQuantity} ${draft.normalizedBaseUnit})'
                        : '${draft.normalizedQuantity} ${draft.normalizedBaseUnit}',
                    theme,
                  ),
                  if (draft.preferences != null)
                    ...draft.preferences!.entries.map(
                      (e) =>
                          _buildDraftRow(e.key.toUpperCase(), e.value, theme),
                    ),
                  _buildDraftRow(
                    'Delivery Area',
                    draft.locationText ?? 'Not specified',
                    theme,
                  ),
                  if (draft.maximumBudget != null)
                    _buildDraftRow(
                      'Maximum budget',
                      'LKR ${draft.maximumBudget!.toStringAsFixed(2)}',
                      theme,
                    ),
                  if (draft.deadline != null)
                    _buildDraftRow('Needed by', draft.deadline!, theme),
                  if (draft.missingRequiredFields.isNotEmpty) ...[
                    const SizedBox(height: 6),
                    Text(
                      'Missing required information: ${draft.missingRequiredFields.join(", ")}',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.error,
                      ),
                    ),
                  ],
                  const SizedBox(height: 12),
                  if (draft.readyForReview)
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: widget.onReview,
                        icon: const Icon(Icons.rate_review_outlined),
                        label: const Text('Yes, find matches'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: theme.colorScheme.primary,
                          foregroundColor: theme.colorScheme.onPrimary,
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildDraftRow(String label, String value, ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(
              label,
              style: theme.textTheme.bodySmall?.copyWith(
                fontWeight: FontWeight.bold,
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: theme.textTheme.bodySmall?.copyWith(
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

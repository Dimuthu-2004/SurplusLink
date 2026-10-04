import 'package:flutter/material.dart';
import 'package:mobile/core/api_client.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_location.dart';
import 'package:mobile/screens/requirement_form_screen.dart';
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

  void _handleReviewRequirement(AiRequirementDraft draft) {
    if (widget.requirementGateway == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Requirement creation service is unavailable.'),
        ),
      );
      return;
    }

    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (context) => RequirementFormScreen(
          gateway: widget.requirementGateway!,
          initialCategoryId: draft.categoryId,
          aiPrefill: AiRequirementPrefill(draft: draft),
        ),
      ),
    );
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
              itemCount: _messages.length + (_isLoading ? 1 : 0),
              itemBuilder: (context, index) {
                if (index == _messages.length && _isLoading) {
                  return _buildTypingIndicator(theme);
                }
                return _buildMessageBubble(_messages[index], theme);
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
              onReview: () => _handleReviewRequirement(msg.draft!),
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
                        label: const Text('Review Requirement'),
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

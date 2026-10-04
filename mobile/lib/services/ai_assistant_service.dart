import 'package:mobile/core/api_client.dart';

import '../models/ai_assistant_models.dart';

final class AiAssistantService {
  AiAssistantService({required this.apiClient});

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

    final resConvId =
        json['conversation_id'] as String? ?? conversationId ?? '';
    final resMessage = json['message'] as String? ?? '';
    final intent = json['intent'] as String?;

    List<AiCitation> citations = [];
    if (json['citations'] is List) {
      citations = (json['citations'] as List)
          .map((item) => AiCitation.fromJson(item as Map<String, dynamic>))
          .toList();
    }

    AiRequirementDraft? draft;
    if (json['requirement_draft'] is Map) {
      draft = AiRequirementDraft.fromJson(
        json['requirement_draft'] as Map<String, dynamic>,
      );
    }

    AiClientAction? clientAction;
    if (json['client_action'] is Map) {
      clientAction = AiClientAction.fromJson(
        json['client_action'] as Map<String, dynamic>,
      );
    }

    List<String> actions = [];
    if (json['suggested_actions'] is List) {
      actions = (json['suggested_actions'] as List)
          .map((item) => item.toString())
          .toList();
    }

    return AiChatMessage(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      conversationId: resConvId,
      role: ChatRole.assistant,
      content: resMessage,
      intent: intent,
      citations: citations,
      draft: draft,
      clientAction: clientAction,
      suggestedActions: actions,
      timestamp: DateTime.now(),
    );
  }
}

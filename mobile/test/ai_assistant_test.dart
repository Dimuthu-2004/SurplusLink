import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/models/ai_assistant_models.dart';

void main() {
  group('AI Assistant Models Test Suite', () {
    test('AiCitation JSON parsing', () {
      final json = {
        'title': 'Cement Storage Guide',
        'source': 'SLS 107',
        'url': 'https://slsi.lk',
        'section': 'Storage',
      };
      final citation = AiCitation.fromJson(json);
      expect(citation.title, equals('Cement Storage Guide'));
      expect(citation.source, equals('SLS 107'));
      expect(citation.url, equals('https://slsi.lk'));
      expect(citation.section, equals('Storage'));
    });

    test('AiRequirementDraft JSON parsing for package mode', () {
      final json = {
        'template_id': 'tpl-123',
        'item_name': 'Paint',
        'input_mode': 'PACKAGE',
        'entered_quantity': 2,
        'entered_unit': 'cans (4L)',
        'normalized_quantity': 8.0,
        'normalized_base_unit': 'L',
        'preferences': {'colour': 'Yellow'},
        'location_text': 'Negombo',
        'maximum_budget': 100000,
        'deadline': '2030-01-01T00:00:00Z',
        'delivery_required': true,
        'notes': null,
        'missing_required_fields': [],
        'ready_for_review': true,
      };

      final draft = AiRequirementDraft.fromJson(json);
      expect(draft.templateId, equals('tpl-123'));
      expect(draft.itemName, equals('Paint'));
      expect(draft.inputMode, equals('PACKAGE'));
      expect(draft.enteredQuantity, equals(2.0));
      expect(draft.normalizedQuantity, equals(8.0));
      expect(draft.normalizedBaseUnit, equals('L'));
      expect(draft.preferences?['colour'], equals('Yellow'));
      expect(draft.locationText, equals('Negombo'));
      expect(draft.maximumBudget, equals(100000));
      expect(draft.deadline, equals('2030-01-01T00:00:00Z'));
      expect(draft.deliveryRequired, isTrue);
      expect(draft.readyForReview, isTrue);
    });

    test('AiChatMessage construction', () {
      final msg = AiChatMessage(
        id: '1',
        conversationId: 'c-1',
        role: ChatRole.assistant,
        content: 'Hello, how can I help?',
        timestamp: DateTime.now(),
      );

      expect(msg.id, equals('1'));
      expect(msg.role, equals(ChatRole.assistant));
      expect(msg.content, contains('how can I help'));
    });

    test(
      'workflow progress parses the persisted four-agent execution state',
      () {
        final progress = AiWorkflowProgress.fromJson({
          'workflowId': 'workflow-1',
          'requirementId': 'requirement-1',
          'status': 'RUNNING',
          'currentStage': 'LOGISTICS',
          'matchResultsReady': false,
          'errorCode': null,
          'startedAtUtc': '2030-01-01T00:00:00Z',
          'completedAtUtc': null,
          'steps': [
            {
              'stage': 'PLANNER',
              'status': 'COMPLETED',
              'errorCode': null,
              'retryCount': 0,
              'durationMilliseconds': 120,
              'toolCallCount': 0,
            },
            {
              'stage': 'MATCHING',
              'status': 'COMPLETED',
              'errorCode': null,
              'retryCount': 1,
              'durationMilliseconds': 340,
              'toolCallCount': 2,
            },
          ],
        });

        expect(progress.workflowId, 'workflow-1');
        expect(progress.requirementId, 'requirement-1');
        expect(progress.currentStage, 'LOGISTICS');
        expect(progress.isRunning, isTrue);
        expect(progress.matchResultsReady, isFalse);
        expect(progress.steps, hasLength(2));
        expect(progress.steps.last.toolCallCount, 2);
        expect(progress.steps.last.retryCount, 1);
      },
    );
  });
}

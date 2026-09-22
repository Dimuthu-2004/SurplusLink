import 'package:flutter/material.dart';
import 'package:mobile/core/api_exception.dart';

String matchError(Object error) => switch (error) {
  ApiException(statusCode: 403) => 'You do not have access to these matches.',
  ApiException(statusCode: 404) =>
    'These matches are not available. Return to your requirements or retry.',
  ApiException(statusCode: 401) =>
    'Your session has expired. Please sign in again.',
  ApiException(statusCode: 429) => 'Too many requests. Please wait and retry.',
  ApiException(:final message) => message,
  FormatException() => 'The server returned invalid match data. Please retry.',
  _ => 'Unable to load matches. Please retry.',
};

String matchDetailsError(Object error) {
  if (error is ApiException) {
    if (error.statusCode == 403) return 'You do not have access to this match.';
    if (error.statusCode == 404) return 'Match not found.';
    if (error.statusCode == 401) {
      return 'Your session has expired. Please sign in again.';
    }
    if (error.statusCode == 429) {
      return 'Too many requests. Please wait and retry.';
    }
    return 'Unable to load match details.';
  }
  if (error is FormatException) {
    return 'The server returned invalid match data. Please retry.';
  }
  return 'Unable to load match details.';
}

class MatchErrorBox extends StatelessWidget {
  const MatchErrorBox(this.message, {required this.onRetry, super.key});
  final String message;
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      Text(message, textAlign: TextAlign.center),
      OutlinedButton(onPressed: onRetry, child: const Text('Retry')),
    ],
  );
}

class MatchPagination extends StatelessWidget {
  const MatchPagination({
    required this.page,
    required this.totalPages,
    required this.busy,
    required this.onPage,
    super.key,
  });
  final int page, totalPages;
  final bool busy;
  final ValueChanged<int> onPage;
  @override
  Widget build(BuildContext context) => Wrap(
    alignment: WrapAlignment.center,
    crossAxisAlignment: WrapCrossAlignment.center,
    spacing: 12,
    children: [
      TextButton(
        onPressed: !busy && page > 1 ? () => onPage(page - 1) : null,
        child: const Text('Previous'),
      ),
      Text('Page $page of ${totalPages == 0 ? 1 : totalPages}'),
      TextButton(
        onPressed: !busy && page < totalPages ? () => onPage(page + 1) : null,
        child: const Text('Next'),
      ),
    ],
  );
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/core/api_exception.dart';

import 'requirement_models.dart';

String requirementError(Object error) {
  if (error is ApiException) {
    if (error.statusCode == 401) {
      return 'Your session has expired. Please sign in again.';
    }
    if (error.statusCode == 403) {
      return 'You do not have access to this requirement.';
    }
    if (error.statusCode == 404) return 'This requirement could not be found.';
    if (error.validationErrors?.isNotEmpty == true) {
      return error.validationErrors!.values.expand((items) => items).join('\n');
    }
    return error.message;
  }
  return 'Unable to complete the request. Check your connection and try again.';
}

String requirementDate(DateTime date) {
  final local = date.toLocal();
  return '${local.year}-${local.month.toString().padLeft(2, '0')}-${local.day.toString().padLeft(2, '0')} ${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
}

class RequirementErrorBox extends StatelessWidget {
  const RequirementErrorBox(this.message, {this.onRetry, super.key});
  final String message;
  final VoidCallback? onRetry;
  @override
  Widget build(BuildContext context) => Card(
    color: Theme.of(context).colorScheme.errorContainer,
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            message,
            key: const Key('requirement-error'),
            semanticsLabel: 'Error: $message',
          ),
          if (onRetry != null)
            TextButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
    ),
  );
}

class RequirementStatusChip extends StatelessWidget {
  const RequirementStatusChip(this.status, {super.key});
  final String status;
  @override
  Widget build(BuildContext context) => Chip(label: Text(statusLabel(status)));
}

class RequirementBackButton extends StatelessWidget {
  const RequirementBackButton({this.fallback = '/requirements', super.key});
  final String fallback;
  @override
  Widget build(BuildContext context) => BackButton(
    onPressed: () {
      if (context.canPop()) {
        context.pop();
      } else {
        context.go(fallback);
      }
    },
  );
}

class RequirementPager extends StatelessWidget {
  const RequirementPager({
    required this.page,
    required this.totalPages,
    required this.onPage,
    this.busy = false,
    super.key,
  });
  final int page, totalPages;
  final bool busy;
  final ValueChanged<int> onPage;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisAlignment: MainAxisAlignment.center,
    children: [
      IconButton(
        tooltip: 'Previous page',
        onPressed: busy || page <= 1 ? null : () => onPage(page - 1),
        icon: const Icon(Icons.chevron_left),
      ),
      Text('Page $page of ${totalPages == 0 ? '1' : '$totalPages'}'),
      IconButton(
        tooltip: 'Next page',
        onPressed: busy || page >= totalPages ? null : () => onPage(page + 1),
        icon: const Icon(Icons.chevron_right),
      ),
    ],
  );
}

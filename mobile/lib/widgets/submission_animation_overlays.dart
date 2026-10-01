import 'dart:async';

import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';

const _submittedAsset = 'assets/animations/submitted.json';
const _workflowAsset = 'assets/animations/workflow-running.json';

/// Presents one confirmed-success acknowledgement before the caller navigates.
/// The caller must invoke this only after its backend mutation has succeeded.
Future<void> showSubmittedAnimationOverlay(
  BuildContext context, {
  required String title,
  String? message,
}) => showDialog<void>(
  context: context,
  barrierDismissible: false,
  builder: (dialogContext) => SubmittedAnimationOverlay(
    title: title,
    message: message,
    onCompleted: () => Navigator.of(dialogContext).pop(),
  ),
);

class SubmittedAnimationOverlay extends StatefulWidget {
  const SubmittedAnimationOverlay({
    required this.title,
    this.message,
    this.onCompleted,
    super.key,
  });

  final String title;
  final String? message;
  final VoidCallback? onCompleted;

  @override
  State<SubmittedAnimationOverlay> createState() =>
      _SubmittedAnimationOverlayState();
}

class _SubmittedAnimationOverlayState extends State<SubmittedAnimationOverlay>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(vsync: this);
  Timer? _reducedMotionTimer;
  bool _completed = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (MediaQuery.disableAnimationsOf(context)) {
      _reducedMotionTimer ??= Timer(const Duration(milliseconds: 700), _finish);
    }
  }

  void _playOnce(Duration duration) {
    if (MediaQuery.disableAnimationsOf(context)) return;
    _controller
      ..duration = duration
      ..forward(from: 0).whenComplete(_finish);
  }

  void _finish() {
    if (_completed || !mounted) return;
    _completed = true;
    widget.onCompleted?.call();
  }

  @override
  void dispose() {
    _reducedMotionTimer?.cancel();
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: false,
    child: Dialog(
      insetPadding: const EdgeInsets.all(24),
      child: Semantics(
        liveRegion: true,
        label: widget.message == null
            ? widget.title
            : '${widget.title}. ${widget.message}',
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 28, 24, 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (MediaQuery.disableAnimationsOf(context))
                const Icon(Icons.check_circle_outline, size: 96)
              else
                SizedBox(
                  height: 210,
                  width: 210,
                  child: Lottie.asset(
                    _submittedAsset,
                    controller: _controller,
                    repeat: false,
                    onLoaded: (composition) => _playOnce(composition.duration),
                  ),
                ),
              const SizedBox(height: 12),
              Text(
                widget.title,
                key: const Key('submitted-animation-title'),
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleLarge,
              ),
              if (widget.message?.isNotEmpty == true) ...[
                const SizedBox(height: 8),
                Text(
                  widget.message!,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ],
          ),
        ),
      ),
    ),
  );
}

/// A non-dismissable visual state for an already-running backend workflow.
/// It deliberately owns no timers or request state: the backend status remains
/// authoritative and its parent removes this widget only when that status ends.
class WorkflowRunningOverlay extends StatelessWidget {
  const WorkflowRunningOverlay({
    required this.title,
    required this.message,
    super.key,
  });

  final String title;
  final String message;

  @override
  Widget build(BuildContext context) => ColoredBox(
    color: Colors.black54,
    child: Center(
      child: Semantics(
        liveRegion: true,
        label: '$title. $message',
        child: Card(
          margin: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 380),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (MediaQuery.disableAnimationsOf(context))
                    const Icon(Icons.auto_awesome, size: 96)
                  else
                    SizedBox(
                      height: 220,
                      width: 220,
                      child: Lottie.asset(_workflowAsset, repeat: true),
                    ),
                  const SizedBox(height: 16),
                  Text(
                    title,
                    key: const Key('workflow-running-title'),
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Text(message, textAlign: TextAlign.center),
                ],
              ),
            ),
          ),
        ),
      ),
    ),
  );
}

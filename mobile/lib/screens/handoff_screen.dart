import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/handoff/mobile_handoff_gateway.dart';
import 'package:mobile/handoff/mobile_handoff_models.dart';
import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

class HandoffScreen extends StatefulWidget {
  const HandoffScreen({
    required this.code,
    required this.authController,
    required this.handoffGateway,
    this.onRedeemed,
    super.key,
  });

  final String code;
  final AuthController authController;
  final MobileHandoffGateway handoffGateway;
  final void Function(MobileHandoffRedemption redemption)? onRedeemed;

  @override
  State<HandoffScreen> createState() => _HandoffScreenState();
}

class _HandoffScreenState extends State<HandoffScreen> {
  bool _loading = true;
  String? _error;
  bool _isWrongAccount = false;

  @override
  void initState() {
    super.initState();
    _processHandoff();
  }

  Future<void> _processHandoff() async {
    if (!widget.authController.isAuthenticated) {
      // User is not authenticated; router redirect handles sending to login with pending location
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
      _isWrongAccount = false;
    });

    try {
      final redemption = await widget.handoffGateway.redeem(widget.code);

      if (!mounted) return;

      // Switch marketplace mode to buyer so requirements flow is accessible
      widget.authController.marketplace.select(MarketplaceMode.buyer);

      if (widget.onRedeemed != null) {
        widget.onRedeemed!(redemption);
      } else {
        context.go('/requirements/new?categoryId=${redemption.categoryId}');
      }
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        if (e.statusCode == 403) {
          _isWrongAccount = true;
          _error = 'This handoff belongs to another account.';
        } else if (e.statusCode == 400) {
          _error = e.message.isNotEmpty
              ? e.message
              : 'This handoff has expired or is no longer valid.';
        } else if (e.statusCode == 404) {
          _error = 'Invalid handoff code.';
        } else {
          _error = e.message.isNotEmpty ? e.message : 'Unable to redeem handoff.';
        }
      });
    } on Object catch (_) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = 'An unexpected error occurred while redeeming handoff.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Mobile Handoff')),
        body: const Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircularProgressIndicator(),
              SizedBox(height: 16),
              Text(
                'Connecting to SurplusLink Marketplace...',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Mobile Handoff')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                _isWrongAccount
                    ? Icons.account_circle_outlined
                    : Icons.error_outline_rounded,
                size: 64,
                color: _isWrongAccount
                    ? SurplusLinkTheme.amber
                    : Theme.of(context).colorScheme.error,
              ),
              const SizedBox(height: 16),
              Text(
                _isWrongAccount ? 'Account Mismatch' : 'Handoff Issue',
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
              ),
              const SizedBox(height: 12),
              Text(
                _error ?? 'Unable to process this handoff request.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.grey.shade700, fontSize: 15),
              ),
              const SizedBox(height: 24),
              if (_isWrongAccount) ...[
                FilledButton.icon(
                  onPressed: () async {
                    await widget.authController.logout();
                    if (context.mounted) {
                      context.go('/login');
                    }
                  },
                  icon: const Icon(Icons.logout),
                  label: const Text('Log Out & Switch Account'),
                ),
                const SizedBox(height: 10),
              ],
              OutlinedButton(
                onPressed: () => context.go('/home'),
                child: const Text('Continue to Dashboard'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

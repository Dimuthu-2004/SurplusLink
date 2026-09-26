import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_controller.dart';
import 'package:mobile/handoff/qr_payload_parser.dart';
import 'package:mobile/theme/surplus_link_theme.dart';
import 'package:mobile/widgets/surplus_link_logo.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

class QrScannerScreen extends StatefulWidget {
  const QrScannerScreen({
    required this.authController,
    this.onCodeDetected,
    this.scannerWidget,
    super.key,
  });

  final AuthController authController;
  final void Function(String code)? onCodeDetected;
  final Widget? scannerWidget;

  @override
  State<QrScannerScreen> createState() => _QrScannerScreenState();
}

class _QrScannerScreenState extends State<QrScannerScreen>
    with SingleTickerProviderStateMixin {
  late final MobileScannerController _scannerController;
  late final AnimationController _pulseController;
  late final Animation<double> _pulseAnimation;

  bool _isProcessing = false;
  String? _errorMessage;
  final TextEditingController _manualInputController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _scannerController = MobileScannerController(
      detectionSpeed: DetectionSpeed.noDuplicates,
      facing: CameraFacing.back,
      torchEnabled: false,
    );

    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1400),
    );
    if (!WidgetsBinding.instance.runtimeType.toString().contains('Test')) {
      _pulseController.repeat(reverse: true);
    }

    _pulseAnimation = Tween<double>(begin: 0.98, end: 1.02).animate(
      CurvedAnimation(parent: _pulseController, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _scannerController.dispose();
    _pulseController.dispose();
    _manualInputController.dispose();
    super.dispose();
  }

  void _handleBarcode(BarcodeCapture capture) {
    if (_isProcessing) return;

    for (final barcode in capture.barcodes) {
      final raw = barcode.rawValue;
      if (raw == null) continue;

      final code = QrPayloadParser.parse(raw);
      if (code != null) {
        _onValidCode(code);
        return;
      }
    }

    if (mounted && _errorMessage == null) {
      setState(() {
        _errorMessage = 'Invalid QR code. Please scan a SurplusLink Web Handoff QR.';
      });
      Future.delayed(const Duration(seconds: 3), () {
        if (mounted) {
          setState(() {
            _errorMessage = null;
          });
        }
      });
    }
  }

  void _onValidCode(String code) {
    if (_isProcessing) return;
    setState(() {
      _isProcessing = true;
      _errorMessage = null;
    });

    _scannerController.stop();

    if (widget.onCodeDetected != null) {
      widget.onCodeDetected!(code);
      return;
    }

    if (!widget.authController.isAuthenticated) {
      // Unauthenticated: save pending route and redirect to login
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Handoff detected. Sign in with the same account to continue.'),
            backgroundColor: SurplusLinkTheme.amberDark,
            duration: Duration(seconds: 4),
          ),
        );
        context.go('/login', extra: '/handoff/$code');
      }
    } else {
      // Authenticated: navigate directly to handoff redemption
      if (mounted) {
        context.go('/handoff/$code');
      }
    }
  }

  void _showManualInputDialog() {
    _manualInputController.clear();
    showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Enter Handoff Code'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Enter the handoff code from the web QR modal or payload (e.g., SLH1:code or code):',
              style: TextStyle(fontSize: 13, color: SurplusLinkTheme.slate600),
            ),
            const SizedBox(height: 12),
            TextField(
              key: const Key('scan-qr-manual-field'),
              controller: _manualInputController,
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'Handoff Code or SLH1:...',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Cancel'),
          ),
          FilledButton(
            key: const Key('scan-qr-manual-submit'),
            onPressed: () {
              final raw = _manualInputController.text.trim();
              final code = QrPayloadParser.parse(raw);
              if (code != null) {
                Navigator.of(dialogContext).pop();
                _onValidCode(code);
              } else {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('Invalid code format. Try SLH1:<code or raw code.'),
                    backgroundColor: Colors.red,
                  ),
                );
              }
            },
            child: const Text('Continue'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black.withValues(alpha: 0.85),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.white),
          onPressed: () {
            if (context.canPop()) {
              context.pop();
            } else {
              context.go('/home');
            }
          },
        ),
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SurplusLinkLogo(size: 24, showWordmark: false),
            const SizedBox(width: 8),
            const Text(
              'Scan Web Handoff QR',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 18,
              ),
            ),
          ],
        ),
        actions: [
          ValueListenableBuilder<MobileScannerState>(
            valueListenable: _scannerController,
            builder: (context, state, child) {
              final isTorchOn = state.torchState == TorchState.on;
              return IconButton(
                icon: Icon(
                  isTorchOn ? Icons.flash_on : Icons.flash_off,
                  color: isTorchOn ? SurplusLinkTheme.amber : Colors.white70,
                ),
                onPressed: () => _scannerController.toggleTorch(),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.cameraswitch, color: Colors.white70),
            onPressed: () => _scannerController.switchCamera(),
          ),
        ],
      ),
      body: Stack(
        children: [
          // 1. Camera Viewfinder or injected scanner widget
          Positioned.fill(
            child: widget.scannerWidget ??
                MobileScanner(
                  controller: _scannerController,
                  onDetect: _handleBarcode,
                  errorBuilder: (context, error) {
                    return Center(
                      child: Padding(
                        padding: const EdgeInsets.all(24.0),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(
                              Icons.camera_alt_outlined,
                              size: 48,
                              color: Colors.white54,
                            ),
                            const SizedBox(height: 16),
                            const Text(
                              'Camera preview unavailable',
                              style: TextStyle(color: Colors.white70, fontSize: 16),
                            ),
                            const SizedBox(height: 12),
                            OutlinedButton.icon(
                              onPressed: _showManualInputDialog,
                              icon: const Icon(Icons.keyboard_alt_outlined),
                              label: const Text('Enter Code Manually'),
                              style: OutlinedButton.styleFrom(foregroundColor: Colors.white),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
          ),

          // 2. Viewfinder Overlay Frame with animated amber corners
          Center(
            child: ScaleTransition(
              scale: _pulseAnimation,
              child: Container(
                width: 260,
                height: 260,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: SurplusLinkTheme.amber.withValues(alpha: 0.85),
                    width: 2.5,
                  ),
                ),
                child: Stack(
                  children: [
                    // Corner accents
                    _Corner(top: 0, left: 0),
                    _Corner(top: 0, right: 0),
                    _Corner(bottom: 0, left: 0),
                    _Corner(bottom: 0, right: 0),
                  ],
                ),
              ),
            ),
          ),

          // 3. Top Instruction Banner
          Positioned(
            top: 24,
            left: 24,
            right: 24,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.65),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.white12),
              ),
              child: const Text(
                'Align the QR code from the SurplusLink web marketplace within the frame',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ),

          // 4. Processing overlay
          if (_isProcessing)
            Positioned.fill(
              child: Container(
                color: Colors.black.withValues(alpha: 0.75),
                child: const Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      CircularProgressIndicator(
                        valueColor: AlwaysStoppedAnimation<Color>(SurplusLinkTheme.amber),
                      ),
                      SizedBox(height: 16),
                      Text(
                        'Verifying Handoff Code...',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),

          // 5. Error banner
          if (_errorMessage != null)
            Positioned(
              bottom: 100,
              left: 20,
              right: 20,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                decoration: BoxDecoration(
                  color: Colors.red.shade900.withValues(alpha: 0.9),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.red.shade400),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline, color: Colors.white, size: 20),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        _errorMessage!,
                        style: const TextStyle(color: Colors.white, fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
            ),

          // 6. Bottom manual entry button
          Positioned(
            bottom: 30,
            left: 30,
            right: 30,
            child: TextButton.icon(
              key: const Key('scan-qr-manual-btn'),
              onPressed: _showManualInputDialog,
              icon: const Icon(Icons.keyboard_alt_outlined, color: Colors.white70),
              label: const Text(
                'Enter code manually',
                style: TextStyle(
                  color: Colors.white70,
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Corner extends StatelessWidget {
  const _Corner({
    this.top,
    this.bottom,
    this.left,
    this.right,
  });

  final double? top, bottom, left, right;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: top,
      bottom: bottom,
      left: left,
      right: right,
      child: Container(
        width: 24,
        height: 24,
        decoration: BoxDecoration(
          border: Border(
            top: top != null ? const BorderSide(color: SurplusLinkTheme.amberDark, width: 4) : BorderSide.none,
            bottom: bottom != null ? const BorderSide(color: SurplusLinkTheme.amberDark, width: 4) : BorderSide.none,
            left: left != null ? const BorderSide(color: SurplusLinkTheme.amberDark, width: 4) : BorderSide.none,
            right: right != null ? const BorderSide(color: SurplusLinkTheme.amberDark, width: 4) : BorderSide.none,
          ),
        ),
      ),
    );
  }
}

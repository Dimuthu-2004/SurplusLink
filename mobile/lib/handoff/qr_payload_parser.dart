class QrPayloadParser {
  /// Extracts the handoff code from a scanned QR payload.
  ///
  /// Supported formats:
  /// - `SLH1:<code` (recommended version 1 payload)
  /// - `surpluslink://handoff/<code>` (custom scheme path)
  /// - `surpluslink://handoff?code=<code>` (custom scheme query parameter)
  /// - Raw valid code (e.g. SLH-ABC12345 or hex token)
  ///
  /// Returns the sanitized code or `null` if invalid.
  static String? parse(String? raw) {
    if (raw == null) return null;
    final trimmed = raw.trim();
    if (trimmed.isEmpty) return null;

    // Format 1: SLH1:<code
    if (trimmed.startsWith('SLH1:')) {
      final code = trimmed.substring('SLH1:'.length).trim();
      return _isValidCode(code) ? code : null;
    }

    // Format 2: surpluslink://handoff/...
    if (trimmed.startsWith('surpluslink://')) {
      final uri = Uri.tryParse(trimmed);
      if (uri != null && uri.scheme == 'surpluslink') {
        if (uri.queryParameters.containsKey('code')) {
          final code = uri.queryParameters['code']!.trim();
          if (_isValidCode(code)) return code;
        }
        if (uri.pathSegments.isNotEmpty) {
          final code = uri.pathSegments.last.trim();
          if (_isValidCode(code)) return code;
        }
        if (uri.host == 'handoff' && uri.path.isNotEmpty) {
          final code = uri.path.replaceAll('/', '').trim();
          if (_isValidCode(code)) return code;
        }
      }
    }

    // Format 3: Raw handoff code (SLH- prefixed or alphanumeric token)
    if (_isValidCode(trimmed) && (trimmed.startsWith('SLH-') || trimmed.length >= 8)) {
      return trimmed;
    }

    return null;
  }

  static bool _isValidCode(String code) {
    if (code.length < 6 || code.length > 128) return false;
    final validChars = RegExp(r'^[A-Za-z0-9\-_]+$');
    return validChars.hasMatch(code);
  }
}

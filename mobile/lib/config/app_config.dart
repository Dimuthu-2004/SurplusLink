class AppConfig {
  AppConfig._();

  static const _configuredApiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://127.0.0.1:5170',
  );

  static Uri get apiBaseUri {
    final uri = Uri.parse(_configuredApiBaseUrl);
    if (!uri.hasScheme || (uri.scheme != 'http' && uri.scheme != 'https')) {
      throw StateError('API_BASE_URL must be an absolute HTTP or HTTPS URL.');
    }
    return uri;
  }
}

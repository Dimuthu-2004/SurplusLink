final class ApiException implements Exception {
  const ApiException(
    this.message, {
    this.statusCode,
    this.validationErrors,
    this.code,
  });

  final String message;
  final int? statusCode;
  final Map<String, List<String>>? validationErrors;
  final String? code;

  @override
  String toString() => message;
}

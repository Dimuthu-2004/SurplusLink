import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract interface class MarketplaceModeStorage {
  Future<String?> read(String userId);
  Future<void> write(String userId, String mode);
  Future<void> clear(String userId);
}

final class SecureMarketplaceModeStorage implements MarketplaceModeStorage {
  const SecureMarketplaceModeStorage();
  static const _storage = FlutterSecureStorage();
  String _key(String userId) => 'marketplace_mode:$userId';
  @override
  Future<String?> read(String userId) => _storage.read(key: _key(userId));
  @override
  Future<void> write(String userId, String mode) =>
      _storage.write(key: _key(userId), value: mode);
  @override
  Future<void> clear(String userId) => _storage.delete(key: _key(userId));
}

final class MemoryMarketplaceModeStorage implements MarketplaceModeStorage {
  final values = <String, String>{};
  @override
  Future<String?> read(String userId) async => values[userId];
  @override
  Future<void> write(String userId, String mode) async => values[userId] = mode;
  @override
  Future<void> clear(String userId) async => values.remove(userId);
}

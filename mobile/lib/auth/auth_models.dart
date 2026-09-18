enum AppRole {
  seller,
  buyer,
  manager;

  String get apiValue => name.toUpperCase();

  String get label => switch (this) {
    AppRole.seller => 'Seller',
    AppRole.buyer => 'Buyer',
    AppRole.manager => 'Manager',
  };

  static AppRole fromApi(String value) => switch (value.toUpperCase()) {
    'SELLER' => AppRole.seller,
    'BUYER' => AppRole.buyer,
    'MANAGER' => AppRole.manager,
    _ => throw FormatException('Unsupported user role: $value'),
  };
}

final class AppUser {
  const AppUser({
    required this.id,
    required this.email,
    required this.role,
    this.fullName,
    this.phoneNumber,
    this.businessName,
    this.address,
  });

  factory AppUser.fromJson(Map<String, dynamic> json) => AppUser(
    id: _requiredString(json, 'id'),
    email: _requiredString(json, 'email'),
    role: AppRole.fromApi(_requiredString(json, 'role')),
    fullName: json['fullName'] as String?,
    phoneNumber: json['phoneNumber'] as String?,
    businessName: json['businessName'] as String?,
    address: json['address'] as String?,
  );

  final String id;
  final String email;
  final AppRole role;
  final String? fullName, phoneNumber, businessName, address;
}

final class AuthSession {
  const AuthSession({required this.token, required this.user});

  factory AuthSession.fromJson(Map<String, dynamic> json) {
    final user = json['user'];
    if (user is! Map<String, dynamic>) {
      throw const FormatException('Missing or invalid user.');
    }
    return AuthSession(
      token: _requiredString(json, 'token'),
      user: AppUser.fromJson(user),
    );
  }

  final String token;
  final AppUser user;
}

String _requiredString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! String || value.isEmpty) {
    throw FormatException('Missing or invalid $key.');
  }
  return value;
}

class UserProfile {
  const UserProfile({
    required this.fullName,
    required this.phoneNumber,
    required this.address,
    this.businessName = '',
  });
  final String fullName, phoneNumber, businessName, address;
  Map<String, dynamic> toJson() => {
    'fullName': fullName.trim(),
    'phoneNumber': phoneNumber.trim(),
    'businessName': businessName.trim(),
    'address': address.trim(),
  };
}

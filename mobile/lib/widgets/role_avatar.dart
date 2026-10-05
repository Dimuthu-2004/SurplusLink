import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/config/app_config.dart';

class RoleAvatar extends StatelessWidget {
  const RoleAvatar({required this.user, this.size = 44, super.key});
  final AppUser user;
  final double size;

  Color get _borderColor => user.hasRole(AppRole.manager)
      ? Colors.orange
      : user.hasRole(AppRole.buyer) && user.hasRole(AppRole.seller)
      ? Colors.purple
      : user.hasRole(AppRole.seller)
      ? Colors.green
      : Colors.blue;

  String get _initials {
    final name = (user.fullName?.trim().isNotEmpty ?? false)
        ? user.fullName!.trim()
        : user.email;
    final words = name.split(RegExp(r'\s+'));
    return words.length > 1
        ? '${words.first[0]}${words[1][0]}'.toUpperCase()
        : name.substring(0, name.length < 2 ? 1 : 2).toUpperCase();
  }

  String? get _photoUrl {
    final path = user.profilePhotoUrl;
    if (path == null || path.trim().isEmpty || path.startsWith('file:')) return null;
    final uri = Uri.tryParse(path);
    return uri?.hasScheme == true ? path : AppConfig.apiBaseUri.resolve(path).toString();
  }

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    padding: const EdgeInsets.all(3),
    decoration: BoxDecoration(
      shape: BoxShape.circle,
      border: Border.all(color: _borderColor, width: 2.5),
      boxShadow: [BoxShadow(color: _borderColor.withValues(alpha: .35), blurRadius: 7)],
    ),
    child: ClipOval(
      child: _photoUrl == null
          ? ColoredBox(color: const Color(0xFFE2E8F0), child: Center(child: Text(_initials, style: TextStyle(fontWeight: FontWeight.w800, fontSize: size * .32))))
          : Image.network(_photoUrl!, fit: BoxFit.cover, errorBuilder: (_, _, _) => ColoredBox(color: const Color(0xFFE2E8F0), child: Center(child: Text(_initials, style: TextStyle(fontWeight: FontWeight.w800, fontSize: size * .32))))),
    ),
  );
}

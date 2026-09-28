import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class LocaleController extends ChangeNotifier {
  LocaleController({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();
  static const _key = 'surpluslink.locale';
  final FlutterSecureStorage _storage;
  Locale _locale = const Locale('en');
  Locale get locale => _locale;

  Future<void> load() async {
    final value = await _storage.read(key: _key);
    if (value == 'en' || value == 'si' || value == 'ta') {
      _locale = Locale(value!);
      notifyListeners();
    }
  }

  Future<void> select(Locale locale) async {
    if (_locale == locale) return;
    _locale = locale;
    notifyListeners();
    await _storage.write(key: _key, value: locale.languageCode);
  }
}

class LanguageSelector extends StatelessWidget {
  const LanguageSelector({required this.controller, super.key});
  final LocaleController controller;

  @override
  Widget build(BuildContext context) => Material(
        color: Colors.white.withValues(alpha: 0.92),
        borderRadius: BorderRadius.circular(18),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Row(mainAxisSize: MainAxisSize.min, children: [
            _choice('en', 'English'),
            _choice('si', 'සිංහල'),
            _choice('ta', 'தமிழ்'),
          ]),
        ),
      );

  Widget _choice(String code, String label) => TextButton(
        onPressed: () => controller.select(Locale(code)),
        child: Text(label, style: const TextStyle(fontSize: 11)),
      );
}

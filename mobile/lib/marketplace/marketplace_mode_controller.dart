import 'package:flutter/widgets.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/mutation_activity.dart';

import 'marketplace_mode.dart';
import 'marketplace_mode_storage.dart';

final class MarketplaceModeController extends ChangeNotifier {
  MarketplaceModeController({
    MarketplaceModeStorage? storage,
    MutationActivity? mutations,
  }) : _storage = storage ?? MemoryMarketplaceModeStorage(),
       mutations = mutations ?? MutationActivity() {
    this.mutations.addListener(_notify);
  }

  final MarketplaceModeStorage _storage;
  final MutationActivity mutations;
  AppUser? _user;
  MarketplaceMode? _activeMode;
  bool _saving = false;
  bool _disposed = false;
  int _revision = 0;
  Future<void> _writes = Future.value();
  String? persistenceError;

  MarketplaceMode? get activeMode => _activeMode;
  bool get isDualRole => isDualMarketplaceUser(_user);
  bool get canSwitch => isDualRole && !_saving && !mutations.isBusy;
  int get revision => _revision;

  Future<void> bind(AppUser? user) async {
    _user = user;
    _activeMode = availableMode(user);
    persistenceError = null;
    if (isDualRole) {
      try {
        final stored = await _storage.read(user!.id);
        if (!identical(_user, user)) return;
        final preferred = MarketplaceMode.values
            .where((m) => m.name == stored)
            .firstOrNull;
        _activeMode = availableMode(user, preferred);
      } on Object {
        persistenceError = 'Your saved mode could not be restored.';
      }
    }
    _notify();
  }

  Future<bool> select(MarketplaceMode mode) async {
    if (!canSwitch || mode == _activeMode) return false;
    final userId = _user!.id;
    _activeMode = mode;
    _revision++;
    _saving = true;
    persistenceError = null;
    _notify();
    // Serialize writes with logout so an older save cannot recreate its key.
    _writes = _writes.then((_) async {
      try {
        await _storage.write(userId, mode.name);
      } on Object {
        persistenceError =
            'Mode changed, but could not be saved on this device.';
      }
    });
    await _writes;
    _saving = false;
    _notify();
    return true;
  }

  Future<void> clear() async {
    final userId = _user?.id;
    _user = null;
    _activeMode = null;
    persistenceError = null;
    _notify();
    _writes = _writes.then((_) async {
      if (userId == null) return;
      try {
        await _storage.clear(userId);
      } on Object {
        // A local preference failure must not prevent logout.
      }
    });
    await _writes;
  }

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    mutations.removeListener(_notify);
    super.dispose();
  }
}

class MarketplaceModeScope
    extends InheritedNotifier<MarketplaceModeController> {
  const MarketplaceModeScope({
    required MarketplaceModeController controller,
    required super.child,
    super.key,
  }) : super(notifier: controller);

  static MarketplaceModeController? maybeOf(BuildContext context) => context
      .dependOnInheritedWidgetOfExactType<MarketplaceModeScope>()
      ?.notifier;
}

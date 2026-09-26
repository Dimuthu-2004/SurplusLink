import 'package:flutter/foundation.dart';

/// Observes requests without cancelling or modifying them.
final class MutationActivity extends ChangeNotifier {
  int _pending = 0;
  bool get isBusy => _pending > 0;

  Future<T> track<T>(Future<T> Function() request) async {
    _pending++;
    notifyListeners();
    try {
      return await request();
    } finally {
      _pending--;
      notifyListeners();
    }
  }
}

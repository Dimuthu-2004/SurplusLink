import 'mobile_handoff_models.dart';

abstract interface class MobileHandoffGateway {
  Future<MobileHandoffRedemption> redeem(String code);
}

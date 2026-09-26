import 'package:mobile/core/api_client.dart';
import 'mobile_handoff_gateway.dart';
import 'mobile_handoff_models.dart';

class MobileHandoffRepository implements MobileHandoffGateway {
  MobileHandoffRepository(this._api);
  final ApiClient _api;

  @override
  Future<MobileHandoffRedemption> redeem(String code) async {
    final data = await _api.postJson(
      '/api/mobile-handoffs/redeem',
      {'code': code.trim()},
      authenticated: true,
    );
    return MobileHandoffRedemption.fromJson(data);
  }
}

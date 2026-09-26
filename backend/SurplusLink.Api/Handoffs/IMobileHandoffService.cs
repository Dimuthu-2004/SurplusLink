namespace SurplusLink.Api.Handoffs;

public interface IMobileHandoffService
{
    Task<MobileHandoffResponse> CreateHandoffAsync(Guid userId, CreateMobileHandoffRequest request, CancellationToken ct);
    Task<RedeemMobileHandoffResponse> RedeemHandoffAsync(Guid actorUserId, RedeemMobileHandoffRequest request, CancellationToken ct);
    Task<MobileHandoffStatusResponse> GetStatusAsync(string code, CancellationToken ct);
}

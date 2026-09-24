using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public interface IMaterialInventoryService
{
    Task<MaterialListingResponse> CreateListingAsync(Guid sellerId, CreateMaterialListingRequest request, CancellationToken cancellationToken);
    Task<PagedMaterialListingsResponse> SearchListingsAsync(MaterialActor actor, MaterialListingQuery query, CancellationToken cancellationToken);
    Task<MaterialListingResponse> GetListingAsync(Guid listingId, MaterialActor actor, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaterialListingHistoryResponse>> GetListingHistoryAsync(Guid listingId, MaterialActor actor, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaterialListingResponse>> GetMyListingsAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<MaterialListingResponse> UpdateListingAsync(Guid sellerId, Guid listingId, UpdateMaterialListingRequest request, CancellationToken cancellationToken);
    Task DeleteListingAsync(Guid sellerId, Guid listingId, CancellationToken cancellationToken);
    Task<MaterialListingResponse> PublishListingAsync(Guid sellerId, Guid listingId, CancellationToken cancellationToken);
    Task<MaterialListingResponse> VerifyListingAsync(Guid managerId, Guid listingId, VerifyListingRequest request, CancellationToken cancellationToken);
    Task<MaterialAnalyticsSummaryResponse> GetAnalyticsSummaryAsync(MaterialAnalyticsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaterialCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetUnitCatalogAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetCategoryUnitsAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetActiveUnitsAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<MaterialCategoryResponse> CreateCategoryAsync(MaterialCategoryRequest request, CancellationToken cancellationToken);
    Task<MaterialCategoryResponse> UpdateCategoryAsync(Guid categoryId, MaterialCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken);
}

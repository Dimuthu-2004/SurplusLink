using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public interface IMaterialInventoryService
{
    Task<MaterialListingResponse> CreateListingAsync(Guid sellerId, CreateMaterialListingRequest request, CancellationToken cancellationToken);
    Task<MaterialListingResponse> GetListingAsync(Guid listingId, MaterialActor actor, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaterialListingResponse>> GetMyListingsAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<MaterialListingResponse> UpdateListingAsync(Guid sellerId, Guid listingId, UpdateMaterialListingRequest request, CancellationToken cancellationToken);
    Task DeleteListingAsync(Guid sellerId, Guid listingId, CancellationToken cancellationToken);
    Task<MaterialListingResponse> PublishListingAsync(Guid sellerId, Guid listingId, CancellationToken cancellationToken);
    Task<MaterialListingResponse> VerifyListingAsync(Guid managerId, Guid listingId, VerifyListingRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaterialCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<MaterialCategoryResponse> CreateCategoryAsync(MaterialCategoryRequest request, CancellationToken cancellationToken);
    Task<MaterialCategoryResponse> UpdateCategoryAsync(Guid categoryId, MaterialCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken);
}

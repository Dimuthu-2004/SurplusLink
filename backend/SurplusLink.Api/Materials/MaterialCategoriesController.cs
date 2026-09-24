using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

[ApiController]
[Route("api/material-categories")]
[Authorize]
public sealed class MaterialCategoriesController(IMaterialInventoryService service) : MaterialsControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SELLER,BUYER,MANAGER")]
    [ProducesResponseType<IReadOnlyList<MaterialCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaterialCategoryResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetCategoriesAsync(cancellationToken));

    [HttpGet("unit-catalog")]
    [Authorize(Roles = "SELLER,BUYER,MANAGER")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetUnitCatalog(CancellationToken cancellationToken) =>
        Ok(await service.GetUnitCatalogAsync(cancellationToken));

    [HttpGet("{categoryId:guid}/units")]
    [Authorize(Roles = "SELLER,BUYER,MANAGER")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetUnits(Guid categoryId, CancellationToken cancellationToken) =>
        Ok(await service.GetCategoryUnitsAsync(categoryId, cancellationToken));

    [HttpGet("{categoryId:guid}/active-units")]
    [Authorize(Roles = "SELLER,BUYER,MANAGER")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetActiveUnits(Guid categoryId, CancellationToken cancellationToken) =>
        Ok(await service.GetActiveUnitsAsync(categoryId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.MANAGER))]
    [ProducesResponseType<MaterialCategoryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MaterialCategoryResponse>> Create(
        MaterialCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.CreateCategoryAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.MANAGER))]
    [ProducesResponseType<MaterialCategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialCategoryResponse>> Update(
        Guid id,
        MaterialCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.UpdateCategoryAsync(id, request, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.MANAGER))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteCategoryAsync(id, cancellationToken);
            return NoContent();
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }
}

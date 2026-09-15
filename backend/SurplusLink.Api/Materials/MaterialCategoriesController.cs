using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

[ApiController]
[Route("api/material-categories")]
[Authorize(Roles = nameof(UserRole.MANAGER))]
public sealed class MaterialCategoriesController(IMaterialInventoryService service) : MaterialsControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MaterialCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaterialCategoryResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetCategoriesAsync(cancellationToken));

    [HttpPost]
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

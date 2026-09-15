using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

[ApiController]
[Route("api/materials")]
[Authorize]
public sealed class MaterialListingsController(IMaterialInventoryService service) : MaterialsControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SELLER,BUYER,MANAGER")]
    [ProducesResponseType<PagedMaterialListingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedMaterialListingsResponse>> Search(
        [FromQuery] MaterialListingQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            return Ok(await service.SearchListingsAsync(actor, query, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpGet("analytics/summary")]
    [Authorize(Roles = nameof(UserRole.MANAGER))]
    [ProducesResponseType<MaterialAnalyticsSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialAnalyticsSummaryResponse>> GetAnalyticsSummary(
        [FromQuery] MaterialAnalyticsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAnalyticsSummaryAsync(query, cancellationToken));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SELLER))]
    [ProducesResponseType<MaterialListingResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MaterialListingResponse>> Create(
        CreateMaterialListingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            var response = await service.CreateListingAsync(actor.Id, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SELLER,BUYER,MANAGER")]
    [ProducesResponseType<MaterialListingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialListingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            return Ok(await service.GetListingAsync(id, actor, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = "SELLER,MANAGER")]
    [ProducesResponseType<IReadOnlyList<MaterialListingHistoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaterialListingHistoryResponse>>> GetHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            return Ok(await service.GetListingHistoryAsync(id, actor, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SELLER))]
    [ProducesResponseType<MaterialListingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialListingResponse>> Update(
        Guid id,
        UpdateMaterialListingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            return Ok(await service.UpdateListingAsync(actor.Id, id, request, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SELLER))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            await service.DeleteListingAsync(actor.Id, id, cancellationToken);
            return NoContent();
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = nameof(UserRole.SELLER))]
    [ProducesResponseType<IReadOnlyList<MaterialListingResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaterialListingResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        return Ok(await service.GetMyListingsAsync(actor.Id, cancellationToken));
    }

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = nameof(UserRole.SELLER))]
    [ProducesResponseType<MaterialListingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialListingResponse>> Publish(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            return Ok(await service.PublishListingAsync(actor.Id, id, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }

    [HttpPatch("{id:guid}/verify")]
    [Authorize(Roles = nameof(UserRole.MANAGER))]
    [ProducesResponseType<MaterialListingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaterialListingResponse>> Verify(
        Guid id,
        VerifyListingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();
        try
        {
            return Ok(await service.VerifyListingAsync(actor.Id, id, request, cancellationToken));
        }
        catch (MaterialOperationException exception)
        {
            return MaterialError(exception);
        }
    }
}

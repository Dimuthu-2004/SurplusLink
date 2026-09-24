using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Api.Locations;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(IReverseGeocodingService service) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ReverseGeocodingResponse>>> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length is < 3 or > 400)
            return Problem(statusCode: 400, detail: "Enter an address between 3 and 400 characters.");
        try { return Ok(await service.SearchAsync(query.Trim(), cancellationToken)); }
        catch (ReverseGeocodingUnavailableException exception) { return Problem(statusCode: 503, detail: exception.Message); }
    }

    [HttpGet("reverse")]
    [ProducesResponseType<ReverseGeocodingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReverseGeocodingResponse>> Reverse(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        CancellationToken cancellationToken)
    {
        if (latitude is < -90 or > 90)
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Longitude must be between -180 and 180.");

        try
        {
            return Ok(await service.ReverseAsync(latitude, longitude, cancellationToken));
        }
        catch (ReverseGeocodingUnavailableException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: exception.Message);
        }
    }
}
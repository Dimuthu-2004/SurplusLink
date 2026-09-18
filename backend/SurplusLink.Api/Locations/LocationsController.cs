using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Api.Locations;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(IReverseGeocodingService service) : ControllerBase
{
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
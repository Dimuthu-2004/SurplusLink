using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Tests;

[ApiController]
[Route("_tests")]
public sealed class TestEndpointsController : ControllerBase
{
    [HttpGet("exception")]
    public IActionResult ThrowException() =>
        throw new InvalidOperationException("Sensitive exception detail must not be returned.");

    [HttpPost("validation")]
    public IActionResult Validate([FromBody] TestRequest request) => Ok(request);
}

public sealed class TestRequest
{
    [Required, MinLength(3)]
    public string? Name { get; init; }
}

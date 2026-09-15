using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public readonly record struct MaterialActor(Guid Id, UserRole Role);

public abstract class MaterialsControllerBase : ControllerBase
{
    protected bool TryGetActor(out MaterialActor actor)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (Guid.TryParse(subject, out var userId) && Enum.TryParse<UserRole>(role, out var userRole))
        {
            actor = new MaterialActor(userId, userRole);
            return true;
        }

        actor = default;
        return false;
    }

    protected ActionResult MaterialError(MaterialOperationException exception) => exception.Error switch
    {
        MaterialOperationError.Validation => BadRequest(new { message = exception.Message }),
        MaterialOperationError.NotFound => NotFound(new { message = exception.Message }),
        MaterialOperationError.Forbidden => Forbid(),
        MaterialOperationError.Conflict => Conflict(new { message = exception.Message }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}


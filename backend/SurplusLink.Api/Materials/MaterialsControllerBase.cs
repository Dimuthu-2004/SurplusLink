using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

public readonly record struct MaterialActor(Guid Id, IReadOnlySet<UserRole> Roles)
{
    public bool HasRole(UserRole role) => Roles.Contains(role);
}

public abstract class MaterialsControllerBase : ControllerBase
{
    protected bool TryGetActor(out MaterialActor actor)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var roles = Enum.GetValues<UserRole>().Where(role => User.IsInRole(role.ToString())).ToHashSet();
        if (Guid.TryParse(subject, out var userId) && roles.Count > 0)
        {
            actor = new MaterialActor(userId, roles);
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


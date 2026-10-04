using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Materials;

namespace SurplusLink.Api.Controllers;

[ApiController]
[Route("api/profile-photos")]
public sealed class ProfilePhotosController(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    SurplusLinkDbContext dbContext) : ControllerBase
{
    public const int MaximumBytes = 5 * 1024 * 1024;
    private string Root => configuration["Uploads:Directory"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "profile-photos");

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(MaximumBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumBytes + 65536)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();

        if (file.Length is <= 0 or > MaximumBytes)
            return BadRequest(new { message = "Choose a photo up to 5 MB." });

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        var extension = MaterialPhotosController.ImageExtension(bytes);
        if (extension is null)
            return BadRequest(new { message = "Choose a valid JPEG, PNG, or WebP photo." });

        var photoId = Guid.NewGuid();
        var directory = Path.Combine(Root, userId.ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{photoId:N}.{extension}");
        await System.IO.File.WriteAllBytesAsync(path, bytes, ct);

        var photoUrl = $"/api/profile-photos/{userId:D}/{photoId:D}/{extension}";

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound();

        user.ProfilePhotoUrl = photoUrl;
        await dbContext.SaveChangesAsync(ct);

        return Ok(new { photoUrl });
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Remove(CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound();

        user.ProfilePhotoUrl = null;
        await dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("{owner:guid}/{id:guid}/{extension}")]
    public IActionResult Download(Guid owner, Guid id, string extension)
    {
        var mime = extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            _ => null
        };
        if (mime is null) return NotFound();

        var path = Path.GetFullPath(Path.Combine(Root, owner.ToString("N"), $"{id:N}.{extension}"));
        if (!System.IO.File.Exists(path)) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public,max-age=86400,immutable";
        return PhysicalFile(path, mime);
    }
}

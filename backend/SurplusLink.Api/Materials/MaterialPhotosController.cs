using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SurplusLink.Api.Materials;

[ApiController]
[Route("api/material-photos")]
public sealed class MaterialPhotosController(IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    public const int MaximumBytes = 8 * 1024 * 1024;
    private string Root => configuration["Uploads:Directory"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "material-photos");

    [Authorize(Roles = "SELLER")]
    [HttpPost]
    [RequestSizeLimit(MaximumBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumBytes + 65536)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var owner)) return Unauthorized();
        if (file.Length is <= 0 or > MaximumBytes) return BadRequest(new { message = "Choose a photo up to 8 MB." });
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var extension = ImageExtension(bytes);
        if (extension is null) return BadRequest(new { message = "Choose a JPEG, PNG, or WebP photo." });
        // Never use a filename or directory supplied by the client.
        var id = Guid.NewGuid();
        var directory = Path.Combine(Root, owner.ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{id:N}.{extension}");
        await System.IO.File.WriteAllBytesAsync(path, bytes, cancellationToken);
        var photoUrl = $"/api/material-photos/{owner:D}/{id:D}/{extension}";
        return Created(photoUrl, new { photoUrl });
    }

    // Listing photos are shared media; only image bytes, never profile metadata, are exposed here.
    [HttpGet("{owner:guid}/{id:guid}/{extension}")]
    public IActionResult Download(Guid owner, Guid id, string extension)
    {
        var mime = extension switch { "jpg" => "image/jpeg", "png" => "image/png", "webp" => "image/webp", _ => null };
        if (mime is null) return NotFound();
        var path = Path.GetFullPath(Path.Combine(Root, owner.ToString("N"), $"{id:N}.{extension}"));
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public,max-age=86400,immutable";
        return PhysicalFile(path, mime);
    }

    internal static string? ImageExtension(byte[] bytes)
    {
        if (bytes.Length < 12) return null;
        if (bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff && bytes[^2] == 0xff && bytes[^1] == 0xd9) return "jpg";
        if (bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "png";
        if (bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }
}

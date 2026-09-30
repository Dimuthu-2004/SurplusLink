using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Materials;

[ApiController]
[Route("api/construction-item-templates")]
public sealed class ConstructionItemTemplatesController(SurplusLinkDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ConstructionItemTemplateResponse>>> GetTemplates(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? itemClass = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        IQueryable<ConstructionItemTemplate> query = dbContext.ConstructionItemTemplates
            .AsNoTracking()
            .Include(t => t.Category);

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(itemClass))
        {
            var normalizedClass = itemClass.Trim().ToUpperInvariant();
            query = query.Where(t => t.ItemClass == normalizedClass);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term) ||
                t.Category.Name.ToLower().Contains(term) ||
                t.ItemClass.ToLower().Contains(term));
        }

        var templates = await query
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return Ok(templates.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ConstructionItemTemplateResponse>> GetTemplateById(
        Guid id,
        CancellationToken ct = default)
    {
        var template = await dbContext.ConstructionItemTemplates
            .AsNoTracking()
            .Include(t => t.Category)
            .SingleOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
        {
            return NotFound(new { message = "Construction item template was not found." });
        }

        return Ok(ToResponse(template));
    }

    [HttpGet("resolve")]
    [AllowAnonymous]
    public async Task<ActionResult<TemplateMatchResolutionResponse>> ResolvePhrase(
        [FromQuery] string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Query phrase is required." });
        }

        var clean = query.Trim().ToLowerInvariant();
        var allTemplates = await dbContext.ConstructionItemTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(ct);

        // Deterministic heuristics based on keywords
        ConstructionItemTemplate? matched = null;
        decimal confidence = 0.5m;

        if (clean.Contains("generator") || clean.Contains("kva") || clean.Contains("genset"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Generator", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("paint") || clean.Contains("emulsion") || clean.Contains("enamel"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Paint", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("steel rod") || clean.Contains("rebar") || clean.Contains("steel bar") || clean.Contains("reinforcement") || Regex.IsMatch(clean, @"\b\d+mm\b.*steel"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Reinforcement Steel", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("cement") || clean.Contains("opc") || clean.Contains("ppc"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Cement", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("tile") && !clean.Contains("adhesive"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Tiles", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("compressor") || clean.Contains("air compressor"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Air Compressor", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("grinder") || clean.Contains("angle grinder"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Angle Grinder", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("pipe") || clean.Contains("pvc"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("PVC Pipes", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("roofing") || clean.Contains("zinc alum") || clean.Contains("corrugated"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Roofing Sheets", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else if (clean.Contains("sand") || clean.Contains("river sand") || clean.Contains("m-sand"))
        {
            matched = allTemplates.FirstOrDefault(t => t.Name.Equals("Sand", StringComparison.OrdinalIgnoreCase));
            confidence = 0.95m;
        }
        else
        {
            // Exact or substring match on template name
            matched = allTemplates.FirstOrDefault(t => clean.Contains(t.Name.ToLowerInvariant()));
            if (matched is not null)
            {
                confidence = 0.8m;
            }
        }

        if (matched is null)
        {
            return Ok(new TemplateMatchResolutionResponse(null, null, null, null, null, null, 0.0m));
        }

        return Ok(new TemplateMatchResolutionResponse(
            matched.Id,
            matched.Name,
            matched.ItemClass,
            matched.QuantityMode,
            matched.BaseUnit,
            matched.PackageType,
            confidence));
    }

    [HttpPost]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<ConstructionItemTemplateResponse>> CreateTemplate(
        [FromBody] CreateConstructionItemTemplateRequest request,
        CancellationToken ct = default)
    {
        var category = await dbContext.Categories.FindAsync([request.CategoryId], ct);
        if (category is null)
        {
            return BadRequest(new { message = "Category was not found." });
        }

        var template = new ConstructionItemTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Category = category,
            ItemClass = request.ItemClass.Trim().ToUpperInvariant(),
            QuantityMode = request.QuantityMode.Trim().ToUpperInvariant(),
            BaseUnit = request.BaseUnit.Trim().ToLowerInvariant(),
            PackageType = string.IsNullOrWhiteSpace(request.PackageType) ? null : request.PackageType.Trim().ToUpperInvariant(),
            AllowedUnits = request.AllowedUnits,
            AllowedPackageSizes = request.AllowedPackageSizes ?? [],
            BuyerInputModes = QuantitySemantics.ResolveBuyerInputModes(request.BuyerInputModes, request.QuantityMode),
            AttributeSchema = string.IsNullOrWhiteSpace(request.AttributeSchema) ? "[]" : request.AttributeSchema.Trim(),
            PriceBasis = string.IsNullOrWhiteSpace(request.PriceBasis) ? "PER_UNIT" : request.PriceBasis.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.ConstructionItemTemplates.Add(template);
        await dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, ToResponse(template));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<ConstructionItemTemplateResponse>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateConstructionItemTemplateRequest request,
        CancellationToken ct = default)
    {
        var template = await dbContext.ConstructionItemTemplates
            .Include(t => t.Category)
            .SingleOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
        {
            return NotFound(new { message = "Construction item template was not found." });
        }

        var category = await dbContext.Categories.FindAsync([request.CategoryId], ct);
        if (category is null)
        {
            return BadRequest(new { message = "Category was not found." });
        }

        template.Name = request.Name.Trim();
        template.CategoryId = request.CategoryId;
        template.Category = category;
        template.ItemClass = request.ItemClass.Trim().ToUpperInvariant();
        template.QuantityMode = request.QuantityMode.Trim().ToUpperInvariant();
        template.BaseUnit = request.BaseUnit.Trim().ToLowerInvariant();
        template.PackageType = string.IsNullOrWhiteSpace(request.PackageType) ? null : request.PackageType.Trim().ToUpperInvariant();
        template.AllowedUnits = request.AllowedUnits;
        template.AllowedPackageSizes = request.AllowedPackageSizes ?? [];
        template.BuyerInputModes = QuantitySemantics.ResolveBuyerInputModes(request.BuyerInputModes, request.QuantityMode);
        template.AttributeSchema = string.IsNullOrWhiteSpace(request.AttributeSchema) ? "[]" : request.AttributeSchema.Trim();
        template.PriceBasis = string.IsNullOrWhiteSpace(request.PriceBasis) ? "PER_UNIT" : request.PriceBasis.Trim().ToUpperInvariant();
        template.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        return Ok(ToResponse(template));
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<ConstructionItemTemplateResponse>> ToggleTemplateStatus(
        Guid id,
        [FromBody] ToggleTemplateStatusRequest request,
        CancellationToken ct = default)
    {
        var template = await dbContext.ConstructionItemTemplates
            .Include(t => t.Category)
            .SingleOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
        {
            return NotFound(new { message = "Construction item template was not found." });
        }

        template.IsActive = request.IsActive;
        template.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        return Ok(ToResponse(template));
    }

    private static ConstructionItemTemplateResponse ToResponse(ConstructionItemTemplate template) =>
        new(
            template.Id,
            template.Name,
            template.CategoryId,
            template.Category?.Name ?? string.Empty,
            template.ItemClass,
            template.QuantityMode,
            template.BaseUnit,
            template.PackageType,
            template.AllowedUnits,
            template.AllowedPackageSizes,
            QuantitySemantics.ResolveBuyerInputModes(template.BuyerInputModes, template.QuantityMode),
            template.AttributeSchema,
            template.PriceBasis,
            template.IsActive,
            template.CreatedAtUtc,
            template.UpdatedAtUtc);
}

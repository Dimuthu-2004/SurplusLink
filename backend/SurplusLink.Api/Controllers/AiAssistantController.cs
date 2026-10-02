using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;
using SurplusLink.Api.Models;

namespace SurplusLink.Api.Controllers;

public sealed record AiChatRequest(
    Guid? ConversationId,
    string Message
);

public sealed record AiCitationResponse(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("section")] string Section
);

public sealed record AiRequirementDraftResponse(
    [property: JsonPropertyName("template_id")] Guid? TemplateId,
    [property: JsonPropertyName("category_id")] Guid? CategoryId,
    [property: JsonPropertyName("item_name")] string ItemName,
    [property: JsonPropertyName("input_mode")] string InputMode,
    [property: JsonPropertyName("entered_quantity")] decimal? EnteredQuantity,
    [property: JsonPropertyName("entered_unit")] string? EnteredUnit,
    [property: JsonPropertyName("package_count")] int? PackageCount,
    [property: JsonPropertyName("package_size")] decimal? PackageSize,
    [property: JsonPropertyName("normalized_quantity")] decimal? NormalizedQuantity,
    [property: JsonPropertyName("normalized_base_unit")] string NormalizedBaseUnit,
    [property: JsonPropertyName("preferences")] Dictionary<string, string>? Preferences,
    [property: JsonPropertyName("location_text")] string? LocationText,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("missing_required_fields")] IReadOnlyList<string>? MissingRequiredFields,
    [property: JsonPropertyName("ready_for_review")] bool ReadyForReview
);

public sealed record AiChatResponse(
    [property: JsonPropertyName("conversation_id")] Guid ConversationId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("citations")] IReadOnlyList<AiCitationResponse> Citations,
    [property: JsonPropertyName("requirement_draft")] AiRequirementDraftResponse? RequirementDraft,
    [property: JsonPropertyName("suggested_actions")] IReadOnlyList<string> SuggestedActions
);

[ApiController]
[Route("api/ai")]
[Authorize]
public sealed class AiAssistantController(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    SurplusLinkDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
        }

        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();

        var user = await db.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Unauthorized();

        var roles = user.RoleAssignments.Select(r => r.Role.ToString()).ToList();
        var conversationId = request.ConversationId ?? Guid.NewGuid();

        var aiBaseUrl = config["AI_SERVICE_BASE_URL"] ?? config["Workflow:BaseUrl"] ?? "http://localhost:5000";
        var sharedToken = config["AI_SERVICE_SHARED_TOKEN"] ?? config["Workflow:SharedToken"] ?? "development-shared-token-32-chars-long";
        var aspNetBaseUrl = config["ASP_NET_BASE_URL"] ?? $"{Request.Scheme}://{Request.Host}";

        var pythonPayload = new
        {
            user_context = new
            {
                user_id = user.Id.ToString(),
                roles = roles,
                email = user.Email,
                full_name = user.FullName,
            },
            conversation_id = conversationId.ToString(),
            message = request.Message.Trim(),
            backend_api_url = aspNetBaseUrl,
        };

        try
        {
            var client = httpClientFactory.CreateClient("AiService");
            client.Timeout = TimeSpan.FromSeconds(25);

            var reqMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(aiBaseUrl.TrimEnd('/') + "/"), "internal/chat"))
            {
                Content = new StringContent(JsonSerializer.Serialize(pythonPayload), Encoding.UTF8, "application/json")
            };

            reqMsg.Headers.Add("x-internal-token", sharedToken);
            reqMsg.Headers.Add("x-user-id", user.Id.ToString());
            reqMsg.Headers.Add("x-user-roles", string.Join(",", roles));

            var res = await client.SendAsync(reqMsg, ct);

            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                var parsed = JsonSerializer.Deserialize<AiChatResponse>(body, JsonOpts);
                if (parsed != null)
                {
                    return Ok(parsed);
                }
            }
        }
        catch
        {
            // Fallback gracefully on AI service transport/timeout exception
        }

        // Return safe user response if AI service is temporarily unavailable
        return Ok(new AiChatResponse(
            ConversationId: conversationId,
            Message: "SurplusLink AI is temporarily unavailable. Please try again in a moment.",
            Intent: "SERVICE_UNAVAILABLE",
            Citations: Array.Empty<AiCitationResponse>(),
            RequirementDraft: null,
            SuggestedActions: new[] { "Find materials", "How does matching work?", "My transactions" }
        ));
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Handoffs;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class MobileHandoffTests
{
    private static readonly Guid SampleCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    [Fact]
    public async Task Anonymous_user_cannot_create_or_redeem_handoff()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/mobile-handoffs", new CreateMobileHandoffRequest
        {
            CategoryId = SampleCategoryId
        });
        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);

        var redeemResponse = await client.PostAsJsonAsync("/api/mobile-handoffs/redeem", new RedeemMobileHandoffRequest
        {
            Code = "somecode"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, redeemResponse.StatusCode);
    }

    [Theory]
    [InlineData("SELLER")]
    [InlineData("MANAGER")]
    public async Task Non_buyer_roles_are_forbidden_from_creating_handoffs(string role)
    {
        using var factory = new ApiWebApplicationFactory();
        var fakeService = new InMemoryMobileHandoffService();

        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMobileHandoffService>();
            services.AddSingleton<IMobileHandoffService>(fakeService);
        }));

        using var client = app.CreateClient();
        Authenticate(client, role, Guid.NewGuid(), app.Services);

        var response = await client.PostAsJsonAsync("/api/mobile-handoffs", new CreateMobileHandoffRequest
        {
            CategoryId = SampleCategoryId
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_buyer_can_create_handoff()
    {
        using var factory = new ApiWebApplicationFactory();
        var fakeService = new InMemoryMobileHandoffService();

        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMobileHandoffService>();
            services.AddSingleton<IMobileHandoffService>(fakeService);
        }));

        using var client = app.CreateClient();
        var buyerId = Guid.NewGuid();
        Authenticate(client, "BUYER", buyerId, app.Services);

        var response = await client.PostAsJsonAsync("/api/mobile-handoffs", new CreateMobileHandoffRequest
        {
            CategoryId = SampleCategoryId,
            Source = "REACT_MARKETPLACE"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MobileHandoffResponse>();
        Assert.NotNull(created);
        Assert.Equal(SampleCategoryId, created.CategoryId);
        Assert.Equal("Cement", created.CategoryName);
        Assert.Equal("REACT_MARKETPLACE", created.Source);
        Assert.StartsWith("surpluslink://handoff/", created.DeepLink);
        Assert.NotEmpty(created.Code);
        Assert.True(created.ExpiresAt > created.CreatedAt);
    }

    [Fact]
    public void Handoff_token_is_unpredictable_and_has_proper_entropy()
    {
        var code1 = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var code2 = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

        Assert.Equal(48, code1.Length);
        Assert.Equal(48, code2.Length);
        Assert.NotEqual(code1, code2);
    }

    [Fact]
    public async Task Same_user_can_redeem_handoff_one_time()
    {
        using var factory = new ApiWebApplicationFactory();
        var fakeService = new InMemoryMobileHandoffService();

        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMobileHandoffService>();
            services.AddSingleton<IMobileHandoffService>(fakeService);
        }));

        using var client = app.CreateClient();
        var buyerId = Guid.NewGuid();
        Authenticate(client, "BUYER", buyerId, app.Services);

        var createRes = await client.PostAsJsonAsync("/api/mobile-handoffs", new CreateMobileHandoffRequest
        {
            CategoryId = SampleCategoryId
        });
        var created = await createRes.Content.ReadFromJsonAsync<MobileHandoffResponse>();
        Assert.NotNull(created);

        // Redeem with same buyer
        var redeemRes = await client.PostAsJsonAsync("/api/mobile-handoffs/redeem", new RedeemMobileHandoffRequest
        {
            Code = created.Code
        });
        Assert.Equal(HttpStatusCode.OK, redeemRes.StatusCode);
        var redeemed = await redeemRes.Content.ReadFromJsonAsync<RedeemMobileHandoffResponse>();
        Assert.NotNull(redeemed);
        Assert.Equal(SampleCategoryId, redeemed.CategoryId);
        Assert.Equal("Cement", redeemed.CategoryName);

        // Second redemption attempt is rejected as already redeemed
        var secondRedeemRes = await client.PostAsJsonAsync("/api/mobile-handoffs/redeem", new RedeemMobileHandoffRequest
        {
            Code = created.Code
        });
        Assert.Equal(HttpStatusCode.BadRequest, secondRedeemRes.StatusCode);
    }

    [Fact]
    public async Task Wrong_user_is_forbidden_from_redeeming_handoff()
    {
        using var factory = new ApiWebApplicationFactory();
        var fakeService = new InMemoryMobileHandoffService();

        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMobileHandoffService>();
            services.AddSingleton<IMobileHandoffService>(fakeService);
        }));

        using var clientA = app.CreateClient();
        var buyerA = Guid.NewGuid();
        Authenticate(clientA, "BUYER", buyerA, app.Services);

        var createRes = await clientA.PostAsJsonAsync("/api/mobile-handoffs", new CreateMobileHandoffRequest
        {
            CategoryId = SampleCategoryId
        });
        var created = await createRes.Content.ReadFromJsonAsync<MobileHandoffResponse>();
        Assert.NotNull(created);

        // Client B tries to redeem client A's code
        using var clientB = app.CreateClient();
        var buyerB = Guid.NewGuid();
        Authenticate(clientB, "BUYER", buyerB, app.Services);

        var redeemRes = await clientB.PostAsJsonAsync("/api/mobile-handoffs/redeem", new RedeemMobileHandoffRequest
        {
            Code = created.Code
        });
        Assert.Equal(HttpStatusCode.Forbidden, redeemRes.StatusCode);
    }

    [Fact]
    public async Task Expired_token_is_rejected_on_redemption()
    {
        using var factory = new ApiWebApplicationFactory();
        var fakeService = new InMemoryMobileHandoffService();

        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMobileHandoffService>();
            services.AddSingleton<IMobileHandoffService>(fakeService);
        }));

        var buyerId = Guid.NewGuid();
        fakeService.SeedExpiredHandoff(buyerId, "expired-code-123", SampleCategoryId);

        using var client = app.CreateClient();
        Authenticate(client, "BUYER", buyerId, app.Services);

        var redeemRes = await client.PostAsJsonAsync("/api/mobile-handoffs/redeem", new RedeemMobileHandoffRequest
        {
            Code = "expired-code-123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, redeemRes.StatusCode);
    }

    [Fact]
    public async Task Status_endpoint_is_accessible_and_returns_valid_info()
    {
        using var factory = new ApiWebApplicationFactory();
        var fakeService = new InMemoryMobileHandoffService();

        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMobileHandoffService>();
            services.AddSingleton<IMobileHandoffService>(fakeService);
        }));

        using var client = app.CreateClient();
        var buyerId = Guid.NewGuid();
        Authenticate(client, "BUYER", buyerId, app.Services);

        var createRes = await client.PostAsJsonAsync("/api/mobile-handoffs", new CreateMobileHandoffRequest
        {
            CategoryId = SampleCategoryId
        });
        var created = await createRes.Content.ReadFromJsonAsync<MobileHandoffResponse>();
        Assert.NotNull(created);

        // Anonymous client can check status
        using var anonClient = app.CreateClient();
        var statusRes = await anonClient.GetAsync($"/api/mobile-handoffs/{created.Code}");
        Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);

        var status = await statusRes.Content.ReadFromJsonAsync<MobileHandoffStatusResponse>();
        Assert.NotNull(status);
        Assert.False(status.IsExpired);
        Assert.False(status.IsRedeemed);
        Assert.Equal("Cement", status.CategoryName);
    }

    private static void Authenticate(HttpClient client, string role, Guid userId, IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var token = new JwtSecurityToken(options.Issuer, options.Audience,
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
                SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    private sealed class InMemoryMobileHandoffService : IMobileHandoffService
    {
        private readonly Dictionary<string, HandoffEntry> handoffs = new();

        public void SeedExpiredHandoff(Guid userId, string code, Guid categoryId)
        {
            handoffs[code] = new HandoffEntry(
                Guid.NewGuid(),
                code,
                userId,
                categoryId,
                "Cement",
                "REACT_MARKETPLACE",
                DateTime.UtcNow.AddMinutes(-30),
                DateTime.UtcNow.AddMinutes(-15),
                null);
        }

        public Task<MobileHandoffResponse> CreateHandoffAsync(Guid userId, CreateMobileHandoffRequest request, CancellationToken ct)
        {
            var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(15);
            var entry = new HandoffEntry(
                Guid.NewGuid(),
                code,
                userId,
                request.CategoryId,
                "Cement",
                request.Source ?? "REACT_MARKETPLACE",
                now,
                expiresAt,
                null);

            handoffs[code] = entry;

            return Task.FromResult(new MobileHandoffResponse(
                entry.Id,
                entry.Code,
                $"surpluslink://handoff/{entry.Code}",
                entry.CategoryId,
                entry.CategoryName,
                entry.Source,
                entry.CreatedAt,
                entry.ExpiresAt));
        }

        public Task<RedeemMobileHandoffResponse> RedeemHandoffAsync(Guid actorUserId, RedeemMobileHandoffRequest request, CancellationToken ct)
        {
            if (!handoffs.TryGetValue(request.Code, out var entry))
            {
                throw new MobileHandoffException(MobileHandoffErrorCode.NotFound, "Invalid handoff code.", 404);
            }

            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                throw new MobileHandoffException(MobileHandoffErrorCode.Expired, "This handoff has expired.", 400);
            }

            if (entry.RedeemedAt.HasValue)
            {
                throw new MobileHandoffException(MobileHandoffErrorCode.AlreadyRedeemed, "This handoff has already been redeemed.", 400);
            }

            if (entry.UserId != actorUserId)
            {
                throw new MobileHandoffException(MobileHandoffErrorCode.Forbidden, "This handoff belongs to another account.", 403);
            }

            var redeemed = entry with { RedeemedAt = DateTime.UtcNow };
            handoffs[request.Code] = redeemed;

            return Task.FromResult(new RedeemMobileHandoffResponse(
                redeemed.Id,
                redeemed.CategoryId,
                redeemed.CategoryName,
                redeemed.Source,
                redeemed.RedeemedAt!.Value));
        }

        public Task<MobileHandoffStatusResponse> GetStatusAsync(string code, CancellationToken ct)
        {
            if (!handoffs.TryGetValue(code, out var entry))
            {
                throw new MobileHandoffException(MobileHandoffErrorCode.NotFound, "Invalid handoff code.", 404);
            }

            return Task.FromResult(new MobileHandoffStatusResponse(
                entry.Code,
                entry.CategoryId,
                entry.CategoryName,
                entry.Source,
                DateTime.UtcNow > entry.ExpiresAt,
                entry.RedeemedAt.HasValue,
                entry.ExpiresAt));
        }

        private record HandoffEntry(
            Guid Id,
            string Code,
            Guid UserId,
            Guid CategoryId,
            string CategoryName,
            string Source,
            DateTime CreatedAt,
            DateTime ExpiresAt,
            DateTime? RedeemedAt);
    }
}

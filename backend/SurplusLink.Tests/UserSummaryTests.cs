using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Controllers;
using SurplusLink.Api.Models;
using Xunit;

namespace SurplusLink.Tests;

public sealed class UserSummaryTests(RequirementsDatabase fixture) : IClassFixture<RequirementsDatabase>
{
    [Fact]
    public async Task Anonymous_cannot_access_user_summary()
    {
        using var app = new ApiWebApplicationFactory();
        using var client = app.CreateClient();
        var response = await client.GetAsync("/api/users/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("BUYER")]
    [InlineData("SELLER")]
    public async Task Non_manager_cannot_access_user_summary(string role)
    {
        using var app = new ApiWebApplicationFactory();
        using var client = app.CreateClient();
        Authenticate(client, role, app.Services);
        var response = await client.GetAsync("/api/users/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [PostgresFact]
    public async Task Manager_can_access_summary_and_receive_valid_counts()
    {
        using var app = fixture.App();
        using var client = app.CreateClient();
        Authenticate(client, "MANAGER", app.Services);
        var response = await client.GetAsync("/api/users/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalUsers >= 0);
        Assert.True(summary.DualRoleUsers >= 0);
        Assert.True(summary.Managers >= 1);
    }

    [Fact]
    public void Dual_role_counting_logic_counts_users_with_both_roles()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), RoleAssignments = [new() { Role = UserRole.BUYER }] },
            new() { Id = Guid.NewGuid(), RoleAssignments = [new() { Role = UserRole.SELLER }] },
            new() { Id = Guid.NewGuid(), RoleAssignments = [new() { Role = UserRole.BUYER }, new() { Role = UserRole.SELLER }] },
            new() { Id = Guid.NewGuid(), RoleAssignments = [new() { Role = UserRole.MANAGER }] },
        };

        int totalUsers = users.Count;
        int buyers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.BUYER));
        int sellers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.SELLER));
        int dualRoleUsers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.BUYER) && u.RoleAssignments.Any(r => r.Role == UserRole.SELLER));
        int managers = users.Count(u => u.RoleAssignments.Any(r => r.Role == UserRole.MANAGER));

        Assert.Equal(4, totalUsers);
        Assert.Equal(2, buyers);
        Assert.Equal(2, sellers);
        Assert.Equal(1, dualRoleUsers);
        Assert.Equal(1, managers);
    }

    private static void Authenticate(HttpClient client, string role, IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var token = new JwtSecurityToken(options.Issuer, options.Audience,
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
                SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}

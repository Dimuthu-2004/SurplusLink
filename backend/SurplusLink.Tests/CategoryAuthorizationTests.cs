using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Materials;

namespace SurplusLink.Tests;

// Exercise real routing/JWT/authorization without a database dependency.
public sealed class CategoryAuthorizationTests
{
    [Theory]
    [InlineData("MANAGER")]
    [InlineData("SELLER")]
    [InlineData("BUYER")]
    public async Task Authenticated_roles_can_list_categories(string role)
    {
        using var factory = new ApiWebApplicationFactory();
        var service = DispatchProxy.Create<IMaterialInventoryService, CategoryServiceStub>();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMaterialInventoryService>();
            services.AddSingleton(service);
        }));
        using var client = app.CreateClient();
        Authenticate(client, role, app.Services);
        using var response = await client.GetAsync("/api/material-categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<MaterialCategoryResponse[]>();
        Assert.Equal("Cement", Assert.Single(categories!).Name);
        Assert.Equal(1, ((CategoryServiceStub)(object)service).Calls);
    }

    [Theory]
    [InlineData(null, "GET", 401)]
    [InlineData(null, "POST", 401)]
    [InlineData(null, "PUT", 401)]
    [InlineData(null, "DELETE", 401)]
    [InlineData("SELLER", "POST", 403)]
    [InlineData("SELLER", "PUT", 403)]
    [InlineData("SELLER", "DELETE", 403)]
    [InlineData("BUYER", "POST", 403)]
    [InlineData("BUYER", "PUT", 403)]
    [InlineData("BUYER", "DELETE", 403)]
    [InlineData("MANAGER", "POST", 201)]
    [InlineData("MANAGER", "PUT", 200)]
    [InlineData("MANAGER", "DELETE", 204)]
    public async Task Anonymous_is_blocked_and_only_manager_can_write(string? role, string method, int expected)
    {
        using var factory = new ApiWebApplicationFactory();
        var service = DispatchProxy.Create<IMaterialInventoryService, CategoryServiceStub>();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMaterialInventoryService>();
            services.AddSingleton(service);
        }));
        using var client = app.CreateClient();
        if (role is not null) Authenticate(client, role, app.Services);
        var path = "/api/material-categories" + (method is "PUT" or "DELETE" ? $"/{Guid.NewGuid()}" : "");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { name = "Cement", allowedUnits = new[] { "kg" } });
        using var response = await client.SendAsync(request);
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal(expected < 300 ? 1 : 0, ((CategoryServiceStub)(object)service).Calls);
    }

    [Theory]
    [InlineData("SELLER", false)]
    [InlineData("BUYER", false)]
    [InlineData("MANAGER", true)]
    [InlineData("BUYER", true)]
    public async Task Category_units_returns_a_string_array_including_empty_categories(string role, bool empty)
    {
        using var factory = new ApiWebApplicationFactory();
        var service = DispatchProxy.Create<IMaterialInventoryService, CategoryServiceStub>();
        ((CategoryServiceStub)(object)service).Units = empty ? [] : ["kg", "m2"];
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMaterialInventoryService>();
            services.AddSingleton(service);
        }));
        using var client = app.CreateClient();
        Authenticate(client, role, app.Services);
        using var response = await client.GetAsync($"/api/material-categories/{Guid.NewGuid()}/units");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(empty ? [] : new[] { "kg", "m2" }, await response.Content.ReadFromJsonAsync<string[]>());
    }

    [Fact]
    public void Units_normalize_case_and_whitespace_and_remove_empty_duplicates()
    {
        Assert.Equal(new[] { "kg", "square metre" }, MaterialUnits.Distinct([" KG ", "kg", "Kg", " square   METRE ", "square metre", " "]));
        Assert.Empty(MaterialUnits.Distinct([]));
        Assert.Equal("kg", MaterialUnits.Normalize(" KG "));
    }

    [Fact]
    public void Allowed_units_must_come_from_catalog_and_are_canonical()
    {
        Assert.Equal(new[] { "box", "kg", "l" }, MaterialUnits.ValidateAllowed([" KG ", "kg", " BOX ", "L"], MaterialUnits.Catalog));
        Assert.Throws<MaterialOperationException>(() => MaterialUnits.ValidateAllowed([], MaterialUnits.Catalog));
        Assert.Throws<MaterialOperationException>(() => MaterialUnits.ValidateAllowed(["arbitrary"], MaterialUnits.Catalog));
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

    public class CategoryServiceStub : DispatchProxy
    {
        public int Calls { get; private set; }
        public IReadOnlyList<string> Units { get; set; } = [];
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++;
            var category = new MaterialCategoryResponse(Guid.NewGuid(), "Cement", DateTime.UtcNow, DateTime.UtcNow);
            return targetMethod!.Name switch
            {
                nameof(IMaterialInventoryService.GetUnitCatalogAsync) => Task.FromResult(MaterialUnits.Catalog),
                nameof(IMaterialInventoryService.GetCategoryUnitsAsync) => Task.FromResult(Units),
                nameof(IMaterialInventoryService.GetCategoriesAsync) => Task.FromResult<IReadOnlyList<MaterialCategoryResponse>>([category]),
                nameof(IMaterialInventoryService.CreateCategoryAsync) => Task.FromResult(category),
                nameof(IMaterialInventoryService.UpdateCategoryAsync) => Task.FromResult(category),
                nameof(IMaterialInventoryService.DeleteCategoryAsync) => Task.CompletedTask,
                _ => throw new InvalidOperationException("Unexpected inventory operation in category authorization test.")
            };
        }
    }
}

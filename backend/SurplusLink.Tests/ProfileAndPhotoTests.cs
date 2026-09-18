using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Models;

namespace SurplusLink.Tests;

public sealed class ProfileAndPhotoTests
{
    private static readonly byte[] Photo = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j5o8AAAAASUVORK5CYII=");

    [Theory]
    [InlineData("", "0771234567", "Colombo", false)]
    [InlineData("A Seller", "bad", "Colombo", false)]
    [InlineData("A Seller", "+94 771234567", "", false)]
    [InlineData("A Seller", "+94 771234567", "Colombo", true)]
    public void Profile_fields_validate(string name, string phone, string address, bool valid)
    {
        var request = new RegisterRequest { FullName = name, PhoneNumber = phone, Address = address,
            Email = "seller@example.test", Password = "Password123!", Roles = ["SELLER"] };
        Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Fact]
    public void Account_response_includes_profile_but_not_password_and_photo_paths_reject_active_content()
    {
        var user = new User { FullName = "Seller Name", PhoneNumber = "0771234567", Address = "Private address", PasswordHash = "never-expose" };
        var json = JsonSerializer.Serialize(UserResponse.From(user));
        Assert.Contains("Seller Name", json);
        Assert.DoesNotContain("never-expose", json);
        var validator = new PhotoUrlAttribute();
        Assert.True(validator.IsValid($"/api/material-photos/{Guid.NewGuid()}/{Guid.NewGuid()}/png"));
        Assert.True(validator.IsValid("https://example.test/photo.jpg"));
        Assert.False(validator.IsValid("javascript:alert(1)"));
        Assert.False(validator.IsValid("/api/material-photos/../../secrets"));
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("BUYER", 403)]
    [InlineData("MANAGER", 403)]
    [InlineData("SELLER", 201)]
    public async Task Photo_upload_requires_seller_and_stores_real_image_bytes(string? role, int status)
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "surpluslink-photos-" + Guid.NewGuid().ToString("N")));
        try
        {
            using var factory = new ApiWebApplicationFactory();
            using var app = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Directory"] = root })));
            using var client = app.CreateClient();
            if (role is not null) Authenticate(client, app.Services, role);
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(Photo), "file", "../../untrusted-name.png");
            var response = await client.PostAsync("/api/material-photos", content);
            Assert.Equal(status, (int)response.StatusCode);
            if (status == 201)
            {
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
                var path = payload.GetProperty("photoUrl").GetString()!;
                Assert.StartsWith("/api/material-photos/", path);
                Assert.DoesNotContain("untrusted-name", path);
                using var reader = app.CreateClient();
                var image = await reader.GetAsync(path);
                Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
                Assert.Equal(Photo, await image.Content.ReadAsByteArrayAsync());
                Assert.Single(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
                using var bad = new MultipartFormDataContent();
                bad.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("<svg onload='alert(1)'></svg>")), "file", "fake.png");
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/material-photos", bad)).StatusCode);
                using var large = new MultipartFormDataContent();
                large.Add(new ByteArrayContent(new byte[MaterialPhotosController.MaximumBytes + 1]), "file", "large.png");
                var oversized = await client.PostAsync("/api/material-photos", large);
                Assert.Contains(oversized.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge });
            }
        }
        finally
        {
            var tempRoot = Path.GetFullPath(Path.GetTempPath());
            if (!root.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(root).StartsWith("surpluslink-photos-"))
                throw new InvalidOperationException("Unexpected test media directory.");
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void Authenticate(HttpClient client, IServiceProvider services, string role)
    {
        var options = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var token = new JwtSecurityToken(options.Issuer, options.Audience,
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}

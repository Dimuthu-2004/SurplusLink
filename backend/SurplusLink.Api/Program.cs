using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SurplusLink.Api.Auth;
using SurplusLink.Api.Configuration;
using SurplusLink.Api.Data;
using SurplusLink.Api.ErrorHandling;
using SurplusLink.Api.Models;
using SurplusLink.Api.Materials;
using SurplusLink.Api.Locations;
using SurplusLink.Api.Observability;
using SurplusLink.Api.Reservations;
using SurplusLink.Api.Routing;

var builder = WebApplication.CreateBuilder(args);

// The ASP.NET Core API is the sole public backend for web and mobile clients.
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
else
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        options.UseUtcTimestamp = true;
    });
}

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddTransient<RequestLoggingMiddleware>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://www.rfc-editor.org/rfc/rfc9110#name-400-bad-request",
                Instance = context.HttpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
builder.Services.AddMemoryCache();
builder.Services.AddRoutingProvider();
builder.Services.AddHttpClient<IReverseGeocodingService, NominatimReverseGeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/reverse");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SurplusLink/1.0 (+https://github.com/SurplusLink)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddOptions<ApiCorsOptions>()
    .BindConfiguration(ApiCorsOptions.SectionName)
    .Validate(
        options => options.AllowedOrigins.Length > 0,
        $"{ApiCorsOptions.SectionName}:AllowedOrigins must contain at least one origin.")
    .Validate(
        options => options.AllowedOrigins.All(ApiCorsOptions.IsValidOrigin),
        $"{ApiCorsOptions.SectionName}:AllowedOrigins must contain only absolute HTTP or HTTPS origins without paths or wildcards.")
    .ValidateOnStart();
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>()
    .Configure<IOptions<ApiCorsOptions>>((options, configuredOrigins) =>
    {
        options.AddPolicy(ApiCorsOptions.PolicyName, policy =>
            policy.WithOrigins(configuredOrigins.Value.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SurplusLink),
        "ConnectionStrings:SurplusLink must be supplied through environment configuration or user secrets.")
    .ValidateOnStart();
builder.Services.AddDbContext<SurplusLinkDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IOptions<DatabaseOptions>>().Value.SurplusLink));

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer must be configured.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience must be configured.")
    .Validate(options => options.Secret.Length >= 32, "Jwt:Secret must contain at least thirty-two characters.")
    .Validate(options => options.ExpirationMinutes > 0, "Jwt:ExpirationMinutes must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IMaterialInventoryService, MaterialInventoryService>();
builder.Services.AddScoped<SurplusLink.Api.Requirements.RequirementService>();
builder.Services.AddScoped<SurplusLink.Api.Matching.MatchService>();
builder.Services.AddScoped<SurplusLink.Api.Transactions.TransactionService>();
builder.Services.AddScoped<SurplusLink.Api.Workflows.AgentWorkflowService>();
builder.Services.AddScoped<SurplusLink.Api.Handoffs.IMobileHandoffService, SurplusLink.Api.Handoffs.MobileHandoffService>();
builder.Services.AddOptions<SurplusLink.Api.Workflows.WorkflowExecutionOptions>()
    .BindConfiguration("AgentWorkflow")
    .Configure(options =>
    {
        options.BaseUrl = builder.Configuration["AI_SERVICE_BASE_URL"] ?? options.BaseUrl;
        options.SharedToken = builder.Configuration["AI_SERVICE_SHARED_TOKEN"] ?? options.SharedToken;
    })
    .Validate(x => !x.Enabled || (x.SharedToken.Length >= 32 && !x.SharedToken.Any(char.IsControl) &&
        Uri.TryCreate(x.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)),
        "Valid internal service URL and token required.")
    .Validate(x => x.PollSeconds is >= 1 and <= 60 && x.TimeoutSeconds is >= 1 and <= 180 &&
        x.HttpTimeoutSeconds is >= 1 and <= 120 && x.MaxRetries is >= 0 and <= 3 && x.MaxCandidates is >= 1 and <= 20,
        "Workflow execution limits are out of range.")
    .ValidateOnStart();
builder.Services.AddHttpClient<SurplusLink.Api.Workflows.IAgentWorkflowClient, SurplusLink.Api.Workflows.AgentWorkflowClient>(
    client => { client.Timeout = Timeout.InfiniteTimeSpan; client.MaxResponseContentBufferSize = 2 * 1024 * 1024; })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
    .RedactLoggedHeaders(_ => true);
builder.Services.AddScoped<SurplusLink.Api.Workflows.WorkflowQueueProcessor>();
builder.Services.AddHostedService<SurplusLink.Api.Workflows.WorkflowExecutionWorker>();
builder.Services.AddScoped<SurplusLink.Api.Workflows.IRequirementWorkflowStarter,
    SurplusLink.Api.Workflows.PersistentRequirementWorkflowStarter>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, configuredJwt) =>
    {
        var jwt = configuredJwt.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter a JWT bearer token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(bearerScheme.Reference.Id, bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearerScheme] = Array.Empty<string>()
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

var routing = app.Services.GetRequiredService<RoutingOptions>();
if (!routing.CanRoute)
    app.Logger.LogWarning("Routing provider configuration is missing: Routing__Endpoint and/or Routing__ApiKey (OpenRouteService).");
else if (!routing.CanEstimate)
    app.Logger.LogWarning("Routing transport pricing is missing: Routing__BaseFee, Routing__CostPerKm, and Routing__CostPerMinute.");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SurplusLinkDbContext>();
    await dbContext.Database.MigrateAsync();
    await DevelopmentSeed.SeedAsync(scope.ServiceProvider, app.Environment);
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(ApiCorsOptions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
}).AllowAnonymous();
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var address in app.Urls)
    {
        var baseUrl = address.TrimEnd('/');
        app.Logger.LogInformation(
            "SurplusLink API ready | Swagger: {SwaggerUrl} | Health: {HealthUrl}",
            $"{baseUrl}/swagger",
            $"{baseUrl}/health");
    }
});

app.Run();

public partial class Program;

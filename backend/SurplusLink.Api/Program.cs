using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SurplusLink.Api.Configuration;
using SurplusLink.Api.Data;
using SurplusLink.Api.ErrorHandling;
using SurplusLink.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

// The ASP.NET Core API is the sole public backend for web and mobile clients.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

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

builder.Services.AddOptions<ApiCorsOptions>()
    .BindConfiguration(ApiCorsOptions.SectionName)
    .Validate(
        options => options.AllowedOrigins.Length > 0,
        $"{ApiCorsOptions.SectionName}:AllowedOrigins must contain at least one origin.")
    .Validate(
        options => options.AllowedOrigins.All(ApiCorsOptions.IsValidOrigin),
        $"{ApiCorsOptions.SectionName}:AllowedOrigins must contain only absolute HTTP or HTTPS origins without paths or wildcards.")
    .ValidateOnStart();

var allowedOrigins = builder.Configuration
    .GetSection($"{ApiCorsOptions.SectionName}:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiCorsOptions.PolicyName, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var jwtConfiguration = builder.Configuration.GetSection("Authentication:JwtBearer");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtConfiguration["Authority"];
        options.Audience = jwtConfiguration["Audience"];
        options.RequireHttpsMetadata = jwtConfiguration.GetValue("RequireHttpsMetadata", true);
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

builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SurplusLink),
        "ConnectionStrings:SurplusLink must be supplied through environment configuration or user secrets.")
    .ValidateOnStart();
builder.Services.AddDbContext<SurplusLinkDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IOptions<DatabaseOptions>>().Value.SurplusLink));

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
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

app.Run();

public partial class Program;

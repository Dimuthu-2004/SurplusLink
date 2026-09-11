using Microsoft.EntityFrameworkCore;
using SurplusLink.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// The ASP.NET Core API is the sole public backend for web and mobile clients.
builder.Services.AddControllers();
builder.Services.AddDbContext<SurplusLinkDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SurplusLink")));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

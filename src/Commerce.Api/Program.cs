using System.Text.Json;
using System.Text.Json.Serialization;
using Commerce.Application.Features.Support;
using Commerce.Infrastructure;
using Commerce.Infrastructure.Persistence;
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5080");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddFastEndpoints(o => o.Assemblies = [typeof(SubmitTicketCommand).Assembly])
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "CommercePilot API";
            s.DocumentName = "v1";
            s.Version = "v1";
        };
        o.ShortSchemaNames = true;
    });

string[] corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:3000"];
// Configured origins + any localhost port — Next.js hops to 3001+ when 3000 is taken.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(origin =>
        corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
        || (Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && uri.Host is "localhost" or "127.0.0.1"))
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

// Phase-1 static bearer key (decision 9): empty key = open local dev; JWT+RBAC arrives in Phase 4.
string? apiKey = app.Configuration["Auth:ApiKey"];
if (!string.IsNullOrWhiteSpace(apiKey))
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")
            && ctx.Request.Headers.Authorization != $"Bearer {apiKey}")
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        await next();
    });
}

app.UseFastEndpoints(c =>
{
    c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
app.UseSwaggerGen(); // serves /swagger (UI) + /swagger/v1/swagger.json (frontend codegen)

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

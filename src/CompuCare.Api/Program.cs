using System.Text;
using CompuCare.Api.IdentityAccess.Infrastructure;
using CompuCare.Application.Common;
using CompuCare.Application.IdentityAccess;
using CompuCare.Application.CompaniesEquipment;
using CompuCare.Application.SubscriptionsCoverage;
using CompuCare.Application.ServiceRequests;
using CompuCare.Application.QuotationsPayments;
using CompuCare.Infrastructure.Seeding;
using CompuCare.Domain.Common;
using CompuCare.Infrastructure;
using CompuCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("CompuCare");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Set ConnectionStrings__CompuCare (see .env.example).");
var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Set Jwt__SigningKey to a secret of at least 32 bytes.");

builder.Services.AddInfrastructure(connection);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<IRuntimeMode, RuntimeMode>();
builder.Services.AddScoped<IamService>();
builder.Services.AddScoped<CompanyEquipmentService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<ServiceRequestService>();
builder.Services.AddScoped<QuotationService>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppProblemHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CompuCare REST API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Paste the JWT obtained from POST /api/v1/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CompuCare.Api",
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CompuCare.Frontend",
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub", RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (origins.Length > 0)
    builder.Services.AddCors(o => o.AddPolicy("frontend", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
if (origins.Length > 0) app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = "CompuCare.Api", status = "healthy" })).AllowAnonymous();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:AutoCreateDevelopment"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CompuCareDbContext>();
    await db.Database.EnsureCreatedAsync(); // development-only bootstrap, migrations needed in production
    await DevelopmentSeeder.SeedPlansAsync(db);
    if (app.Configuration.GetValue<bool>("DemoSeed:Enabled"))
    {
        var password = app.Configuration["DemoSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set DemoSeed__Password in local, untracked environment settings.");
        await DevelopmentSeeder.SeedDemoAccountsAsync(db,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(), password);
    }
}
app.Run();
public partial class Program { }

public sealed class AppProblemHandler(ILogger<AppProblemHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var code = error switch {
            AppException app => app.StatusCode,
            DomainRuleException => 400,
            DbUpdateConcurrencyException => 409,
            _ => 500
        };
        if (code >= 500) logger.LogError(error, "API request failed");
        else logger.LogWarning("Request rejected: {Reason}", error.Message);
        context.Response.StatusCode = code;
        var details = new ProblemDetails {
            Status = code,
            Title = code == 500 ? "Internal server error" : error.Message,
            Detail = code == 500 ? "Unexpected error. Contact support." : error.Message,
            Instance = context.Request.Path
        };
        await context.Response.WriteAsJsonAsync(details, cancellationToken: ct);
        return true;
    }
}

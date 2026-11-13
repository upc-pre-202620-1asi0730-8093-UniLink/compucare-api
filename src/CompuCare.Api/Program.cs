using CompuCare.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var databaseConnection = builder.Configuration.GetConnectionString("CompuCare");
if (string.IsNullOrWhiteSpace(databaseConnection))
    throw new InvalidOperationException(
        "Missing database configuration. Set ConnectionStrings__CompuCare (see .env.example).");

builder.Services.AddInfrastructure(databaseConnection);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("CompuCareFrontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
if (allowedOrigins.Length > 0) app.UseCors("CompuCareFrontend");

// Health checks are public. No business endpoints are exposed until authentication
// and tenant/role-specific policies have been implemented and tested.
app.MapGet("/health", () => Results.Ok(new { service = "CompuCare.Api", status = "healthy" }))
    .WithName("Health")
    .WithTags("Operations");

app.Run();

public partial class Program { }

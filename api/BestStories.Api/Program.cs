using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // The document; Scalar renders it. Root redirects so a reviewer landing on
    // the host sees the API reference rather than a 404.
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.Run();

// Exposes the implicit Program class of top-level statements to WebApplicationFactory<Program>.
public partial class Program { }

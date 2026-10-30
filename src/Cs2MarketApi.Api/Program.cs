using Cs2MarketApi.Application.Exceptions;
using Cs2MarketApi.Application.Services;
using Cs2MarketApi.Infrastructure;
using Cs2MarketApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<InventoryValuationService>();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// create tables on startup (switch to EF migrations later)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try { db.Database.EnsureCreated(); break; }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning("Database is not ready ({Message}), retry {Attempt}/10", ex.Message, attempt);
            Thread.Sleep(3000);
        }
    }
}

// domain exceptions -> proper HTTP status codes
app.UseExceptionHandler(errors => errors.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = ex switch
    {
        SteamNotFoundException => (404, "Steam user not found"),
        InventoryPrivateException => (403, "Inventory is private"),
        ExternalServiceException => (502, "External service error"),
        _ => (500, "Unexpected error")
    };

    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = status == 500 ? null : ex?.Message
    });
}));

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

app.Run();

public partial class Program;

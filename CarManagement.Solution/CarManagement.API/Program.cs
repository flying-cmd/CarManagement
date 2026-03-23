using CarManagement.API.Middlewares;
using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagement.Repository.Repositories;
using CarManagement.Service.Interfaces;
using CarManagement.Service.Services;
using CarManagementApi.Repository.Interfaces;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder();

// Sqlite
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();

//Password hasher
builder.Services.AddSingleton<IPasswordHasher<Dealer>, PasswordHasher<Dealer>>();

//Jwt
builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = builder.Configuration["Jwt:SigningKey"])
    .AddAuthorization();

// Repositories
builder.Services.AddScoped<IDealerRepository, DealerRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddFastEndpoints();

builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Car Management API";
        s.Version = "v1";
    };
});

var app = builder.Build();

// Global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

app.UseAuthentication();

app.UseAuthorization();

app.UseFastEndpoints();

app.UseSwaggerGen();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
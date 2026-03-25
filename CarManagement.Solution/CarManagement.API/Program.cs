using CarManagement.API.Middlewares;
using CarManagement.Common.Extensions;
using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using CarManagement.Repository.Repositories;
using CarManagement.Service.Interfaces;
using CarManagement.Service.Mappers;
using CarManagement.Service.Services;
using CarManagementApi.Repository.Interfaces;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder();

// Sqlite
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();

// Password hasher
builder.Services.AddSingleton<IPasswordHasher<Dealer>, PasswordHasher<Dealer>>();

// Jwt authentication and authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// UserContext
builder.Services.AddScoped<IUserContext, UserContext>();

// Mapper
builder.Services.AddSingleton<CarMapper>();

// Repositories
builder.Services.AddScoped<IDealerRepository, DealerRepository>();
builder.Services.AddScoped<ICarRepository, CarRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddFastEndpoints();

// Swagger
builder.Services.AddAppSwagger();

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

// Used for integration tests
public partial class Program { }

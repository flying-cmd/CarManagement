using FastEndpoints;
using CarManagement.Common.Utilities;
using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagement.Repository.Repositories;
using CarManagement.Service.Interfaces;
using CarManagementApi.Repository.Interfaces;
using FastEndpoints.Security;
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

builder.Services.AddFastEndpoints();

var app = builder.Build();

app.UseAuthentication();

app.UseAuthorization();

app.UseFastEndpoints();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
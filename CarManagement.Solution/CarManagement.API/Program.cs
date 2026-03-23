global using FastEndpoints;
global using FluentValidation;
using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder();

// Sqlite
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();

// Password hasher
builder.Services.AddSingleton<IPasswordHasher<Dealer>, PasswordHasher<Dealer>>();

builder.Services.AddFastEndpoints();

var app = builder.Build();
app.UseFastEndpoints();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
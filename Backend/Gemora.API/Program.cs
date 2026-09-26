using System.Text;
using Gemora.API.Middleware;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE CONFIGURATION
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// ============================================================
// APPLICATION SERVICES / DEPENDENCY INJECTION
// ============================================================

// Authentication service
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT token service
builder.Services.AddScoped<TokenService>();

// Gem Listing service - Component 1
builder.Services.AddScoped<IGemListingService, GemListingService>();

// Gem Verification service - Component 2
builder.Services.AddScoped<IGemVerificationService, GemVerificationService>();

// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();


// ============================================================
// JWT AUTHENTICATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "JWT issuer is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT audience is not configured.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });


// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowReact",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


// ============================================================
// SWAGGER / OPENAPI
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Gemora Marketplace API",
            Version = "v1",
            Description =
                "ASP.NET Core Web API for the Gemora Marketplace."
        });

    // Add JWT Bearer authentication support to Swagger
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter your JWT token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// GLOBAL EXCEPTION HANDLER
// ============================================================

// This catches exceptions from controllers/services.
//
// InvalidOperationException   -> 409 Conflict
// UnauthorizedAccessException -> 403 Forbidden
// KeyNotFoundException       -> 404 Not Found
// Other exceptions           -> 500 Internal Server Error

app.UseMiddleware<GlobalExceptionHandler>();


// ============================================================
// SWAGGER
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ============================================================
// HTTPS
// ============================================================

// We are currently developing/testing the API over localhost HTTP.
// Do NOT enable UseHttpsRedirection here yet if Swagger/React/Flutter
// are calling http://localhost:5198.
//
// app.UseHttpsRedirection();


// ============================================================
// CORS
// ============================================================

app.UseCors("AllowReact");


// ============================================================
// AUTHENTICATION + AUTHORIZATION
// ============================================================

// Authentication MUST come before Authorization.

app.UseAuthentication();

app.UseAuthorization();


// ============================================================
// DATABASE SEEDING
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await DbSeeder.SeedAsync(
        dbContext,
        builder.Configuration);
}


// ============================================================
// MAP CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// RUN APPLICATION
// ============================================================

app.Run();
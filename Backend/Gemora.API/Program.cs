using Gemora.API.Middleware;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Infrastructure.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Text;

var builder = WebApplication.CreateBuilder(args);


// ======================================================
// 1. DATABASE - PostgreSQL + Entity Framework Core
// ======================================================
// The actual connection string is stored securely in
// .NET User Secrets during local development.
// ======================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseNpgsql(connectionString)
);


// ======================================================
// 2. DEPENDENCY INJECTION
// ======================================================

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IExportComplianceService, ExportComplianceService>();

builder.Services.AddScoped<IComplianceRulesService, ComplianceRulesService>();

builder.Services.AddScoped<TokenService>();


// ======================================================
// 3. CONTROLLERS
// ======================================================

builder.Services.AddControllers();


// ======================================================
// 4. JWT CONFIGURATION
// ======================================================
// JWT Key comes from .NET User Secrets.
// Issuer and Audience come from appsettings.json.
// ======================================================

var jwtKey =
    builder.Configuration["Jwt:Key"];

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    builder.Configuration["Jwt:Audience"];


if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is not configured."
    );
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "JWT Issuer is not configured."
    );
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT Audience is not configured."
    );
}


// ======================================================
// 5. JWT AUTHENTICATION
// ======================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Verify who created the token
                ValidateIssuer = true,

                // Verify who the token is intended for
                ValidateAudience = true,

                // Reject expired tokens
                ValidateLifetime = true,

                // Verify the token signature
                ValidateIssuerSigningKey = true,

                // Expected issuer
                ValidIssuer = jwtIssuer,

                // Expected audience
                ValidAudience = jwtAudience,

                // Secret signing key
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                // Token expires exactly at expiration time
                ClockSkew = TimeSpan.Zero
            };
    });


// ======================================================
// 6. AUTHORIZATION
// ======================================================

builder.Services.AddAuthorization();


// ======================================================
// 7. CORS
// ======================================================
// React/Vite development application:
// http://localhost:5173
//
// Flutter does not have browser CORS restrictions in
// the same way, but it will use the same ASP.NET API.
// ======================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "GemoraCorsPolicy",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});


// ======================================================
// 8. SWAGGER / OPENAPI
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // ----------------------------------------------
    // JWT Bearer authentication in Swagger
    // ----------------------------------------------

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
        }
    );


    // ----------------------------------------------
    // Add Bearer authentication to Swagger requests
    // ----------------------------------------------

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
        }
    );
});


// ======================================================
// 9. BUILD APPLICATION
// ======================================================

var app = builder.Build();


// ======================================================
// 10. SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ======================================================
// 11. GLOBAL EXCEPTION HANDLING
// ======================================================

app.UseMiddleware<GlobalExceptionHandler>();


// ======================================================
// 12. CORS
// ======================================================

app.UseCors("GemoraCorsPolicy");


// ======================================================
// 13. AUTHENTICATION
// ======================================================
// Authentication must run before authorization.
// ======================================================

app.UseAuthentication();


// ======================================================
// 14. AUTHORIZATION
// ======================================================

app.UseAuthorization();


// ======================================================
// 15. DATABASE SEEDING
// ======================================================
// Staff passwords are NOT stored here.
//
// They are read from:
//
// SeedUsers:AdminPassword
// SeedUsers:GemologistPassword
// SeedUsers:ExportOfficerPassword
//
// These values are stored in .NET User Secrets for
// local development.
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await DbSeeder.SeedAsync(
        dbContext,
        builder.Configuration
    );
}


// ======================================================
// 16. MAP CONTROLLERS
// ======================================================

app.MapControllers();


// ======================================================
// 17. START APPLICATION
// ======================================================

app.Run();
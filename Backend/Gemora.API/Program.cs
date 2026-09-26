using System.Text;
using Gemora.API.Middleware;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Gemora.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder =
    WebApplication.CreateBuilder(args);


// ============================================================
// DATABASE
// ============================================================

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is not configured.");

builder.Services
    .AddDbContext<ApplicationDbContext>(
        options =>
        {
            options.UseNpgsql(
                connectionString);
        });


// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    TokenService>();

builder.Services.AddScoped<
    IGemListingService,
    GemListingService>();

builder.Services.AddScoped<
    IGemVerificationService,
    GemVerificationService>();


// ============================================================
// FILE STORAGE
//
// IMPORTANT:
//
// LocalFileStorageService must save files inside the actual
// Gemora.API wwwroot directory.
//
// We therefore obtain the path from IWebHostEnvironment instead
// of using Directory.GetCurrentDirectory().
// ============================================================

builder.Services.AddScoped<IFileStorageService>(
    serviceProvider =>
    {
        var environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment>();


        // Normally this resolves to:
        //
        // ...\Backend\Gemora.API\wwwroot
        //
        // If WebRootPath has not been initialized yet,
        // fall back to ContentRootPath + wwwroot.
        var webRootPath =
            environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(
                webRootPath))
        {
            webRootPath =
                Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot");
        }


        // Ensure wwwroot exists.
        Directory.CreateDirectory(
            webRootPath);


        var uploadRoot =
            Path.Combine(
                webRootPath,
                "uploads");


        Directory.CreateDirectory(
            uploadRoot);


        return new LocalFileStorageService(
            uploadRoot);
    });


// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();


// ============================================================
// CORS
//
// React development server:
// http://localhost:5173
// ============================================================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "GemoraCorsPolicy",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "https://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });


// ============================================================
// JWT AUTHENTICATION
// ============================================================

var jwtKey =
    builder.Configuration[
        "Jwt:Key"];

if (string.IsNullOrWhiteSpace(
        jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured.");
}


var jwtIssuer =
    builder.Configuration[
        "Jwt:Issuer"];

var jwtAudience =
    builder.Configuration[
        "Jwt:Audience"];


builder.Services
    .AddAuthentication(
        options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults
                    .AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults
                    .AuthenticationScheme;
        })
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,

                    ValidateAudience = true,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,

                    ValidIssuer =
                        jwtIssuer,

                    ValidAudience =
                        jwtAudience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtKey)),

                    ClockSkew =
                        TimeSpan.Zero
                };
        });


// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title =
                    "Gemora API",

                Version =
                    "v1",

                Description =
                    "Gemora Marketplace ASP.NET Core Web API"
            });


        // --------------------------------------------------------
        // JWT Bearer support in Swagger
        // --------------------------------------------------------

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name =
                    "Authorization",

                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "bearer",

                BearerFormat =
                    "JWT",

                In =
                    ParameterLocation.Header,

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
                                Type =
                                    ReferenceType
                                        .SecurityScheme,

                                Id =
                                    "Bearer"
                            }
                    },

                    Array.Empty<string>()
                }
            });
    });


// ============================================================
// BUILD APPLICATION
// ============================================================

var app =
    builder.Build();


// ============================================================
// GLOBAL EXCEPTION HANDLER
//
// Existing middleware:
// Gemora.API/Middleware/GlobalExceptionHandler.cs
// ============================================================

app.UseMiddleware<
    GlobalExceptionHandler>();


// ============================================================
// SWAGGER
// ============================================================

if (app.Environment
    .IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ============================================================
// STATIC FILES
//
// This exposes:
//
// /uploads/gem-images/...
// /uploads/certificates/...
//
// from:
//
// Gemora.API/wwwroot/uploads/...
// ============================================================

app.UseStaticFiles();


// ============================================================
// CORS
// ============================================================

app.UseCors(
    "GemoraCorsPolicy");


// ============================================================
// AUTHENTICATION
//
// Authentication must run BEFORE Authorization.
// ============================================================

app.UseAuthentication();


// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();


// ============================================================
// CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// DATABASE SEEDING
//
// Existing DbSeeder signature:
//
// SeedAsync(
//     ApplicationDbContext,
//     IConfiguration)
// ============================================================

using (var scope =
       app.Services.CreateScope())
{
    var services =
        scope.ServiceProvider;

    try
    {
        var dbContext =
            services.GetRequiredService<
                ApplicationDbContext>();

        await DbSeeder.SeedAsync(
            dbContext,
            builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger =
            services.GetRequiredService<
                ILogger<Program>>();

        logger.LogError(
            ex,
            "An error occurred while seeding the Gemora database.");

        throw;
    }
}


// ============================================================
// START APPLICATION
// ============================================================

app.Run();
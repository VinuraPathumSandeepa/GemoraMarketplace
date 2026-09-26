using Gemora.Domain.AI;
using System.Text;
using Gemora.API.Middleware;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.AI;
using Gemora.Infrastructure.Data;
using Gemora.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// DATABASE
// ============================================================

var connectionString =
    builder.Configuration
        .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(connectionString);
    });


// ============================================================
// APPLICATION SERVICES
// ============================================================

// Authentication service
builder.Services.AddScoped<
    IAuthService,
    AuthService>();


// JWT token generation service
builder.Services.AddScoped<
    TokenService>();


// Gem listing management
builder.Services.AddScoped<
    IGemListingService,
    GemListingService>();


// Gemologist verification/review workflow
builder.Services.AddScoped<
    IGemVerificationService,
    GemVerificationService>();


// ============================================================
// GEM AI — DETERMINISTIC EVIDENCE VALIDATOR
//
// This performs normal C# validation before Gemini is used.
//
// It checks:
// - Basic listing information
// - Gem image availability
// - Certificate evidence
// - Certificate metadata
// - Gemstone characteristics
// ============================================================

builder.Services.AddScoped<
    IGemEvidenceValidator,
    GemEvidenceValidator>();


// ============================================================
// GEM AI — VERIFICATION AGENT
//
// This is the orchestration layer.
//
// Current/final intended flow:
//
// NotStarted
//      ↓
// Processing
//      ↓
// Deterministic validation
//      ↓
// AI model analysis
//      ↓
// Completed
//      ↓
// Human Gemologist review
//
// The AI does NOT approve or reject listings.
// ============================================================

builder.Services.AddScoped<
    IGemVerificationAgent,
    GemVerificationAgent>();


// ============================================================
// GEMINI CONFIGURATION
//
// Reads:
//
// Gemini:ApiKey
// Gemini:Model
//
// The API key is stored in .NET User Secrets during local
// development and must NOT be committed to GitHub.
// ============================================================

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(
        GeminiOptions.SectionName));


// ============================================================
// GEMINI AI MODEL CLIENT
//
// IGemAiModelClient is the application-level abstraction.
//
// GeminiGemAnalysisClient is the infrastructure implementation.
//
// This keeps Gemini-specific HTTP code outside the application
// orchestration service.
// ============================================================

builder.Services.AddHttpClient<
    IGemAiModelClient,
    GeminiGemAnalysisClient>(
        client =>
        {
            client.BaseAddress =
                new Uri(
                    "https://generativelanguage.googleapis.com/");

            client.Timeout =
                TimeSpan.FromSeconds(60);
        });


// ============================================================
// FILE STORAGE
//
// Uploaded files are stored inside:
//
// Gemora.API/wwwroot/uploads
//
// app.UseStaticFiles() then exposes:
//
// /uploads/gem-images/...
// /uploads/certificates/...
// ============================================================

builder.Services.AddScoped<IFileStorageService>(
    serviceProvider =>
    {
        var environment =
            serviceProvider
                .GetRequiredService<IWebHostEnvironment>();


        var webRootPath =
            environment.WebRootPath;


        // WebRootPath may be null if wwwroot did not exist when
        // the application host was initialized.
        if (string.IsNullOrWhiteSpace(
                webRootPath))
        {
            webRootPath =
                Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot");
        }


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
// React development frontend:
//
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
                    // Validate who issued the JWT.
                    ValidateIssuer = true,

                    // Validate the intended audience.
                    ValidateAudience = true,

                    // Reject expired JWT tokens.
                    ValidateLifetime = true,

                    // Validate the JWT signature.
                    ValidateIssuerSigningKey = true,


                    ValidIssuer =
                        jwtIssuer,


                    ValidAudience =
                        jwtAudience,


                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtKey)),


                    // Token expires exactly at its expiration
                    // time without an additional grace period.
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


        // ========================================================
        // JWT BEARER AUTHENTICATION IN SWAGGER
        // ========================================================

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
// Existing behavior:
//
// InvalidOperationException
//      → 409 Conflict
//
// UnauthorizedAccessException
//      → 403 Forbidden
//
// KeyNotFoundException
//      → 404 Not Found
//
// Other unexpected exception
//      → 500 Internal Server Error
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
// Required for gemstone images and certificates.
//
// Example:
//
// http://localhost:5198/uploads/gem-images/xxx.jpg
//
// Physical location:
//
// Gemora.API/wwwroot/uploads/gem-images/xxx.jpg
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
// Authentication MUST execute before Authorization.
// ============================================================

app.UseAuthentication();


// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();


// ============================================================
// MAP CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// DATABASE SEEDING
//
// Seeds required Gemora staff accounts.
//
// Examples:
// - Admin
// - Gemologist
// - Export Officer
//
// Passwords are read from configuration/User Secrets and are
// not stored directly in this source file.
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
// START GEMORA API
// ============================================================

app.Run();
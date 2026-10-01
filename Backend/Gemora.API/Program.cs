using System.Text;

using Gemora.API.Middleware;
using Gemora.API.Services;

using Gemora.Application.Interfaces;
using Gemora.Application.Services;

using Gemora.Domain.AI;
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
        "Database connection string 'DefaultConnection' is not configured."
    );

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(connectionString);
    }
);


// ============================================================
// APPLICATION SERVICES
// ============================================================

// ------------------------------------------------------------
// Authentication
// ------------------------------------------------------------

builder.Services.AddScoped<
    IAuthService,
    AuthService>();


// ------------------------------------------------------------
// Profile image storage
// ------------------------------------------------------------

builder.Services.AddScoped<
    IProfileImageStorageService,
    ProfileImageStorageService>();


// ------------------------------------------------------------
// Email verification / OTP
// ------------------------------------------------------------

builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService>();


// ------------------------------------------------------------
// JWT token generation
// ------------------------------------------------------------

builder.Services.AddScoped<TokenService>();


// ------------------------------------------------------------
// Gem listing management
// ------------------------------------------------------------

builder.Services.AddScoped<
    IGemListingService,
    GemListingService>();


// ------------------------------------------------------------
// Human Gemologist verification workflow
// ------------------------------------------------------------

builder.Services.AddScoped<
    IGemVerificationService,
    GemVerificationService>();


// ============================================================
// DETERMINISTIC GEM EVIDENCE VALIDATOR
//
// Runs before the generative AI model.
// ============================================================

builder.Services.AddScoped<
    IGemEvidenceValidator,
    GemEvidenceValidator>();


// ============================================================
// GEM VERIFICATION AGENT
//
// Verification
//      ↓
// Deterministic validation
//      ↓
// Image evidence reader
//      ↓
// Gemini multimodal analysis
//      ↓
// Persistent AI result
//      ↓
// Human Gemologist decision
// ============================================================

builder.Services.AddScoped<
    IGemVerificationAgent,
    GemVerificationAgent>();


// ============================================================
// GEMINI CONFIGURATION
//
// User Secrets:
// Gemini:ApiKey
// Gemini:Model
//
// Never put the real API key in appsettings.json.
// ============================================================

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(
        GeminiOptions.SectionName));


// ============================================================
// GEMINI MODEL CLIENT
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
// LOCAL FILE STORAGE SERVICE
//
// Gemora.API
//   └── wwwroot
//       └── uploads
//           ├── gem-images
//           └── certificates
// ============================================================

builder.Services.AddScoped<IFileStorageService>(
    serviceProvider =>
    {
        var environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment>();

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
// GEM IMAGE READER
//
// Application layer sees:
// IGemImageReader
//
// Infrastructure handles:
// - wwwroot
// - physical file paths
// - streams
// - MIME types
// - path safety
// ============================================================

builder.Services.AddScoped<IGemImageReader>(
    serviceProvider =>
    {
        var environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment>();

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

        Directory.CreateDirectory(
            webRootPath);

        var uploadRoot =
            Path.Combine(
                webRootPath,
                "uploads");

        Directory.CreateDirectory(
            uploadRoot);

        return new LocalGemImageReader(
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
                        "https://localhost:5173",
                        "https://localhost:5174",
                        "http://localhost:5174",
                        "http://localhost:5175",
                        "https://localhost:5175")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });


// ============================================================
// JWT AUTHENTICATION
// ============================================================
>>>>>>> Stashed changes

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

var jwtIssuer =
    builder.Configuration[
        "Jwt:Issuer"];

var jwtAudience =
    builder.Configuration[
        "Jwt:Audience"];


// ======================================================
// 5. JWT AUTHENTICATION
// ======================================================

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

app.UseMiddleware<
    GlobalExceptionHandler>();

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

        logger.LogError(
            ex,
            "An error occurred while seeding the Gemora database.");

// ======================================================
// 16. MAP CONTROLLERS
// ======================================================

app.MapControllers();


// ======================================================
// 17. START APPLICATION
// ======================================================

app.Run();
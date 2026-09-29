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
                        "https://localhost:5175",
                        "https://gemora-marketplace-web.onrender.com",
                        "http://gemora-marketplace-web.onrender.com"
                )
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

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured."
    );
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
                Title = "Gemora API",

                Version = "v1",

                Description =
                    "Gemora Marketplace ASP.NET Core Web API"
            });


        // ====================================================
        // SWAGGER JWT AUTHENTICATION
        // ====================================================

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
// InvalidOperationException
//      → 409
//
// UnauthorizedAccessException
//      → 403
//
// KeyNotFoundException
//      → 404
//
// Unexpected exception
//      → 500
// ============================================================

app.UseMiddleware<
    GlobalExceptionHandler>();


// ============================================================
// SWAGGER
// ============================================================

// Always enable Swagger so the deployed API can be tested.
// In a production-hardened app you would restrict this,
// but for a campus project this is convenient.
app.UseSwagger();

app.UseSwaggerUI();


// ============================================================
// STATIC FILES
//
// Allows:
//
// /uploads/gem-images/example.jpg
//
// /uploads/certificates/example.pdf
// ============================================================

app.UseStaticFiles();


// ============================================================
// CORS
// ============================================================

app.UseCors(
    "GemoraCorsPolicy");


// ============================================================
// AUTHENTICATION
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
// Existing staff accounts:
// - Admin
// - Gemologist
// - ExportOfficer
//
// Passwords are loaded from secure configuration / User Secrets.
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

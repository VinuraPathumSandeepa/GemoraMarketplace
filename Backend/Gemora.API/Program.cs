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
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


// ============================================================
// BUILD APPLICATION
// ============================================================

var builder =
    WebApplication.CreateBuilder(args);


// ============================================================
// SUPABASE STORAGE CONFIGURATION
//
// Uses the modern sb_secret_ API key.
//
// Local:
// .NET User Secrets
//
// Render:
// Environment Variables
// ============================================================

builder.Services.AddSingleton(
    new SupabaseStorageOptions
    {
        ProjectUrl =
            builder.Configuration[
                "SupabaseStorage:ProjectUrl"
            ] ?? "",

        ServiceRoleKey =
            builder.Configuration[
                "SupabaseStorage:ServiceRoleKey"
            ] ?? ""
    }
);


// ============================================================
// SUPABASE STORAGE HTTP CLIENT
//
// Used by:
//
// 1. Gemstone image uploads
// 2. Profile image uploads
// 3. Private certificate uploads
// 4. AI gemstone image reading
// 5. Private certificate signed URL generation
// ============================================================

builder.Services.AddHttpClient<
    SupabaseStorageClient
>();


// ============================================================
// WEB ROOT CONFIGURATION
//
// Local wwwroot support is retained for legacy files.
//
// Existing database records may still contain:
//
// /uploads/profiles/...
// /uploads/gem-images/...
// /uploads/certificates/...
//
// New certificate uploads no longer use local storage.
// ============================================================

var webRootPath =
    Path.Combine(
        builder.Environment.ContentRootPath,
        "wwwroot"
    );

Directory.CreateDirectory(
    webRootPath
);

var uploadRootPath =
    Path.Combine(
        webRootPath,
        "uploads"
    );

Directory.CreateDirectory(
    uploadRootPath
);

Directory.CreateDirectory(
    Path.Combine(
        uploadRootPath,
        "profiles"
    )
);

Directory.CreateDirectory(
    Path.Combine(
        uploadRootPath,
        "gem-images"
    )
);

Directory.CreateDirectory(
    Path.Combine(
        uploadRootPath,
        "certificates"
    )
);

builder.Environment.WebRootPath =
    webRootPath;

builder.Environment.WebRootFileProvider =
    new PhysicalFileProvider(
        webRootPath
    );


// ============================================================
// DATABASE - POSTGRESQL / SUPABASE
// ============================================================

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "DefaultConnection"
        )
    ?? throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is not configured."
    );

builder.Services.AddDbContext<
    ApplicationDbContext
>(
    options =>
    {
        options.UseNpgsql(
            connectionString
        );
    }
);


// ============================================================
// AUTHENTICATION SERVICE
// ============================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService
>();


// ============================================================
// PROFILE IMAGE STORAGE - SUPABASE
//
// ProfileImageStorageService uses SupabaseStorageClient.
// ============================================================

builder.Services.AddScoped<
    IProfileImageStorageService,
    ProfileImageStorageService
>();


// ============================================================
// EMAIL VERIFICATION / OTP SERVICE
// ============================================================

builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService
>();


// ============================================================
// JWT TOKEN SERVICE
// ============================================================

builder.Services.AddScoped<
    TokenService
>();


// ============================================================
// GEM LISTING MANAGEMENT
// ============================================================

builder.Services.AddScoped<
    IGemListingService,
    GemListingService
>();


// ============================================================
// HUMAN GEMOLOGIST VERIFICATION SERVICE
// ============================================================

builder.Services.AddScoped<
    IGemVerificationService,
    GemVerificationService
>();


// ============================================================
// GEM EVIDENCE VALIDATOR
// ============================================================

builder.Services.AddScoped<
    IGemEvidenceValidator,
    GemEvidenceValidator
>();


// ============================================================
// GEM VERIFICATION AGENT
// ============================================================

builder.Services.AddScoped<
    IGemVerificationAgent,
    GemVerificationAgent
>();


// ============================================================
// GEMINI OPTIONS
// ============================================================

builder.Services.Configure<
    GeminiOptions
>(
    builder.Configuration.GetSection(
        GeminiOptions.SectionName
    )
);


// ============================================================
// GEMINI MODEL HTTP CLIENT
// ============================================================

builder.Services.AddHttpClient<
    IGemAiModelClient,
    GeminiGemAnalysisClient
>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                "https://generativelanguage.googleapis.com/"
            );

        client.Timeout =
            TimeSpan.FromSeconds(60);
    }
);


// ============================================================
// LEGACY LOCAL FILE STORAGE
//
// Still required for:
//
// - deleting old local gemstone images;
// - deleting old local certificates;
// - supporting old /uploads/... database references.
//
// New gemstone images and certificates are stored in Supabase.
// ============================================================

builder.Services.AddScoped<
    LocalFileStorageService
>(
    serviceProvider =>
    {
        var environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment
                >();

        var configuredWebRoot =
            environment.WebRootPath;

        if (
            string.IsNullOrWhiteSpace(
                configuredWebRoot
            )
        )
        {
            configuredWebRoot =
                Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot"
                );
        }

        var uploadDirectory =
            Path.Combine(
                configuredWebRoot,
                "uploads"
            );

        Directory.CreateDirectory(
            uploadDirectory
        );

        return new LocalFileStorageService(
            uploadDirectory
        );
    }
);


// ============================================================
// LEGACY LOCAL GEM IMAGE READER
//
// Supports old database references:
//
// /uploads/gem-images/filename.jpg
//
// Required by HybridGemImageReader.
// ============================================================

builder.Services.AddScoped<
    LocalGemImageReader
>(
    serviceProvider =>
    {
        var environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment
                >();

        var configuredWebRoot =
            environment.WebRootPath;

        if (
            string.IsNullOrWhiteSpace(
                configuredWebRoot
            )
        )
        {
            configuredWebRoot =
                Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot"
                );
        }

        var uploadDirectory =
            Path.Combine(
                configuredWebRoot,
                "uploads"
            );

        Directory.CreateDirectory(
            uploadDirectory
        );

        return new LocalGemImageReader(
            uploadDirectory
        );
    }
);


// ============================================================
// ACTIVE HYBRID FILE STORAGE
//
// Gemstone images:
//
//     SUPABASE
//     -> public gem-images bucket
//
// Certificates:
//
//     SUPABASE
//     -> private gem-certificates bucket
//
// Legacy local /uploads/... references remain supported for
// deletion and backward compatibility.
// ============================================================

builder.Services.AddScoped<
    IFileStorageService,
    HybridFileStorageService
>();


// ============================================================
// ACTIVE HYBRID GEM IMAGE READER
//
// Supports:
//
// 1. Existing local gemstone photographs.
// 2. New Supabase gemstone photographs.
//
// Used by the Gemini Verification Agent.
// ============================================================

builder.Services.AddScoped<
    IGemImageReader,
    HybridGemImageReader
>();


// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();


// ============================================================
// CORS CONFIGURATION
//
// React development and deployed Render frontend.
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

                        "http://localhost:5174",
                        "https://localhost:5174",

                        "http://localhost:5175",
                        "https://localhost:5175",

                        "https://gemora-marketplace-web.onrender.com",
                        "http://gemora-marketplace-web.onrender.com"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        );
    }
);


// ============================================================
// JWT AUTHENTICATION CONFIGURATION
// ============================================================

var jwtKey =
    builder.Configuration[
        "Jwt:Key"
    ];

if (
    string.IsNullOrWhiteSpace(
        jwtKey
    )
)
{
    throw new InvalidOperationException(
        "JWT signing key is not configured."
    );
}

var jwtIssuer =
    builder.Configuration[
        "Jwt:Issuer"
    ];

var jwtAudience =
    builder.Configuration[
        "Jwt:Audience"
    ];

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
        }
    )
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer =
                        true,

                    ValidateAudience =
                        true,

                    ValidateLifetime =
                        true,

                    ValidateIssuerSigningKey =
                        true,

                    ValidIssuer =
                        jwtIssuer,

                    ValidAudience =
                        jwtAudience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtKey
                            )
                        ),

                    ClockSkew =
                        TimeSpan.Zero
                };
        }
    );


// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// SWAGGER CONFIGURATION
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
            }
        );

        // ----------------------------------------------------
        // JWT SECURITY DEFINITION
        // ----------------------------------------------------

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
            }
        );

        // ----------------------------------------------------
        // APPLY JWT AUTHENTICATION TO SWAGGER
        // ----------------------------------------------------

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
            }
        );
    }
);


// ============================================================
// BUILD THE APPLICATION
// ============================================================

var app =
    builder.Build();


// ============================================================
// GLOBAL EXCEPTION HANDLER
// ============================================================

app.UseMiddleware<
    GlobalExceptionHandler
>();


// ============================================================
// SWAGGER
// ============================================================

app.UseSwagger();

app.UseSwaggerUI();


// ============================================================
// BLOCK DIRECT PUBLIC ACCESS TO LEGACY CERTIFICATES
//
// Historical certificates may still physically exist under:
//
// wwwroot/uploads/certificates/...
//
// They must NOT be downloaded directly through StaticFiles.
//
// Authorized users must instead use:
//
// /api/GemCertificates/listings/{id}/access
//
// The GemCertificatesController performs authorization and:
//
// - serves legacy local certificates securely;
// - generates temporary signed URLs for private Supabase
//   certificates.
//
// IMPORTANT:
//
// This middleware must remain BEFORE app.UseStaticFiles().
// ============================================================

app.Use(
    async (context, next) =>
    {
        var requestPath =
            context.Request.Path.Value;

        if (
            !string.IsNullOrWhiteSpace(
                requestPath
            ) &&
            (
                requestPath.Equals(
                    "/uploads/certificates",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                requestPath.StartsWith(
                    "/uploads/certificates/",
                    StringComparison.OrdinalIgnoreCase
                )
            )
        )
        {
            context.Response.StatusCode =
                StatusCodes.Status404NotFound;

            return;
        }

        await next();
    }
);


// ============================================================
// STATIC FILES
//
// Retained only for public legacy assets such as:
//
// /uploads/profiles/...
// /uploads/gem-images/...
//
// /uploads/certificates/... is blocked by the middleware above.
// ============================================================

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider =
            builder.Environment
                .WebRootFileProvider
    }
);


// ============================================================
// CORS
// ============================================================

app.UseCors(
    "GemoraCorsPolicy"
);


// ============================================================
// AUTHENTICATION
// ============================================================

app.UseAuthentication();


// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();


// ============================================================
// MAP API CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// DATABASE SEEDING
//
// Preserves existing staff accounts:
//
// - Admin
// - Gemologist
// - Export Officer
//
// Passwords come from secure configuration.
// ============================================================

using (
    var scope =
        app.Services.CreateScope()
)
{
    var services =
        scope.ServiceProvider;

    try
    {
        var dbContext =
            services
                .GetRequiredService<
                    ApplicationDbContext
                >();

        await DbSeeder.SeedAsync(
            dbContext,
            builder.Configuration
        );
    }
    catch (Exception ex)
    {
        var logger =
            services
                .GetRequiredService<
                    ILogger<Program>
                >();

        logger.LogError(
            ex,
            "An error occurred while seeding the Gemora database."
        );

        throw;
    }
}


// ============================================================
// START GEMORA API
// ============================================================

app.Run();
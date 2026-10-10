using System.Text;
using System.Security.Claims;

using Gemora.API.Middleware;
using Gemora.API.Services;

using Gemora.Application.Configuration;
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
using Npgsql;


// ============================================================
// BUILD APPLICATION
// ============================================================

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// SUPABASE STORAGE CONFIGURATION
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
// Used for:
// - gemstone images
// - profile images
// - private certificates
// - AI gemstone image reading
// - certificate signed URLs
// ============================================================

builder.Services.AddHttpClient<
    SupabaseStorageClient
>();


// ============================================================
// WEB ROOT CONFIGURATION
//
// Legacy local files remain supported during migration.
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
// PROFILE IMAGE STORAGE
//
// Profile images use Supabase Storage.
// ============================================================

builder.Services.AddScoped<
    IProfileImageStorageService,
    ProfileImageStorageService
>();


// ============================================================
// EMAIL VERIFICATION / OTP
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
// COMPONENT 4 - EXPORT COMPLIANCE FILE STORAGE
//
// IMPORTANT:
//
// This IFileStorageService belongs to:
//
// Gemora.Application.Interfaces
//
// It is different from the Gem Verification storage interface
// under Gemora.Domain.Interfaces.
// ============================================================

builder.Services.AddSingleton<
    Gemora.Application.Interfaces.IFileStorageService,
    Gemora.API.Services.LocalFileStorageService
>();


// ============================================================
// COMPONENT 4 - EXPORT COMPLIANCE SERVICES
// ============================================================

builder.Services.AddScoped<
    IComplianceRulesService,
    ComplianceRulesService
>();

builder.Services.AddScoped<
    IExportComplianceService,
    ExportComplianceService
>();

builder.Services.AddScoped<
    IExportOfficerService,
    ExportOfficerService
>();

builder.Services.AddScoped<
    IComplianceAgentToolService,
    ComplianceAgentToolService
>();

builder.Services.AddScoped<
    IComplianceWorkflowService,
    ComplianceWorkflowService
>();

// Secure shipping and insurance
builder.Services.AddScoped<
    Gemora.Application.Services.IShipmentService,
    ShipmentService>();
builder.Services.AddScoped<
    Gemora.Application.Services.IShippingAgentService,
    ShippingAgentService>();

// Courier provider adapter (MOCK/SIMULATION)
builder.Services.AddScoped<
    Gemora.Domain.Interfaces.IShippingProviderAdapter,
    Gemora.Infrastructure.Adapters.MockShippingProviderAdapter>(sp =>
{
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Gemora.Infrastructure.Adapters.MockShippingProviderAdapter>>();
    return new Gemora.Infrastructure.Adapters.MockShippingProviderAdapter(
        logger,
        maxRetryAttempts: 3,
        timeoutSeconds: 30,
        simulateFailures: false // Set to true for testing failure scenarios
    );
});

// Real shipping agent: model-selected read-only tools followed by validated JSON output.
builder.Services.AddHttpClient<
    Gemora.Domain.Interfaces.ILlmProvider,
    Gemora.Infrastructure.Providers.GeminiShippingAgentProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(50);
});

// ============================================================
// COMPONENT 4 - GEMINI COMPLIANCE AI
// ============================================================

builder.Services.Configure<
    GeminiComplianceOptions
>(
    builder.Configuration.GetSection(
        GeminiComplianceOptions.SectionName
    )
);

builder.Services.AddHttpClient<
    IComplianceAiClient,
    GeminiComplianceAiClient
>();


// ============================================================
// GEM LISTING MANAGEMENT
// ============================================================

builder.Services.AddScoped<
    IGemListingService,
    GemListingService
>();


// ============================================================
// HUMAN GEMOLOGIST VERIFICATION
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
// GEM VERIFICATION GEMINI OPTIONS
// ============================================================

builder.Services.Configure<
    GeminiOptions
>(
    builder.Configuration.GetSection(
        GeminiOptions.SectionName
    )
);


// ============================================================
// GEM VERIFICATION GEMINI HTTP CLIENT
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
// LEGACY GEM VERIFICATION LOCAL FILE STORAGE
//
// IMPORTANT:
//
// This is the Infrastructure LocalFileStorageService.
//
// It is different from:
//
// Gemora.API.Services.LocalFileStorageService
//
// Used for:
// - legacy gem images
// - legacy certificates
// - cleanup of old /uploads/... references
// ============================================================

builder.Services.AddScoped<
    Gemora.Infrastructure.Services.LocalFileStorageService
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

        return new
            Gemora.Infrastructure.Services.LocalFileStorageService(
                uploadDirectory
            );
    }
);


// ============================================================
// LEGACY LOCAL GEM IMAGE READER
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
// GEM VERIFICATION HYBRID FILE STORAGE
//
// IMPORTANT:
//
// This IFileStorageService belongs to:
//
// Gemora.Domain.Interfaces
//
// New gemstone images:
//   Supabase public gem-images bucket
//
// New certificates:
//   Supabase private gem-certificates bucket
//
// Legacy /uploads/... references remain supported.
// ============================================================

builder.Services.AddScoped<
    Gemora.Domain.Interfaces.IFileStorageService,
    HybridFileStorageService
>();


// ============================================================
// HYBRID GEM IMAGE READER
//
// Supports:
//
// 1. Legacy local gemstone images.
// 2. New Supabase gemstone images.
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
// CORS
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
// JWT AUTHENTICATION
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

                // Expected issuer
                ValidIssuer = jwtIssuer,

                // Expected audience
                ValidAudience = jwtAudience,

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
            });
    });


// ============================================================
// BUILD APPLICATION
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
// Legacy certificates under:
//
// /uploads/certificates/...
//
// must only be accessed through:
//
// /api/GemCertificates/listings/{id}/access
//
// This middleware MUST remain before UseStaticFiles().
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
// Public legacy assets remain accessible.
//
// Direct legacy certificate access is blocked above.
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
// CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// DATABASE SEEDING
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

    // ======================================================
    // COMPONENT 3 TABLES NOW MANAGED BY EF CORE MIGRATIONS (Phase 2)
    // Manual SQL below is DISABLED - see migration CompleteShippingWorkflowSchema
    // ======================================================

    /* DISABLED: EF migrations now manage Component 3 schema
    try
    {
        Console.WriteLine("Ensuring Component 3 tables exist...");

        // Create Shipments table (Phase 3: Added booking fields)
        await dbContext.Database.ExecuteSqlRawAsync(
            @"DROP TABLE IF EXISTS ""Shipments"" CASCADE;"
        );
        await dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE ""Shipments"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""OrderId"" uuid NOT NULL,
                ""SellerId"" uuid NOT NULL,
                ""BuyerId"" uuid NOT NULL,
                ""OriginAddress"" character varying(500) NOT NULL,
                ""OriginRegion"" character varying(100) NOT NULL,
                ""OriginCountryCode"" character varying(2) NOT NULL,
                ""DestinationAddress"" character varying(500) NOT NULL,
                ""DestinationRegion"" character varying(100) NOT NULL,
                ""DestinationCountryCode"" character varying(2) NOT NULL,
                ""DeclaredValue"" numeric(18,2) NOT NULL,
                ""Currency"" character varying(20) NOT NULL DEFAULT 'USD',
                ""PackageDescription"" character varying(1000) NOT NULL,
                ""PackageWeight"" numeric(10,2) NULL,
                ""PackageDimensions"" character varying(200) NULL,
                ""SpecialHandlingNotes"" character varying(2000) NOT NULL DEFAULT '',
                ""PreferredService"" character varying(200) NOT NULL,
                ""ExportRequired"" boolean NOT NULL DEFAULT false,
                ""Status"" character varying(50) NOT NULL DEFAULT 'Pending',
                ""RiskLevel"" character varying(20) NULL,
                ""TrackingNumber"" character varying(100) NULL,
                ""CourierName"" character varying(200) NULL,
                ""ExternalShipmentReference"" character varying(200) NULL,
                ""SelectedService"" character varying(200) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                ""UpdatedAt"" timestamp with time zone NULL,
                ""BookedAt"" timestamp with time zone NULL,
                ""ShippedAt"" timestamp with time zone NULL,
                ""DeliveredAt"" timestamp with time zone NULL,
                CONSTRAINT ""FK_Shipments_Orders_OrderId"" FOREIGN KEY (""OrderId"") REFERENCES ""Orders""(""Id""),
                CONSTRAINT ""FK_Shipments_Users_SellerId"" FOREIGN KEY (""SellerId"") REFERENCES ""Users""(""Id""),
                CONSTRAINT ""FK_Shipments_Users_BuyerId"" FOREIGN KEY (""BuyerId"") REFERENCES ""Users""(""Id"")
            );"
        );

        // Create ShippingPlans table (Phase 7: Added GenerationSource and ExecutionSummary)
        await dbContext.Database.ExecuteSqlRawAsync(
            @"DROP TABLE IF EXISTS ""ShippingPlans"" CASCADE;"
        );
        await dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE ""ShippingPlans"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""ShipmentId"" uuid NOT NULL,
                ""RiskLevel"" character varying(20) NOT NULL DEFAULT 'Medium',
                ""RiskReasons"" text NULL,
                ""RecommendedServiceType"" character varying(200) NOT NULL,
                ""InsuranceRecommended"" boolean NOT NULL DEFAULT false,
                ""RecommendedCoverageAmount"" numeric(18,2) NULL,
                ""HandlingRequirements"" text NULL,
                ""RequiredDocuments"" text NULL,
                ""Warnings"" text NULL,
                ""GenerationSource"" character varying(50) NOT NULL DEFAULT 'FallbackRules',
                ""ExecutionSummary"" text NULL,
                ""IsApproved"" boolean NOT NULL DEFAULT false,
                ""ApprovedBy"" uuid NULL,
                ""ApprovedAt"" timestamp with time zone NULL,
                ""AdminNotes"" text NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                ""UpdatedAt"" timestamp with time zone NULL,
                CONSTRAINT ""FK_ShippingPlans_Shipments_ShipmentId"" FOREIGN KEY (""ShipmentId"") REFERENCES ""Shipments""(""Id""),
                CONSTRAINT ""FK_ShippingPlans_Users_ApprovedBy"" FOREIGN KEY (""ApprovedBy"") REFERENCES ""Users""(""Id"")
            );"
        );

        // Create InsuranceRecords table (Phase 4: Added DeclaredValue, PolicyReference, PremiumAmount)
        await dbContext.Database.ExecuteSqlRawAsync(
            @"DROP TABLE IF EXISTS ""InsuranceRecords"" CASCADE;"
        );
        await dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE ""InsuranceRecords"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""ShipmentId"" uuid NOT NULL,
                ""DeclaredValue"" numeric(18,2) NOT NULL DEFAULT 0,
                ""CoverageAmount"" numeric(18,2) NOT NULL,
                ""Currency"" character varying(20) NOT NULL DEFAULT 'USD',
                ""CoverageType"" character varying(50) NOT NULL DEFAULT 'Standard',
                ""PolicyNumber"" character varying(100) NULL,
                ""PolicyReference"" character varying(200) NULL,
                ""ProviderName"" character varying(200) NULL,
                ""PremiumAmount"" numeric(18,2) NOT NULL DEFAULT 0,
                ""PolicyStartDate"" timestamp with time zone NULL,
                ""PolicyEndDate"" timestamp with time zone NULL,
                ""Status"" character varying(50) NOT NULL DEFAULT 'Pending',
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                ""UpdatedAt"" timestamp with time zone NULL,
                CONSTRAINT ""FK_InsuranceRecords_Shipments_ShipmentId"" FOREIGN KEY (""ShipmentId"") REFERENCES ""Shipments""(""Id"")
            );"
        );

        // Create ShipmentTrackingEvents table WITH OccurredAt, RecordedAt, ExternalEventCode (Phase 4)
        await dbContext.Database.ExecuteSqlRawAsync(
            @"DROP TABLE IF EXISTS ""ShipmentTrackingEvents"" CASCADE;"
        );
        await dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE ""ShipmentTrackingEvents"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""ShipmentId"" uuid NOT NULL,
                ""EventType"" character varying(50) NOT NULL,
                ""Location"" character varying(200) NOT NULL,
                ""Description"" character varying(1000) NOT NULL,
                ""ExternalEventCode"" character varying(100) NULL,
                ""OccurredAt"" timestamp with time zone NOT NULL,
                ""RecordedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""FK_ShipmentTrackingEvents_Shipments_ShipmentId"" FOREIGN KEY (""ShipmentId"") REFERENCES ""Shipments""(""Id"")
            );"
        );

        Console.WriteLine("Component 3 tables ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating Component 3 tables: {ex.Message}");
        throw;
    }

    // Mark AddShippingAndInsuranceEntities migration as applied if not already
    try
    {
        Console.WriteLine("Checking migration history...");
        await dbContext.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
              VALUES ('20260928211333_AddShippingAndInsuranceEntities', '8.0.8')
              ON CONFLICT DO NOTHING;"
        );
        Console.WriteLine("Migration history updated.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating migration history: {ex.Message}");
    }
    */

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

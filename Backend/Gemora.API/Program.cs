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


var builder =
    WebApplication.CreateBuilder(args);

// Explicit maintenance command: reset only the existing administrator and exit
// before schema preparation, seeding, or starting the HTTP server.
if (args.Contains("--reset-admin-password", StringComparer.Ordinal))
{
    var resetConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
    var resetPassword = builder.Configuration["SeedUsers:AdminPassword"];
    if (string.IsNullOrWhiteSpace(resetPassword))
        throw new InvalidOperationException("SeedUsers:AdminPassword is not configured.");

    var resetOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(resetConnection).Options;
    await using var resetContext = new ApplicationDbContext(resetOptions);
    var administrator = await resetContext.Users.SingleOrDefaultAsync(
        user => user.Email.ToLower() == "admin@gemora.com" && user.Role == "Admin")
        ?? throw new InvalidOperationException("The existing admin@gemora.com administrator was not found.");

    administrator.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPassword);
    await resetContext.SaveChangesAsync();
    await resetContext.Entry(administrator).ReloadAsync();
    if (!BCrypt.Net.BCrypt.Verify(resetPassword, administrator.PasswordHash))
        throw new InvalidOperationException("Administrator password verification failed.");

    Console.WriteLine("Administrator password reset and verified. Sign in with the configured SeedUsers:AdminPassword.");
    return;
}

// Explicit maintenance command: reset only the existing Gemologist and exit.
// Normal database seeding intentionally preserves existing credentials.
if (args.Contains("--reset-gemologist-password", StringComparer.Ordinal))
{
    var resetConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
    var resetPassword = builder.Configuration["SeedUsers:GemologistPassword"];
    if (string.IsNullOrWhiteSpace(resetPassword))
        throw new InvalidOperationException("SeedUsers:GemologistPassword is not configured.");

    var resetOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(resetConnection).Options;
    await using var resetContext = new ApplicationDbContext(resetOptions);
    var gemologist = await resetContext.Users.SingleOrDefaultAsync(
        user => user.Email.ToLower() == "gemologist@gemora.com" && user.Role == "Gemologist")
        ?? throw new InvalidOperationException("The existing gemologist@gemora.com account was not found.");

    gemologist.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPassword);
    await resetContext.SaveChangesAsync();
    await resetContext.Entry(gemologist).ReloadAsync();
    if (!BCrypt.Net.BCrypt.Verify(resetPassword, gemologist.PasswordHash))
        throw new InvalidOperationException("Gemologist password verification failed.");

    Console.WriteLine("Gemologist password reset and verified. Sign in with the configured SeedUsers:GemologistPassword.");
    return;
}


// Explicit maintenance command: reset only the existing Export Officer and
// exit. Normal database seeding intentionally preserves existing credentials.
if (args.Contains("--reset-export-officer-password", StringComparer.Ordinal))
{
    var resetConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
    var resetPassword = builder.Configuration["SeedUsers:ExportOfficerPassword"];
    if (string.IsNullOrWhiteSpace(resetPassword))
        throw new InvalidOperationException("SeedUsers:ExportOfficerPassword is not configured.");

    var resetOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(resetConnection).Options;
    await using var resetContext = new ApplicationDbContext(resetOptions);
    var exportOfficer = await resetContext.Users.SingleOrDefaultAsync(
        user => user.Email.ToLower() == "export@gemora.com" && user.Role == "ExportOfficer")
        ?? throw new InvalidOperationException("The existing export@gemora.com Export Officer account was not found.");

    exportOfficer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPassword);
    await resetContext.SaveChangesAsync();
    await resetContext.Entry(exportOfficer).ReloadAsync();
    if (!BCrypt.Net.BCrypt.Verify(resetPassword, exportOfficer.PasswordHash))
        throw new InvalidOperationException("Export Officer password verification failed.");

    Console.WriteLine("Export Officer password reset and verified. Sign in with export@gemora.com and the configured SeedUsers:ExportOfficerPassword.");
    return;
}


// ============================================================
// SUPABASE STORAGE CONFIGURATION
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
// ============================================================

builder.Services.AddHttpClient<
    SupabaseStorageClient
>();


// ============================================================
// WEB ROOT CONFIGURATION
//
// Keeps legacy local uploads working together with Supabase.
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
// Uses the current Supabase-aware profile image service.
// ============================================================

builder.Services.AddScoped<ProfileImageStorageService>();
builder.Services.AddScoped<IProfileImageStorageService, DatabaseProfileImageStorageService>();


// ============================================================
// EMAIL / OTP SERVICE
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
// Gemora.Application.Interfaces
//
// It is different from:
// Gemora.Domain.Interfaces.IFileStorageService
// ============================================================

builder.Services.AddSingleton<
    Gemora.Application.Interfaces.IFileStorageService,
    Gemora.API.Services.LocalFileStorageService
>();


// ============================================================
// COMPONENT 4 - EXPORT / COMPLIANCE SERVICES
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

// Academic courier sandbox: generates simulated references without calling a courier API.
builder.Services.AddScoped<
    Gemora.Domain.Interfaces.IShippingProviderAdapter,
    Gemora.Infrastructure.Adapters.MockShippingProviderAdapter>(sp =>
        new Gemora.Infrastructure.Adapters.MockShippingProviderAdapter(
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Gemora.Infrastructure.Adapters.MockShippingProviderAdapter>>(),
            maxRetryAttempts: 3, timeoutSeconds: 30, simulateFailures: false));

// Real shipping agent: model-selected read-only tools followed by validated JSON output.
builder.Services.AddHttpClient<
    Gemora.Domain.Interfaces.ILlmProvider,
    Gemora.Infrastructure.Providers.GeminiShippingAgentProvider>(client =>
{
    // The provider enforces one bounded timeout for the entire multi-call run.
    client.Timeout = Timeout.InfiniteTimeSpan;
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
// COMPONENT 2 - MARKETPLACE & TRANSACTIONS
// ============================================================

builder.Services.AddScoped<
    IMarketplaceService,
    MarketplaceService
>();

builder.Services.AddScoped<
    IPaymentGateway,
    SandboxPaymentGateway
>();


builder.Services.AddScoped<
    IOrderService,
    OrderService
>();


builder.Services.AddScoped<
    IMarketplaceAgentService,
    MarketplaceAgentService
>();


// ============================================================
// COMPONENT 2 - GEMINI MARKETPLACE AI
//
// IMPORTANT:
//
// ALL service registrations MUST remain BEFORE:
//
// var app = builder.Build();
//
// GeminiMarketplaceAiClient implementation is located at:
//
// Gemora.API/Services/GeminiMarketplaceAiClient.cs
// ============================================================

builder.Services.Configure<
    GeminiMarketplaceOptions
>(
    builder.Configuration.GetSection(
        GeminiMarketplaceOptions.SectionName
    )
);


builder.Services.AddHttpClient<
    IMarketplaceAiClient,
    GeminiMarketplaceAiClient
>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                "https://generativelanguage.googleapis.com/"
            );

        client.Timeout =
            TimeSpan.FromSeconds(
                60
            );
    }
);


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
            TimeSpan.FromSeconds(
                60
            );
    }
);


// ============================================================
// LEGACY LOCAL FILE STORAGE
//
// Used by the hybrid storage layer for old /uploads/... files.
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


        Directory.CreateDirectory(
            configuredWebRoot
        );


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


        Directory.CreateDirectory(
            configuredWebRoot
        );


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
// GEM VERIFICATION FILE STORAGE
//
// Development:
//     Local storage keeps localhost uploads independent from
//     external Supabase availability and configuration.
//
// Non-development:
//     Hybrid storage writes new evidence to Supabase and keeps
//     legacy /uploads/... references readable.
//
// Both implementations preserve the same IFileStorageService
// contract, so listing and verification functions are unchanged.
// ============================================================

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<
        Gemora.Domain.Interfaces.IFileStorageService
    >(
        serviceProvider =>
            serviceProvider.GetRequiredService<
                Gemora.Infrastructure.Services.LocalFileStorageService
            >()
    );
}
else
{
    builder.Services.AddScoped<
        Gemora.Domain.Interfaces.IFileStorageService,
        HybridFileStorageService
    >();
}


// ============================================================
// HYBRID GEM IMAGE READER
//
// Supports:
// - Legacy local files
// - Supabase gemstone images
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
                    .SetIsOriginAllowed(origin =>
                    {
                        if (
                            origin.Equals(
                                "https://gemora-marketplace-web.onrender.com",
                                StringComparison.OrdinalIgnoreCase) ||
                            origin.Equals(
                                "http://gemora-marketplace-web.onrender.com",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        if (!Uri.TryCreate(
                                origin,
                                UriKind.Absolute,
                                out var uri))
                        {
                            return false;
                        }

                        return (
                                uri.Host.Equals(
                                    "localhost",
                                    StringComparison.OrdinalIgnoreCase) ||
                                uri.Host.Equals(
                                    "127.0.0.1",
                                    StringComparison.OrdinalIgnoreCase)) &&
                            (uri.Scheme == Uri.UriSchemeHttp ||
                             uri.Scheme == Uri.UriSchemeHttps);
                    })
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
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    if (identity == null || !Guid.TryParse(
                        identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    {
                        context.Fail("Invalid user identity.");
                        return;
                    }

                    // Match authorization to /Auth/me, including role changes
                    // made after this token was issued.
                    var db = context.HttpContext.RequestServices
                        .GetRequiredService<ApplicationDbContext>();
                    var role = await db.Users.AsNoTracking()
                        .Where(user => user.Id == userId)
                        .Select(user => user.Role)
                        .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        context.Fail("Account no longer exists or has no role.");
                        return;
                    }

                    foreach (var claim in identity.FindAll(identity.RoleClaimType).ToList())
                    {
                        identity.RemoveClaim(claim);
                    }
                    identity.AddClaim(new Claim(identity.RoleClaimType, role));
                }
            };

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
//
// IMPORTANT:
//
// Do NOT add builder.Services registrations below this line.
// The IServiceCollection becomes read-only after Build().
// ============================================================

builder.Services.AddHostedService<Gemora.API.Services.OrderExpiryWorker>();

var app =
    builder.Build();


// ============================================================
// DATABASE COMPATIBILITY + SEEDING
//
// Existing migration history and the current Component 2
// transaction schema are not fully aligned.
//
// Do not automatically call Database.Migrate() here.
//
// DatabaseCompatibilityInitializer adds required Component 2
// runtime columns/tables without deleting existing data.
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


        // ----------------------------------------------------
        // COMPONENT 2 DATABASE COMPATIBILITY
        // ----------------------------------------------------

        await DatabaseCompatibilityInitializer
            .EnsureComponent2SchemaAsync(
                dbContext
            );


        // ----------------------------------------------------
        // EXISTING PROJECT SEEDING
        // ----------------------------------------------------

        await DbSeeder.SeedAsync(
            dbContext,
            builder.Configuration
        );

        await ShipmentOrderProgress.ReconcileExistingAsync(dbContext);
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
            "An error occurred while preparing the Gemora database."
        );


        throw;
    }
}


// ============================================================
// GLOBAL EXCEPTION HANDLER
// ============================================================

app.UseMiddleware<
    GlobalExceptionHandler
>();


// ============================================================
// HTTPS
//
// Local development:
// Frontend -> http://localhost:5173
// API      -> http://localhost:5198
//
// Do not force HTTPS redirect during local development.
// ============================================================

if (
    !app.Environment.IsDevelopment()
)
{
    app.UseHttpsRedirection();
}


// ============================================================
// SWAGGER
// ============================================================

app.UseSwagger();

app.UseSwaggerUI();


// ============================================================
// CORS
// ============================================================

app.UseCors(
    "GemoraCorsPolicy"
);


// ============================================================
// BLOCK DIRECT ACCESS TO LEGACY CERTIFICATES
//
// Certificates must be accessed through the protected API,
// not directly through /uploads/certificates/...
//
// This middleware must remain BEFORE UseStaticFiles().
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
// Keeps public legacy:
// - /uploads/profiles/...
// - /uploads/gem-images/...
//
// Legacy certificates are blocked by middleware above.
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
// START GEMORA API
// ============================================================

app.Run();

using System.Text;
using System.Security.Claims;

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
using Npgsql;

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

// Respect the configured Supabase endpoint: session and transaction poolers
// can have different availability and must not be substituted automatically.
var databaseConnection = new NpgsqlConnectionStringBuilder(connectionString);
if (databaseConnection.Host?.EndsWith(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase) == true)
{
    // Use fresh client connections to avoid reset-command stalls on the pooler.
    databaseConnection.Pooling = false;
    if (databaseConnection.Port == 6543)
    {
        // Supabase transaction pooling does not support prepared statements.
        databaseConnection.MaxAutoPrepare = 0;
        databaseConnection.Multiplexing = false;
    }
    databaseConnection.MaxPoolSize = Math.Min(databaseConnection.MaxPoolSize, 5);
    databaseConnection.MinPoolSize = 0;
    databaseConnection.ConnectionIdleLifetime = Math.Min(databaseConnection.ConnectionIdleLifetime, 60);
    databaseConnection.ConnectionPruningInterval = Math.Min(
        databaseConnection.ConnectionPruningInterval, databaseConnection.ConnectionIdleLifetime);
}

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseNpgsql(databaseConnection.ConnectionString);
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

    // ======================================================
    // MANUALLY ADD MISSING COLUMNS TO ORDERS TABLE
    // ======================================================
    
    try
    {
        Console.WriteLine("Ensuring Orders table exists...");
        
        // Recreate with correct Component 3 schema
        await dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS ""Orders"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""BuyerId"" uuid NOT NULL,
                ""SellerId"" uuid NOT NULL,
                ""GemListingId"" integer NULL,
                ""TotalAmount"" numeric(18,2) NOT NULL,
                ""Currency"" character varying(20) NOT NULL DEFAULT 'USD',
                ""Status"" character varying(50) NOT NULL DEFAULT 'Pending',
                ""ShippingAddress"" character varying(500) NOT NULL,
                ""ShippingRegion"" character varying(100) NOT NULL,
                ""ShippingCountryCode"" character varying(2) NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                ""UpdatedAt"" timestamp with time zone NULL,
                ""PaidAt"" timestamp with time zone NULL,
                CONSTRAINT ""FK_Orders_GemListings_GemListingId"" FOREIGN KEY (""GemListingId"") REFERENCES ""GemListings""(""Id""),
                CONSTRAINT ""FK_Orders_Users_BuyerId"" FOREIGN KEY (""BuyerId"") REFERENCES ""Users""(""Id""),
                CONSTRAINT ""FK_Orders_Users_SellerId"" FOREIGN KEY (""SellerId"") REFERENCES ""Users""(""Id"")
            );"
        );
        Console.WriteLine("Orders table ready.");
        
        // Create indexes for performance
        await dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE INDEX IF NOT EXISTS ""IX_Orders_BuyerId"" ON ""Orders""(""BuyerId"");
              CREATE INDEX IF NOT EXISTS ""IX_Orders_SellerId"" ON ""Orders""(""SellerId"");
              CREATE INDEX IF NOT EXISTS ""IX_Orders_GemListingId"" ON ""Orders""(""GemListingId"");
              CREATE INDEX IF NOT EXISTS ""IX_Orders_Status"" ON ""Orders""(""Status"");
              CREATE INDEX IF NOT EXISTS ""IX_Orders_CreatedAt"" ON ""Orders""(""CreatedAt"");"
        );
        Console.WriteLine("Created indexes for Orders table.");
    }
    catch (NpgsqlException ex) when (ex.IsTransient)
    {
        throw new InvalidOperationException(
            $"Cannot connect to PostgreSQL at {databaseConnection.Host}:{databaseConnection.Port}. " +
            "Startup cannot initialize the database. Check that the Supabase project is running, " +
            "copy the current pooler connection settings from the Supabase Connect dialog, " +
            "and check firewall/VPN access and database connection limits. " +
            "Run dotnet run --project Backend/Gemora.Database.Checks from the repository root " +
            "to test connectivity without changing data.", ex);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error initializing Orders table: {ex.Message}");
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

using System.Text;

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
// COMPONENT 4 - FILE STORAGE
// ============================================================

builder.Services.AddSingleton<
    Gemora.Application.Interfaces.IFileStorageService,
    Gemora.API.Services.LocalFileStorageService>();


// ============================================================
// AUTHENTICATION SERVICE
// ============================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService>();


// ============================================================
// PROFILE IMAGE STORAGE
// ============================================================

builder.Services.AddScoped<
    IProfileImageStorageService,
    ProfileImageStorageService>();


// ============================================================
// EMAIL SERVICE
// ============================================================

builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService>();


// ============================================================
// JWT SERVICE
// ============================================================

builder.Services.AddScoped<
    TokenService>();


// ============================================================
// EXPORT / COMPLIANCE
// ============================================================

builder.Services.AddScoped<
    IExportComplianceService,
    ExportComplianceService>();

builder.Services.AddScoped<
    IComplianceRulesService,
    ComplianceRulesService>();

builder.Services.AddScoped<
    IExportOfficerService,
    ExportOfficerService>();

builder.Services.AddScoped<
    IComplianceAgentToolService,
    ComplianceAgentToolService>();

builder.Services.AddScoped<
    IComplianceWorkflowService,
    ComplianceWorkflowService>();

builder.Services.Configure<
    GeminiComplianceOptions>(
        builder.Configuration
            .GetSection(
                GeminiComplianceOptions
                    .SectionName));

builder.Services.AddHttpClient<
    IComplianceAiClient,
    GeminiComplianceAiClient>();


// ============================================================
// GEM LISTING SERVICE
// ============================================================

builder.Services.AddScoped<
    IGemListingService,
    GemListingService>();


// ============================================================
// COMPONENT 2 - MARKETPLACE & TRANSACTIONS
// ============================================================

builder.Services.AddScoped<
    IMarketplaceService,
    MarketplaceService>();

builder.Services.AddScoped<
    IOrderService,
    OrderService>();

builder.Services.AddScoped<
    IMarketplaceAgentService,
    MarketplaceAgentService>();


// ============================================================
// GEM VERIFICATION
// ============================================================

builder.Services.AddScoped<
    IGemVerificationService,
    GemVerificationService>();

builder.Services.AddScoped<
    IGemEvidenceValidator,
    GemEvidenceValidator>();

builder.Services.AddScoped<
    IGemVerificationAgent,
    GemVerificationAgent>();


// ============================================================
// GEMINI SETTINGS
// ============================================================

builder.Services.Configure<
    GeminiOptions>(
        builder.Configuration
            .GetSection(
                GeminiOptions.SectionName));


// ============================================================
// GEMINI HTTP CLIENT
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
                TimeSpan.FromSeconds(
                    60);
        });


// ============================================================
// LOCAL GEM FILE STORAGE
// ============================================================

builder.Services.AddScoped<
    Gemora.Domain.Interfaces.IFileStorageService>(
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
                        environment
                            .ContentRootPath,
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

            return new
                Gemora.Infrastructure.Services
                    .LocalFileStorageService(
                        uploadRoot);
        });


// ============================================================
// GEM IMAGE READER
// ============================================================

builder.Services.AddScoped<
    IGemImageReader>(
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
                        environment
                            .ContentRootPath,
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

            return new
                LocalGemImageReader(
                    uploadRoot);
        });


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
                        "https://localhost:5175")
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
                            Encoding.UTF8
                                .GetBytes(
                                    jwtKey)),

                    ClockSkew =
                        TimeSpan.Zero
                };
        });


// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services
    .AddAuthorization();


// ============================================================
// SWAGGER
// ============================================================

builder.Services
    .AddEndpointsApiExplorer();

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
// DATABASE COMPATIBILITY + SEEDING
// ============================================================
//
// IMPORTANT:
//
// Historical EF migrations and current Component 2
// UUID transaction schema are currently not perfectly
// aligned.
//
// Do NOT automatically call Database.Migrate() here.
//
// This compatibility initializer only creates missing
// Component 2 runtime columns/tables.
//
// It does not delete existing orders.
// ============================================================

using (var scope =
       app.Services.CreateScope())
{
    var services =
        scope.ServiceProvider;

    try
    {
        var dbContext =
            services
                .GetRequiredService<
                    ApplicationDbContext>();


        // ----------------------------------------------------
        // Ensure Marketplace / Transaction schema exists
        // ----------------------------------------------------

        await DatabaseCompatibilityInitializer
            .EnsureComponent2SchemaAsync(
                dbContext);


        // ----------------------------------------------------
        // Existing project seeding
        // ----------------------------------------------------

        await DbSeeder.SeedAsync(
            dbContext,
            builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger =
            services
                .GetRequiredService<
                    ILogger<Program>>();

        logger.LogError(
            ex,
            "An error occurred while preparing the Gemora database.");

        throw;
    }
}


// ============================================================
// GLOBAL EXCEPTION HANDLER
// ============================================================

app.UseMiddleware<
    GlobalExceptionHandler>();


// ============================================================
// HTTPS
// ============================================================
//
// Local frontend:
// http://localhost:5173
//
// Local API:
// http://localhost:5198
//
// Avoid HTTP -> HTTPS redirect during local development.
// ============================================================

if (!app.Environment
        .IsDevelopment())
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
    "GemoraCorsPolicy");


// ============================================================
// STATIC FILES
// ============================================================
//
// Enables:
//
// /uploads/gem-images/...
// /uploads/profiles/...
// /uploads/certificates/...
// ============================================================

app.UseStaticFiles();


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
// START API
// ============================================================

app.Run();
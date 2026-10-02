
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

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SUPABASE STORAGE CLIENT (new sb_secret_ key)
// ============================================================
// The client reads its credentials from backend configuration.
// The key must never be added to React/Flutter or committed.

builder.Services.AddSingleton(new SupabaseStorageOptions
{
    ProjectUrl = builder.Configuration["SupabaseStorage:ProjectUrl"] ?? "",
    ServiceRoleKey = builder.Configuration["SupabaseStorage:ServiceRoleKey"] ?? ""
});

builder.Services.AddHttpClient<SupabaseStorageClient>();

// ============================================================
// WEB ROOT - retain local files during migration
// ============================================================

var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);

var uploadRootPath = Path.Combine(webRootPath, "uploads");
Directory.CreateDirectory(uploadRootPath);
Directory.CreateDirectory(Path.Combine(uploadRootPath, "profiles"));
Directory.CreateDirectory(Path.Combine(uploadRootPath, "gem-images"));
Directory.CreateDirectory(Path.Combine(uploadRootPath, "certificates"));

builder.Environment.WebRootPath = webRootPath;
builder.Environment.WebRootFileProvider = new PhysicalFileProvider(webRootPath);

// ============================================================
// POSTGRESQL / SUPABASE DATABASE
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is not configured."
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<IAuthService, AuthService>();

// Profile images remain local until their separate migration.
builder.Services.AddScoped<IProfileImageStorageService, ProfileImageStorageService>();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IGemListingService, GemListingService>();
builder.Services.AddScoped<IGemVerificationService, GemVerificationService>();
builder.Services.AddScoped<IGemEvidenceValidator, GemEvidenceValidator>();
builder.Services.AddScoped<IGemVerificationAgent, GemVerificationAgent>();

// ============================================================
// GEMINI CONFIGURATION + HTTP CLIENT
// ============================================================

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.SectionName)
);

builder.Services.AddHttpClient<IGemAiModelClient, GeminiGemAnalysisClient>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

// ============================================================
// LEGACY LOCAL STORAGE - still used for certificates and old files
// ============================================================

builder.Services.AddScoped<LocalFileStorageService>(serviceProvider =>
{
    var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
    var configuredWebRoot = environment.WebRootPath;

    if (string.IsNullOrWhiteSpace(configuredWebRoot))
    {
        configuredWebRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    var uploadDirectory = Path.Combine(configuredWebRoot, "uploads");
    Directory.CreateDirectory(uploadDirectory);
    return new LocalFileStorageService(uploadDirectory);
});

// ============================================================
// LEGACY LOCAL GEM IMAGE READER - for /uploads/gem-images/ URLs
// ============================================================

builder.Services.AddScoped<LocalGemImageReader>(serviceProvider =>
{
    var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
    var configuredWebRoot = environment.WebRootPath;

    if (string.IsNullOrWhiteSpace(configuredWebRoot))
    {
        configuredWebRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    var uploadDirectory = Path.Combine(configuredWebRoot, "uploads");
    Directory.CreateDirectory(uploadDirectory);
    return new LocalGemImageReader(uploadDirectory);
});

// ============================================================
// ACTIVE HYBRID STORAGE
// Gem images -> Supabase; certificates -> local temporarily.
// ============================================================

builder.Services.AddScoped<IFileStorageService, HybridFileStorageService>();

// The AI reader supports both old local images and new Supabase URLs.
builder.Services.AddScoped<IGemImageReader, HybridGemImageReader>();

// ============================================================
// CONTROLLERS + CORS
// ============================================================

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("GemoraCorsPolicy", policy =>
    {
        policy.WithOrigins(
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
    });
});

// ============================================================
// JWT AUTHENTICATION + AUTHORIZATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT signing key is not configured.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// SWAGGER + JWT AUTHORIZATION
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gemora API",
        Version = "v1",
        Description = "Gemora Marketplace ASP.NET Core Web API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// HTTP PIPELINE
// ============================================================

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();
app.UseSwagger();
app.UseSwaggerUI();

// Retained for old local images/profiles and temporary certificates.
// WARNING: local /uploads/certificates files remain publicly accessible
// until certificate serving is migrated behind authorization.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = builder.Environment.WebRootFileProvider
});

app.UseCors("GemoraCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ============================================================
// DATABASE SEEDING - existing staff accounts
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await DbSeeder.SeedAsync(dbContext, builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the Gemora database.");
        throw;
    }
}

app.Run();

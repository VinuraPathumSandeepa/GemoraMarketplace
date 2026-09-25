using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Gemora.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        // ==========================================
        // READ DEVELOPMENT PASSWORDS
        // FROM SECURE CONFIGURATION
        // ==========================================

        var adminPassword =
            configuration["SeedUsers:AdminPassword"];

        var gemologistPassword =
            configuration["SeedUsers:GemologistPassword"];

        var exportOfficerPassword =
            configuration["SeedUsers:ExportOfficerPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword) ||
            string.IsNullOrWhiteSpace(gemologistPassword) ||
            string.IsNullOrWhiteSpace(exportOfficerPassword))
        {
            throw new InvalidOperationException(
                "Seed user passwords are not configured."
            );
        }


        // ==========================================
        // ADMIN
        // ==========================================

        var adminEmail = "admin@gemora.com";

        if (!await context.Users.AnyAsync(
                u => u.Email == adminEmail))
        {
            context.Users.Add(new User
            {
                FullName = "Gemora Admin",

                Email = adminEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        adminPassword
                    ),

                Role = UserRoles.Admin,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // GEMOLOGIST
        // ==========================================

        var gemologistEmail =
            "gemologist@gemora.com";

        if (!await context.Users.AnyAsync(
                u => u.Email == gemologistEmail))
        {
            context.Users.Add(new User
            {
                FullName = "Gemora Gemologist",

                Email = gemologistEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        gemologistPassword
                    ),

                Role = UserRoles.Gemologist,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // EXPORT OFFICER
        // ==========================================

        var exportOfficerEmail =
            "export@gemora.com";

        if (!await context.Users.AnyAsync(
                u => u.Email == exportOfficerEmail))
        {
            context.Users.Add(new User
            {
                FullName = "Gemora Export Officer",

                Email = exportOfficerEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        exportOfficerPassword
                    ),

                Role = UserRoles.ExportOfficer,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // SAVE CHANGES
        // ==========================================

        await context.SaveChangesAsync();
    }
}
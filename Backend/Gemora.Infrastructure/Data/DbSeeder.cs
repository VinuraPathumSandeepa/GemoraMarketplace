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
        // ============================================================
        // READ DEVELOPMENT PASSWORDS
        // ============================================================

        var adminPassword =
            configuration["SeedUsers:AdminPassword"];

        var gemologistPassword =
            configuration["SeedUsers:GemologistPassword"];

        var exportOfficerPassword =
            configuration["SeedUsers:ExportOfficerPassword"];

        if (
            string.IsNullOrWhiteSpace(adminPassword) ||
            string.IsNullOrWhiteSpace(gemologistPassword) ||
            string.IsNullOrWhiteSpace(exportOfficerPassword)
        )
        {
            throw new InvalidOperationException(
                "Seed user passwords are not configured."
            );
        }


        // ============================================================
        // ADMIN
        // ============================================================

        var adminEmail =
            "admin@gemora.com";

        var admin =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email == adminEmail
                );

        if (admin == null)
        {
            admin = new User
            {
                FullName =
                    "Gemora Admin",

                Email =
                    adminEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        adminPassword
                    ),

                Role =
                    UserRoles.Admin,

                IsEmailVerified =
                    true,

                EmailVerifiedAt =
                    DateTime.UtcNow,

                CreatedAt =
                    DateTime.UtcNow
            };

            context.Users.Add(admin);
        }
        else
        {
            /*
             * Keep seeded development credentials
             * synchronized with User Secrets.
             *
             * BCrypt hashes contain random salts,
             * therefore we VERIFY instead of comparing
             * hash strings.
             */

            var adminPasswordMatches =
                BCrypt.Net.BCrypt.Verify(
                    adminPassword,
                    admin.PasswordHash
                );

            if (!adminPasswordMatches)
            {
                admin.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        adminPassword
                    );
            }

            admin.Role =
                UserRoles.Admin;

            admin.IsEmailVerified =
                true;

            admin.EmailVerifiedAt ??=
                DateTime.UtcNow;
        }


        // ============================================================
        // GEMOLOGIST
        // ============================================================

        var gemologistEmail =
            "gemologist@gemora.com";

        var gemologist =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email ==
                        gemologistEmail
                );

        if (gemologist == null)
        {
            gemologist = new User
            {
                FullName =
                    "Gemora Gemologist",

                Email =
                    gemologistEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        gemologistPassword
                    ),

                Role =
                    UserRoles.Gemologist,

                IsEmailVerified =
                    true,

                EmailVerifiedAt =
                    DateTime.UtcNow,

                CreatedAt =
                    DateTime.UtcNow
            };

            context.Users.Add(
                gemologist
            );
        }
        else
        {
            /*
             * Do NOT delete or recreate the existing
             * Gemologist.
             *
             * Existing User Id and all related
             * GemVerification records remain intact.
             */

            var gemologistPasswordMatches =
                BCrypt.Net.BCrypt.Verify(
                    gemologistPassword,
                    gemologist.PasswordHash
                );

            if (!gemologistPasswordMatches)
            {
                gemologist.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        gemologistPassword
                    );
            }

            gemologist.Role =
                UserRoles.Gemologist;

            gemologist.IsEmailVerified =
                true;

            gemologist.EmailVerifiedAt ??=
                DateTime.UtcNow;
        }


        // ============================================================
        // EXPORT OFFICER
        // ============================================================

        var exportOfficerEmail =
            "export@gemora.com";

        var exportOfficer =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email ==
                        exportOfficerEmail
                );

        if (exportOfficer == null)
        {
            exportOfficer = new User
            {
                FullName =
                    "Gemora Export Officer",

                Email =
                    exportOfficerEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        exportOfficerPassword
                    ),

                Role =
                    UserRoles.ExportOfficer,

                IsEmailVerified =
                    true,

                EmailVerifiedAt =
                    DateTime.UtcNow,

                CreatedAt =
                    DateTime.UtcNow
            };

            context.Users.Add(
                exportOfficer
            );
        }
        else
        {
            var exportOfficerPasswordMatches =
                BCrypt.Net.BCrypt.Verify(
                    exportOfficerPassword,
                    exportOfficer.PasswordHash
                );

            if (!exportOfficerPasswordMatches)
            {
                exportOfficer.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        exportOfficerPassword
                    );
            }

            exportOfficer.Role =
                UserRoles.ExportOfficer;

            exportOfficer.IsEmailVerified =
                true;

            exportOfficer.EmailVerifiedAt ??=
                DateTime.UtcNow;
        }


        // ============================================================
        // SAVE
        // ============================================================

        await context.SaveChangesAsync();
    }
}
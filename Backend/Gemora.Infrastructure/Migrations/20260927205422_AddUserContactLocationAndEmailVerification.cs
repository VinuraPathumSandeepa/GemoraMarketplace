using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContactLocationAndEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // USER CONTACT / LOCATION
            // ============================================================

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Users",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            /*
             * Existing users were created before email verification
             * was introduced.
             *
             * Keep those existing accounts active and verified.
             *
             * New Buyer/Seller registrations will explicitly set
             * IsEmailVerified = false until the OTP is verified.
             */
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "IsEmailVerified" = TRUE;
                """
            );

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // ============================================================
            // GEM LISTING LOCATION
            // ============================================================

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "GemListings",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "GemListings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // ============================================================
            // EMAIL VERIFICATION CODES
            // ============================================================

            migrationBuilder.CreateTable(
                name: "EmailVerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    UserId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    CodeHash = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false),

                    ExpiresAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    AttemptCount = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    UsedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_EmailVerificationCodes",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_EmailVerificationCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ============================================================
            // INDEXES
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_GemListings_CountryCode",
                table: "GemListings",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_GemListings_CountryCode_Region",
                table: "GemListings",
                columns: new[]
                {
                    "CountryCode",
                    "Region"
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationCodes_UserId",
                table: "EmailVerificationCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationCodes_UserId_CreatedAt",
                table: "EmailVerificationCodes",
                columns: new[]
                {
                    "UserId",
                    "CreatedAt"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // REMOVE EMAIL VERIFICATION TABLE
            // ============================================================

            migrationBuilder.DropTable(
                name: "EmailVerificationCodes");

            // ============================================================
            // REMOVE INDEXES
            // ============================================================

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_GemListings_CountryCode",
                table: "GemListings");

            migrationBuilder.DropIndex(
                name: "IX_GemListings_CountryCode_Region",
                table: "GemListings");

            // ============================================================
            // REMOVE USER FIELDS
            // ============================================================

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Users");

            // ============================================================
            // REMOVE LISTING LOCATION FIELDS
            // ============================================================

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "GemListings");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "GemListings");
        }
    }
}
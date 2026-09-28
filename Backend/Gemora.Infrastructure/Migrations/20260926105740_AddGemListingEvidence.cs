using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGemListingEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateAuthority",
                table: "GemListings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateNumber",
                table: "GemListings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateUrl",
                table: "GemListings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageUrl",
                table: "GemListings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GemListings_CertificateNumber",
                table: "GemListings",
                column: "CertificateNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GemListings_CertificateNumber",
                table: "GemListings");

            migrationBuilder.DropColumn(
                name: "CertificateAuthority",
                table: "GemListings");

            migrationBuilder.DropColumn(
                name: "CertificateNumber",
                table: "GemListings");

            migrationBuilder.DropColumn(
                name: "CertificateUrl",
                table: "GemListings");

            migrationBuilder.DropColumn(
                name: "PrimaryImageUrl",
                table: "GemListings");
        }
    }
}

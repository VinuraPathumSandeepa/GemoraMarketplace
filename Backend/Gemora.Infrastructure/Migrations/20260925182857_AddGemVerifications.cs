using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGemVerifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GemVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GemListingId = table.Column<int>(type: "integer", nullable: false),
                    GemologistId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AiSuggestedGemType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AiFindings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AiRiskFlags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AiStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GemVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GemVerifications_GemListings_GemListingId",
                        column: x => x.GemListingId,
                        principalTable: "GemListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GemVerifications_Users_GemologistId",
                        column: x => x.GemologistId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GemVerifications_AiStatus",
                table: "GemVerifications",
                column: "AiStatus");

            migrationBuilder.CreateIndex(
                name: "IX_GemVerifications_Decision",
                table: "GemVerifications",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_GemVerifications_GemListingId",
                table: "GemVerifications",
                column: "GemListingId");

            migrationBuilder.CreateIndex(
                name: "IX_GemVerifications_GemologistId",
                table: "GemVerifications",
                column: "GemologistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GemVerifications");
        }
    }
}

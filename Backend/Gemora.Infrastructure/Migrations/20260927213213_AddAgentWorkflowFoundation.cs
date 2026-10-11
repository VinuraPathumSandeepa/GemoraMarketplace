using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentWorkflowFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WorkflowType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    PlanJson = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    FinalSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RootEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RootEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkflows_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InputSummaryJson = table.Column<string>(type: "jsonb", nullable: true),
                    OutputSummaryJson = table.Column<string>(type: "jsonb", nullable: true),
                    ValidationResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkflowSteps_AgentWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "AgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentToolCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    InputSummaryJson = table.Column<string>(type: "jsonb", nullable: true),
                    OutputSummaryJson = table.Column<string>(type: "jsonb", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentToolCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentToolCalls_AgentWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "AgentWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolCalls_ToolName",
                table: "AgentToolCalls",
                column: "ToolName");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolCalls_WorkflowStepId",
                table: "AgentToolCalls",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolCalls_WorkflowStepId_ToolName_AttemptNumber",
                table: "AgentToolCalls",
                columns: new[] { "WorkflowStepId", "ToolName", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_RootEntityType_RootEntityId",
                table: "AgentWorkflows",
                columns: new[] { "RootEntityType", "RootEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_Status",
                table: "AgentWorkflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_TriggeredByUserId",
                table: "AgentWorkflows",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_WorkflowType",
                table: "AgentWorkflows",
                column: "WorkflowType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflowSteps_Status",
                table: "AgentWorkflowSteps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflowSteps_WorkflowId",
                table: "AgentWorkflowSteps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflowSteps_WorkflowId_StepNumber",
                table: "AgentWorkflowSteps",
                columns: new[] { "WorkflowId", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentToolCalls");

            migrationBuilder.DropTable(
                name: "AgentWorkflowSteps");

            migrationBuilder.DropTable(
                name: "AgentWorkflows");
        }
    }
}

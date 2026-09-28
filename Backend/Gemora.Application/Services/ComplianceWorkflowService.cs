using System.Diagnostics;
using System.Text.Json;
using Gemora.Application.AgentTools;
using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ComplianceWorkflowService : IComplianceWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly IComplianceAgentToolService _toolService;

    public ComplianceWorkflowService(
        ApplicationDbContext context,
        IComplianceAgentToolService toolService)
    {
        _context = context;
        _toolService = toolService;
    }

    public async Task<ComplianceWorkflowExecutionResult> StartContextCollectionAsync(
        Guid triggeredByUserId,
        Guid exportRequestId,
        CancellationToken cancellationToken = default)
    {
        // 1. Input Validation
        if (triggeredByUserId == Guid.Empty)
        {
            return new ComplianceWorkflowExecutionResult
            {
                Success = false,
                ErrorCode = "INVALID_USER",
                Message = "Triggered by user ID is invalid."
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceWorkflowExecutionResult
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        // 2. User & Request Existence Checks
        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == triggeredByUserId, cancellationToken);

        if (!userExists)
        {
            return new ComplianceWorkflowExecutionResult
            {
                Success = false,
                ErrorCode = "USER_NOT_FOUND",
                Message = "Triggered user was not found."
            };
        }

        var requestExists = await _context.ExportRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == exportRequestId, cancellationToken);

        if (!requestExists)
        {
            return new ComplianceWorkflowExecutionResult
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        // 3. Workflow Duplication Protection
        var activeStatuses = new[]
        {
            AgentWorkflowStatus.Pending,
            AgentWorkflowStatus.Planning,
            AgentWorkflowStatus.Running,
            AgentWorkflowStatus.WaitingForApproval
        };

        var activeWorkflow = await _context.AgentWorkflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.WorkflowType == "ExportCompliance" &&
                w.RootEntityType == "ExportRequest" &&
                w.RootEntityId == exportRequestId &&
                activeStatuses.Contains(w.Status),
                cancellationToken);

        if (activeWorkflow != null)
        {
            return new ComplianceWorkflowExecutionResult
            {
                Success = false,
                ErrorCode = "WORKFLOW_ALREADY_ACTIVE",
                Message = $"An active export compliance workflow ({activeWorkflow.Id}) already exists for this export request.",
                WorkflowId = activeWorkflow.Id
            };
        }

        // 4. Create Structured Workflow Plan
        var planObj = new
        {
            version = "1.0",
            workflowType = "ExportCompliance",
            steps = new object[]
            {
                new
                {
                    stepNumber = 1,
                    actorType = "Agent",
                    actor = "ComplianceRequirementsAgent",
                    action = "CollectValidatedComplianceContext",
                    tools = new[]
                    {
                        ComplianceAgentToolNames.ReadExportRequest,
                        ComplianceAgentToolNames.ReadComplianceDocumentMetadata,
                        ComplianceAgentToolNames.RunComplianceRulesValidator
                    }
                },
                new
                {
                    stepNumber = 2,
                    actorType = "Agent",
                    actor = "ComplianceRequirementsAgent",
                    action = "GenerateStructuredComplianceAssessment"
                },
                new
                {
                    stepNumber = 3,
                    actorType = "Agent",
                    actor = "ComplianceRequirementsAgent",
                    action = "ValidateStructuredAssessment"
                },
                new
                {
                    stepNumber = 4,
                    actorType = "Human",
                    actor = "ExportOfficer",
                    action = "HumanReviewAndDecision"
                }
            }
        };

        var planJson = JsonSerializer.Serialize(planObj);

        // 5. Create AgentWorkflow Entity
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            Objective = "Prepare an advisory compliance review package for the export request.",
            WorkflowType = "ExportCompliance",
            TriggeredByUserId = triggeredByUserId,
            Status = AgentWorkflowStatus.Planning,
            CurrentStep = 1,
            PlanJson = planJson,
            ApprovalStatus = AgentApprovalStatus.NotRequired,
            RootEntityType = "ExportRequest",
            RootEntityId = exportRequestId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 6. Create First Workflow Step Entity
        var inputSummaryObj = new { exportRequestId = exportRequestId.ToString() };

        var step = new AgentWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepNumber = 1,
            AgentName = "ComplianceRequirementsAgent",
            Action = "CollectValidatedComplianceContext",
            Status = AgentWorkflowStepStatus.Running,
            InputSummaryJson = JsonSerializer.Serialize(inputSummaryObj),
            StartedAt = DateTime.UtcNow
        };

        workflow.Status = AgentWorkflowStatus.Running;

        _context.AgentWorkflows.Add(workflow);
        _context.AgentWorkflowSteps.Add(step);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // ======================================================
            // TOOL 1: readExportRequest
            // ======================================================
            if (!ComplianceAgentToolNames.All.Contains(ComplianceAgentToolNames.ReadExportRequest))
            {
                return await RecordToolFailureAndReturnAsync(
                    workflow, step, ComplianceAgentToolNames.ReadExportRequest,
                    "TOOL_NOT_ALLOWED", "Tool is not in the approved allow-list.", cancellationToken);
            }

            var sw1 = Stopwatch.StartNew();
            var res1 = await _toolService.ReadExportRequestAsync(exportRequestId, cancellationToken);
            sw1.Stop();

            var toolCall1 = new AgentToolCall
            {
                Id = Guid.NewGuid(),
                WorkflowStepId = step.Id,
                ToolName = ComplianceAgentToolNames.ReadExportRequest,
                AttemptNumber = 1,
                Succeeded = res1.Success,
                DurationMs = sw1.ElapsedMilliseconds,
                ErrorCode = res1.ErrorCode,
                ErrorMessage = res1.Success ? null : res1.Message,
                InputSummaryJson = JsonSerializer.Serialize(new { exportRequestId = exportRequestId.ToString() }),
                OutputSummaryJson = res1.Success && res1.Data != null
                    ? JsonSerializer.Serialize(new
                    {
                        retrieved = true,
                        status = res1.Data.Status,
                        originPresent = !string.IsNullOrWhiteSpace(res1.Data.OriginCountry),
                        destinationPresent = !string.IsNullOrWhiteSpace(res1.Data.DestinationCountry)
                    })
                    : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.AgentToolCalls.Add(toolCall1);

            if (!res1.Success || res1.Data == null)
            {
                await _context.SaveChangesAsync(cancellationToken);
                return await HandleStepAndWorkflowFailureAsync(
                    workflow, step, res1.ErrorCode ?? "TOOL_FAILED", res1.Message, cancellationToken);
            }

            // ======================================================
            // TOOL 2: readComplianceDocumentMetadata
            // ======================================================
            if (!ComplianceAgentToolNames.All.Contains(ComplianceAgentToolNames.ReadComplianceDocumentMetadata))
            {
                await _context.SaveChangesAsync(cancellationToken);
                return await RecordToolFailureAndReturnAsync(
                    workflow, step, ComplianceAgentToolNames.ReadComplianceDocumentMetadata,
                    "TOOL_NOT_ALLOWED", "Tool is not in the approved allow-list.", cancellationToken);
            }

            var sw2 = Stopwatch.StartNew();
            var res2 = await _toolService.ReadComplianceDocumentMetadataAsync(exportRequestId, cancellationToken);
            sw2.Stop();

            var toolCall2 = new AgentToolCall
            {
                Id = Guid.NewGuid(),
                WorkflowStepId = step.Id,
                ToolName = ComplianceAgentToolNames.ReadComplianceDocumentMetadata,
                AttemptNumber = 1,
                Succeeded = res2.Success,
                DurationMs = sw2.ElapsedMilliseconds,
                ErrorCode = res2.ErrorCode,
                ErrorMessage = res2.Success ? null : res2.Message,
                InputSummaryJson = JsonSerializer.Serialize(new { exportRequestId = exportRequestId.ToString() }),
                OutputSummaryJson = res2.Success && res2.Data != null
                    ? JsonSerializer.Serialize(new
                    {
                        documentCount = res2.Data.Count,
                        documentsWithFiles = res2.Data.Count(d => d.HasUploadedFile)
                    })
                    : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.AgentToolCalls.Add(toolCall2);

            if (!res2.Success || res2.Data == null)
            {
                await _context.SaveChangesAsync(cancellationToken);
                return await HandleStepAndWorkflowFailureAsync(
                    workflow, step, res2.ErrorCode ?? "TOOL_FAILED", res2.Message, cancellationToken);
            }

            // ======================================================
            // TOOL 3: runComplianceRulesValidator
            // ======================================================
            if (!ComplianceAgentToolNames.All.Contains(ComplianceAgentToolNames.RunComplianceRulesValidator))
            {
                await _context.SaveChangesAsync(cancellationToken);
                return await RecordToolFailureAndReturnAsync(
                    workflow, step, ComplianceAgentToolNames.RunComplianceRulesValidator,
                    "TOOL_NOT_ALLOWED", "Tool is not in the approved allow-list.", cancellationToken);
            }

            var sw3 = Stopwatch.StartNew();
            var res3 = await _toolService.RunComplianceRulesValidatorAsync(exportRequestId, cancellationToken);
            sw3.Stop();

            var toolCall3 = new AgentToolCall
            {
                Id = Guid.NewGuid(),
                WorkflowStepId = step.Id,
                ToolName = ComplianceAgentToolNames.RunComplianceRulesValidator,
                AttemptNumber = 1,
                Succeeded = res3.Success,
                DurationMs = sw3.ElapsedMilliseconds,
                ErrorCode = res3.ErrorCode,
                ErrorMessage = res3.Success ? null : res3.Message,
                InputSummaryJson = JsonSerializer.Serialize(new { exportRequestId = exportRequestId.ToString() }),
                OutputSummaryJson = res3.Success && res3.Data != null
                    ? JsonSerializer.Serialize(new
                    {
                        isComplete = res3.Data.IsComplete,
                        missingRequirementCount = res3.Data.MissingRequirements.Count,
                        warningCount = res3.Data.Warnings.Count,
                        invalidDocumentCount = res3.Data.InvalidDocuments.Count
                    })
                    : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.AgentToolCalls.Add(toolCall3);

            if (!res3.Success || res3.Data == null)
            {
                await _context.SaveChangesAsync(cancellationToken);
                return await HandleStepAndWorkflowFailureAsync(
                    workflow, step, res3.ErrorCode ?? "TOOL_FAILED", res3.Message, cancellationToken);
            }

            // ======================================================
            // SUCCESS PATH (All 3 tools executed cleanly)
            // ======================================================
            var context = new ComplianceAgentContextDto
            {
                ExportRequest = res1.Data,
                Documents = res2.Data,
                DeterministicCheck = res3.Data
            };

            step.Status = AgentWorkflowStepStatus.Succeeded;
            step.CompletedAt = DateTime.UtcNow;
            step.OutputSummaryJson = JsonSerializer.Serialize(new
            {
                contextPrepared = true,
                documentCount = res2.Data.Count,
                deterministicComplete = res3.Data.IsComplete,
                missingRequirementCount = res3.Data.MissingRequirements.Count,
                warningCount = res3.Data.Warnings.Count,
                invalidDocumentCount = res3.Data.InvalidDocuments.Count
            });
            step.ValidationResultJson = JsonSerializer.Serialize(new
            {
                toolAllowListPassed = true,
                allToolCallsSucceeded = true,
                safeContextCreated = true
            });

            workflow.CurrentStep = 2;
            workflow.Status = AgentWorkflowStatus.Running;
            workflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new ComplianceWorkflowExecutionResult
            {
                Success = true,
                Message = "Compliance context collected and validated successfully.",
                WorkflowId = workflow.Id,
                WorkflowStepId = step.Id,
                Context = context
            };
        }
        catch (Exception)
        {
            return await HandleStepAndWorkflowFailureAsync(
                workflow, step, "WORKFLOW_EXECUTION_FAILED",
                "An unexpected error occurred during workflow context collection.", cancellationToken);
        }
    }

    // ======================================================
    // HELPER: RECORD TOOL FAILURE AND MARK WORKFLOW FAILED
    // ======================================================
    private async Task<ComplianceWorkflowExecutionResult> RecordToolFailureAndReturnAsync(
        AgentWorkflow workflow,
        AgentWorkflowStep step,
        string toolName,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var failedToolCall = new AgentToolCall
        {
            Id = Guid.NewGuid(),
            WorkflowStepId = step.Id,
            ToolName = toolName,
            AttemptNumber = 1,
            Succeeded = false,
            DurationMs = 0,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        _context.AgentToolCalls.Add(failedToolCall);
        await _context.SaveChangesAsync(cancellationToken);

        return await HandleStepAndWorkflowFailureAsync(
            workflow, step, errorCode, errorMessage, cancellationToken);
    }

    // ======================================================
    // HELPER: MARK STEP & WORKFLOW FAILED AND SAVE
    // ======================================================
    private async Task<ComplianceWorkflowExecutionResult> HandleStepAndWorkflowFailureAsync(
        AgentWorkflow workflow,
        AgentWorkflowStep step,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        step.Status = AgentWorkflowStepStatus.Failed;
        step.ErrorCode = errorCode;
        step.ErrorMessage = errorMessage;
        step.CompletedAt = DateTime.UtcNow;

        workflow.Status = AgentWorkflowStatus.Failed;
        workflow.ErrorCode = errorCode;
        workflow.ErrorMessage = errorMessage;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ComplianceWorkflowExecutionResult
        {
            Success = false,
            ErrorCode = errorCode,
            Message = errorMessage,
            WorkflowId = workflow.Id,
            WorkflowStepId = step.Id,
            Context = null
        };
    }
}

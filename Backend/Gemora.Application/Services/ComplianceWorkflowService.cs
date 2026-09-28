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
    private readonly IComplianceAiClient _aiClient;

    public ComplianceWorkflowService(
        ApplicationDbContext context,
        IComplianceAgentToolService toolService,
        IComplianceAiClient aiClient)
    {
        _context = context;
        _toolService = toolService;
        _aiClient = aiClient;
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
                    actorType = "System",
                    actor = "ComplianceOutputValidator",
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

    // ======================================================
    // WORKFLOW ANALYSIS (STEP 2 + STEP 3 INTEGRATION)
    // ======================================================
    public async Task<ComplianceWorkflowAnalysisResultDto> RunComplianceAnalysisAsync(
        Guid triggeredByUserId,
        Guid exportRequestId,
        CancellationToken cancellationToken = default)
    {
        if (triggeredByUserId == Guid.Empty)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "INVALID_USER",
                Message = "Authenticated user is invalid."
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        // 1. Ownership & Existence Precondition check (IDOR Protection)
        var exportRequest = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == triggeredByUserId, cancellationToken);

        if (exportRequest == null)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        if (exportRequest.Status != ExportRequestStatus.Submitted)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "INVALID_EXPORT_STATUS",
                Message = $"Compliance analysis can only be run on export requests in Submitted status. Current status is {exportRequest.Status}."
            };
        }

        // 2. Reuse Step 1 Context Collection
        var step1Result = await StartContextCollectionAsync(triggeredByUserId, exportRequestId, cancellationToken);

        if (!step1Result.Success || step1Result.Context == null)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = step1Result.ErrorCode ?? "STEP1_FAILED",
                Message = step1Result.Message,
                WorkflowId = step1Result.WorkflowId
            };
        }

        // 3. Update ExportRequest Status to UnderComplianceReview
        var trackedRequest = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == triggeredByUserId, cancellationToken);

        if (trackedRequest == null || trackedRequest.Status != ExportRequestStatus.Submitted)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "INVALID_EXPORT_STATUS",
                Message = "Export request status changed during workflow execution.",
                WorkflowId = step1Result.WorkflowId
            };
        }

        trackedRequest.Status = ExportRequestStatus.UnderComplianceReview;
        trackedRequest.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Load Workflow Entity for Step 2
        var workflow = await _context.AgentWorkflows
            .FirstOrDefaultAsync(w => w.Id == step1Result.WorkflowId, cancellationToken);

        if (workflow == null ||
            workflow.Status != AgentWorkflowStatus.Running ||
            workflow.CurrentStep != 2 ||
            workflow.RootEntityType != "ExportRequest" ||
            workflow.RootEntityId != exportRequestId)
        {
            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "WORKFLOW_STATE_INVALID",
                Message = "Workflow state is invalid for Step 2 execution.",
                WorkflowId = step1Result.WorkflowId
            };
        }

        // 5. Create Step 2 AgentWorkflowStep Entity
        var step2InputSummary = new
        {
            documentCount = step1Result.Context.Documents.Count,
            deterministicComplete = step1Result.Context.DeterministicCheck.IsComplete,
            missingRequirementCount = step1Result.Context.DeterministicCheck.MissingRequirements.Count,
            warningCount = step1Result.Context.DeterministicCheck.Warnings.Count
        };

        var step2 = new AgentWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepNumber = 2,
            AgentName = "ComplianceRequirementsAgent",
            Action = "GenerateStructuredComplianceAssessment",
            Status = AgentWorkflowStepStatus.Running,
            InputSummaryJson = JsonSerializer.Serialize(step2InputSummary),
            StartedAt = DateTime.UtcNow
        };

        _context.AgentWorkflowSteps.Add(step2);
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Invoke AI Client for Step 2
        ComplianceAiClientResult aiResult;
        try
        {
            aiResult = await _aiClient.AnalyzeAsync(step1Result.Context, cancellationToken);
        }
        catch (Exception ex)
        {
            return await HandleStep2FailureAsync(workflow, step2, "AI_ANALYSIS_FAILED", ex.Message, cancellationToken);
        }

        if (!aiResult.Success || aiResult.Result == null)
        {
            return await HandleStep2FailureAsync(workflow, step2, aiResult.ErrorCode ?? "AI_ANALYSIS_FAILED", aiResult.Message, cancellationToken);
        }

        // Step 2 Success Persistence
        step2.Status = AgentWorkflowStepStatus.Succeeded;
        step2.CompletedAt = DateTime.UtcNow;
        step2.OutputSummaryJson = JsonSerializer.Serialize(aiResult.Result);
        step2.ValidationResultJson = JsonSerializer.Serialize(new
        {
            providerResponseParsed = true,
            clientValidationPassed = true,
            modelName = aiResult.ModelName,
            durationMs = aiResult.DurationMs
        });

        workflow.CurrentStep = 3;
        workflow.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Create Step 3 AgentWorkflowStep Entity
        var assessment = aiResult.Result;
        var step3InputSummary = new
        {
            sourceStepNumber = 2,
            documentFindingCount = assessment.DocumentFindings.Count,
            missingRequirementCount = assessment.MissingRequirements.Count,
            confidencePresent = true
        };

        var step3 = new AgentWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            StepNumber = 3,
            AgentName = "ComplianceOutputValidator",
            Action = "ValidateStructuredAssessment",
            Status = AgentWorkflowStepStatus.Running,
            InputSummaryJson = JsonSerializer.Serialize(step3InputSummary),
            StartedAt = DateTime.UtcNow
        };

        _context.AgentWorkflowSteps.Add(step3);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Deterministic Workflow Validation (Step 3)
        string? step3ValidationError = ValidateWorkflowAssessment(assessment, step1Result.Context);

        if (step3ValidationError != null)
        {
            step3.Status = AgentWorkflowStepStatus.Failed;
            step3.ErrorCode = "AI_WORKFLOW_VALIDATION_FAILED";
            step3.ErrorMessage = step3ValidationError;
            step3.CompletedAt = DateTime.UtcNow;

            workflow.Status = AgentWorkflowStatus.Failed;
            workflow.ErrorCode = "AI_WORKFLOW_VALIDATION_FAILED";
            workflow.ErrorMessage = step3ValidationError;
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new ComplianceWorkflowAnalysisResultDto
            {
                Success = false,
                ErrorCode = "AI_WORKFLOW_VALIDATION_FAILED",
                Message = step3ValidationError,
                WorkflowId = workflow.Id,
                AgentStepId = step2.Id,
                ValidationStepId = step3.Id
            };
        }

        // Step 3 Success Persistence
        step3.Status = AgentWorkflowStepStatus.Succeeded;
        step3.CompletedAt = DateTime.UtcNow;
        step3.OutputSummaryJson = JsonSerializer.Serialize(new
        {
            validated = true,
            deterministicComplete = assessment.DeterministicComplete,
            documentFindingCount = assessment.DocumentFindings.Count,
            missingRequirementCount = assessment.MissingRequirements.Count,
            warningCount = assessment.Warnings.Count,
            recommendedOfficerCheckCount = assessment.RecommendedOfficerChecks.Count
        });
        step3.ValidationResultJson = JsonSerializer.Serialize(new
        {
            schemaContractPassed = true,
            deterministicConsistencyPassed = true,
            documentReferenceValidationPassed = true,
            advisoryBoundaryPassed = true
        });

        // 9. Update Workflow to WaitingForApproval
        workflow.CurrentStep = 4;
        workflow.Status = AgentWorkflowStatus.WaitingForApproval;
        workflow.ApprovalStatus = AgentApprovalStatus.Pending;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ComplianceWorkflowAnalysisResultDto
        {
            Success = true,
            Message = "Compliance workflow analysis completed successfully and is waiting for officer approval.",
            WorkflowId = workflow.Id,
            AgentStepId = step2.Id,
            ValidationStepId = step3.Id,
            Assessment = assessment
        };
    }

    private async Task<ComplianceWorkflowAnalysisResultDto> HandleStep2FailureAsync(
        AgentWorkflow workflow,
        AgentWorkflowStep step2,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        step2.Status = AgentWorkflowStepStatus.Failed;
        step2.ErrorCode = errorCode;
        step2.ErrorMessage = errorMessage;
        step2.CompletedAt = DateTime.UtcNow;

        workflow.Status = AgentWorkflowStatus.Failed;
        workflow.ErrorCode = errorCode;
        workflow.ErrorMessage = errorMessage;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ComplianceWorkflowAnalysisResultDto
        {
            Success = false,
            ErrorCode = errorCode,
            Message = errorMessage,
            WorkflowId = workflow.Id,
            AgentStepId = step2.Id
        };
    }

    private static string? ValidateWorkflowAssessment(
        ComplianceAgentResultDto assessment,
        ComplianceAgentContextDto context)
    {
        if (assessment == null)
        {
            return "Assessment is null.";
        }

        if (assessment.MissingRequirements == null ||
            assessment.DocumentFindings == null ||
            assessment.Inconsistencies == null ||
            assessment.Warnings == null ||
            assessment.RecommendedOfficerChecks == null)
        {
            return "Assessment contains a null required collection.";
        }

        if (string.IsNullOrWhiteSpace(assessment.Summary))
        {
            return "AI summary must not be empty.";
        }

        if (assessment.Confidence < 0.0 || assessment.Confidence > 1.0)
        {
            return "AI confidence score must be between 0.0 and 1.0.";
        }

        if (assessment.DeterministicComplete != context.DeterministicCheck.IsComplete)
        {
            return "AI deterministic completeness contradicts application rules.";
        }

        var contextMissingReqs = context.DeterministicCheck.MissingRequirements ?? new List<string>();
        foreach (var req in contextMissingReqs)
        {
            if (!assessment.MissingRequirements.Any(r => string.Equals(r, req, StringComparison.OrdinalIgnoreCase)))
            {
                return $"AI result omitted deterministic missing requirement: '{req}'.";
            }
        }

        var allowedSeverities = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Info", "Warning", "Critical" };
        var docMap = (context.Documents ?? Array.Empty<ComplianceAgentDocumentDto>())
            .Where(d => d.DocumentId != Guid.Empty)
            .ToDictionary(d => d.DocumentId, d => d, EqualityComparer<Guid>.Default);

        foreach (var finding in assessment.DocumentFindings)
        {
            if (finding.DocumentId == Guid.Empty)
            {
                return "AI document finding has empty DocumentId.";
            }

            if (!docMap.TryGetValue(finding.DocumentId, out var contextDoc))
            {
                return $"AI referenced an invalid document ID '{finding.DocumentId}'.";
            }

            if (string.IsNullOrWhiteSpace(finding.Finding))
            {
                return "AI document finding has empty Finding.";
            }

            if (!string.Equals(finding.DocumentType, contextDoc.DocumentType, StringComparison.OrdinalIgnoreCase))
            {
                return "AI document type does not match the validated document metadata.";
            }

            if (string.IsNullOrWhiteSpace(finding.Severity) || !allowedSeverities.Contains(finding.Severity))
            {
                return $"Invalid document finding severity: '{finding.Severity}'.";
            }
        }

        const string expectedDisclaimer = "AI-generated advisory assessment. Final export decisions must be made by an authorized Export Officer.";
        if (!string.Equals(assessment.Disclaimer, expectedDisclaimer, StringComparison.Ordinal))
        {
            return "AI result disclaimer does not match the backend advisory disclaimer.";
        }

        return null;
    }
}

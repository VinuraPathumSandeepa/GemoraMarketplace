using System.Text.Json;
using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ExportOfficerService : IExportOfficerService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IComplianceRulesService _complianceRulesService;

    public ExportOfficerService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IComplianceRulesService complianceRulesService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _complianceRulesService = complianceRulesService;
    }

    // ==========================================
    // 1. GET REVIEW QUEUE
    // ==========================================
    public async Task<ExportOfficerOperationResult> GetReviewQueueAsync(Guid officerUserId)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        var queueStatuses = new[]
        {
            ExportRequestStatus.Submitted,
            ExportRequestStatus.UnderComplianceReview,
            ExportRequestStatus.UnderOfficerReview,
            ExportRequestStatus.RevisionRequired,
            ExportRequestStatus.Approved,
            ExportRequestStatus.Rejected
        };

        var requests = await _context.ExportRequests
            .AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .Where(r => queueStatuses.Contains(r.Status))
            .OrderBy(r => r.SubmittedAt)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        var mapped = requests.Select(MapToOfficerExportRequestResponseDto).ToList();

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = "Export review queue retrieved successfully.",
            Requests = mapped
        };
    }

    // ==========================================
    // 2. GET REQUEST FOR REVIEW
    // ==========================================
    public async Task<ExportOfficerOperationResult> GetRequestForReviewAsync(
        Guid officerUserId,
        Guid exportRequestId)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId);

        if (request == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status == ExportRequestStatus.Draft ||
            request.Status == ExportRequestStatus.Cancelled)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "This export request is not available for officer review.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = "Export request retrieved successfully.",
            Request = await MapToOfficerExportRequestResponseDtoAsync(request)
        };
    }

    // ==========================================
    // 3. START REVIEW
    // ==========================================
    public async Task<ExportOfficerOperationResult> StartReviewAsync(
        Guid officerUserId,
        Guid exportRequestId)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId);

        if (request == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status == ExportRequestStatus.UnderOfficerReview)
        {
            await SynchronizeStartReviewStep4Async(exportRequestId);
            return new ExportOfficerOperationResult
            {
                Success = true,
                Message = "Export request is already under officer review.",
                Request = await MapToOfficerExportRequestResponseDtoAsync(request)
            };
        }

        if (request.Status != ExportRequestStatus.Submitted &&
            request.Status != ExportRequestStatus.UnderComplianceReview)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request cannot enter officer review from its current status.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        request.Status = ExportRequestStatus.UnderOfficerReview;
        request.UpdatedAt = DateTime.UtcNow;

        await SynchronizeStartReviewStep4Async(exportRequestId);

        await _context.SaveChangesAsync();

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = "Export request review started successfully.",
            Request = await MapToOfficerExportRequestResponseDtoAsync(request)
        };
    }

    // ==========================================
    // 4. MAKE DECISION
    // ==========================================
    public async Task<ExportOfficerOperationResult> MakeDecisionAsync(
        Guid officerUserId,
        Guid exportRequestId,
        ExportDecisionDto dto)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Decision))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Decision must be Approve, Reject, or RequestRevision.",
                ErrorCode = "INVALID_DECISION"
            };
        }

        var decision = dto.Decision.Trim();
        ExportRequestStatus newStatus;
        string successMessage;

        if (decision.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = ExportRequestStatus.Approved;
            successMessage = "Export request approved successfully.";
        }
        else if (decision.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = ExportRequestStatus.Rejected;
            successMessage = "Export request rejected successfully.";
        }
        else if (decision.Equals("RequestRevision", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = ExportRequestStatus.RevisionRequired;
            successMessage = "Revision requested successfully.";
        }
        else
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Decision must be Approve, Reject, or RequestRevision.",
                ErrorCode = "INVALID_DECISION"
            };
        }

        var normalizedReviewNotes = string.IsNullOrWhiteSpace(dto.ReviewNotes)
            ? null
            : dto.ReviewNotes.Trim();

        if (newStatus == ExportRequestStatus.Rejected && string.IsNullOrWhiteSpace(normalizedReviewNotes))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Review notes are required when rejecting an export request.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (newStatus == ExportRequestStatus.RevisionRequired && string.IsNullOrWhiteSpace(normalizedReviewNotes))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Review notes are required when requesting a revision.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId);

        if (request == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status != ExportRequestStatus.UnderOfficerReview)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Final decisions can only be made for requests under officer review.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        var now = DateTime.UtcNow;
        request.Status = newStatus;
        request.ReviewedByUserId = officerUserId;
        request.ReviewedAt = now;
        request.UpdatedAt = now;
        request.ReviewNotes = normalizedReviewNotes;

        await SynchronizeDecisionWorkflowAsync(exportRequestId, newStatus, !string.IsNullOrWhiteSpace(normalizedReviewNotes), now);

        await _context.SaveChangesAsync();

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = successMessage,
            Request = await MapToOfficerExportRequestResponseDtoAsync(request)
        };
    }

    // ==========================================
    // 5. GET DOCUMENT FILE FOR REVIEW
    // ==========================================
    public async Task<ComplianceDocumentFileResult> GetDocumentFileForReviewAsync(
        Guid officerUserId,
        Guid exportRequestId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = officerValidation.ErrorCode,
                Message = officerValidation.Message
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        if (documentId == Guid.Empty)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Compliance document ID is invalid."
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == exportRequestId, cancellationToken);

        if (request == null)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        if (request.Status == ExportRequestStatus.Draft ||
            request.Status == ExportRequestStatus.Cancelled)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "INVALID_STATUS",
                Message = "This export request is not available for officer review."
            };
        }

        var document = await _context.ComplianceDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ExportRequestId == exportRequestId, cancellationToken);

        if (document == null)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "DOCUMENT_NOT_FOUND",
                Message = "Compliance document was not found."
            };
        }

        if (string.IsNullOrWhiteSpace(document.FileUrl))
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "FILE_NOT_UPLOADED",
                Message = "No file has been uploaded for this compliance document."
            };
        }

        var stream = await _fileStorageService.OpenReadAsync(
            document.FileUrl,
            cancellationToken
        );

        if (stream == null)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "FILE_NOT_FOUND",
                Message = "The stored compliance document file could not be found."
            };
        }

        var extension = Path.GetExtension(document.FileUrl).ToLowerInvariant();

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };

        if (contentType == null)
        {
            await stream.DisposeAsync();
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "UNSUPPORTED_FILE_TYPE",
                Message = "The stored compliance document file type is not supported."
            };
        }

        var downloadFileName = $"compliance-document-{document.Id}{extension}";

        return new ComplianceDocumentFileResult
        {
            Success = true,
            Message = "Compliance document file retrieved successfully.",
            Content = stream,
            ContentType = contentType,
            DownloadFileName = downloadFileName
        };
    }

    // ==========================================
    // PRIVATE OFFICER VALIDATION HELPER
    // ==========================================
    private async Task<ExportOfficerOperationResult?> ValidateOfficerAsync(Guid officerUserId)
    {
        if (officerUserId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        var officer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == officerUserId);

        if (officer == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export officer account was not found.",
                ErrorCode = "OFFICER_NOT_FOUND"
            };
        }

        if (!officer.Role.Equals(UserRoles.ExportOfficer, StringComparison.OrdinalIgnoreCase))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "You are not authorized to perform export officer operations.",
                ErrorCode = "FORBIDDEN"
            };
        }

        return null;
    }

    // ==========================================
    // PRIVATE MAPPING HELPER
    // ==========================================
    private static OfficerExportRequestResponseDto MapToOfficerExportRequestResponseDto(ExportRequest entity)
    {
        var mappedDocuments = entity.ComplianceDocuments?
            .Select(d =>
            {
                var (effStatus, effReason) = Gemora.Domain.Helpers.ComplianceDocumentStatusHelper.CalculateEffectiveStatus(d);
                return new ComplianceDocumentResponseDto
                {
                    Id = d.Id,
                    ExportRequestId = d.ExportRequestId,
                    DocumentType = d.DocumentType,
                    DocumentNumber = d.DocumentNumber,
                    Issuer = d.Issuer,
                    IssueDate = d.IssueDate,
                    ExpiryDate = d.ExpiryDate,
                    FileUrl = d.FileUrl,
                    Status = d.Status.ToString(),
                    EffectiveStatus = effStatus,
                    EffectiveStatusReason = effReason,
                    UploadedAt = d.UploadedAt
                };
            })
            .ToList() ?? new List<ComplianceDocumentResponseDto>();

        return new OfficerExportRequestResponseDto
        {
            Id = entity.Id,
            RequestedByUserId = entity.RequestedByUserId,
            RequesterName = entity.RequestedByUser?.FullName ?? string.Empty,
            RequesterEmail = entity.RequestedByUser?.Email ?? string.Empty,
            OriginCountry = entity.OriginCountry,
            DestinationCountry = entity.DestinationCountry,
            DeclaredValue = entity.DeclaredValue,
            Currency = entity.Currency,
            Purpose = entity.Purpose,
            Status = entity.Status.ToString(),
            ReviewNotes = entity.ReviewNotes,
            SubmittedAt = entity.SubmittedAt,
            ReviewedAt = entity.ReviewedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Documents = mappedDocuments
        };
    }

    private async Task<OfficerExportRequestResponseDto> MapToOfficerExportRequestResponseDtoAsync(ExportRequest entity)
    {
        var dto = MapToOfficerExportRequestResponseDto(entity);

        try
        {
            var evalResult = await _complianceRulesService.EvaluateAsync(entity.RequestedByUserId, entity.Id);
            dto.DeterministicCompliance = evalResult?.Check;
        }
        catch
        {
            dto.DeterministicCompliance = null;
        }

        dto.AgentWorkflow = await BuildWorkflowReviewDtoAsync(entity.Id);

        return dto;
    }

    private async Task<ComplianceWorkflowReviewDto?> BuildWorkflowReviewDtoAsync(Guid exportRequestId)
    {
        var workflow = await _context.AgentWorkflows
            .AsNoTracking()
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Where(w => w.WorkflowType == "ExportCompliance" &&
                        w.RootEntityType == "ExportRequest" &&
                        w.RootEntityId == exportRequestId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        if (workflow == null)
        {
            return null;
        }

        ComplianceAgentResultDto? assessment = null;
        var step2 = workflow.Steps?.FirstOrDefault(s => s.StepNumber == 2);
        if (step2 != null && !string.IsNullOrWhiteSpace(step2.OutputSummaryJson))
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                assessment = JsonSerializer.Deserialize<ComplianceAgentResultDto>(step2.OutputSummaryJson, options);
            }
            catch
            {
                assessment = null;
            }
        }

        var mappedSteps = new List<ComplianceWorkflowStepDto>();
        if (workflow.Steps != null)
        {
            foreach (var step in workflow.Steps.OrderBy(s => s.StepNumber))
            {
                var toolCalls = new List<ComplianceWorkflowToolCallDto>();
                if (step.ToolCalls != null)
                {
                    foreach (var tc in step.ToolCalls.OrderBy(t => t.CreatedAt).ThenBy(t => t.AttemptNumber))
                    {
                        toolCalls.Add(new ComplianceWorkflowToolCallDto
                        {
                            ToolName = tc.ToolName ?? string.Empty,
                            AttemptNumber = tc.AttemptNumber,
                            Succeeded = tc.Succeeded,
                            DurationMs = tc.DurationMs,
                            ErrorCode = tc.ErrorCode,
                            CreatedAt = tc.CreatedAt
                        });
                    }
                }

                string actorType;
                string actor;

                switch (step.StepNumber)
                {
                    case 1:
                        actorType = "Agent";
                        actor = "ComplianceRequirementsAgent";
                        break;
                    case 2:
                        actorType = "Agent";
                        actor = "ComplianceRequirementsAgent";
                        break;
                    case 3:
                        actorType = "System";
                        actor = "ComplianceOutputValidator";
                        break;
                    case 4:
                        actorType = "Human";
                        actor = "ExportOfficer";
                        break;
                    default:
                        actorType = "System";
                        actor = !string.IsNullOrWhiteSpace(step.AgentName) ? step.AgentName : "System";
                        break;
                }

                mappedSteps.Add(new ComplianceWorkflowStepDto
                {
                    StepNumber = step.StepNumber,
                    ActorType = actorType,
                    Actor = actor,
                    Action = step.Action ?? string.Empty,
                    Status = step.Status.ToString(),
                    StartedAt = step.StartedAt,
                    CompletedAt = step.CompletedAt,
                    ErrorCode = step.ErrorCode,
                    ToolCalls = toolCalls
                });
            }
        }

        return new ComplianceWorkflowReviewDto
        {
            WorkflowId = workflow.Id,
            WorkflowStatus = workflow.Status.ToString(),
            ApprovalStatus = workflow.ApprovalStatus.ToString(),
            CurrentStep = workflow.CurrentStep,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            CompletedAt = workflow.CompletedAt,
            FinalSummary = workflow.FinalSummary,
            Assessment = assessment,
            Steps = mappedSteps
        };
    }

    // ==========================================
    // WORKFLOW SYNCHRONIZATION HELPERS
    // ==========================================
    private async Task SynchronizeStartReviewStep4Async(Guid exportRequestId)
    {
        var activeWorkflow = await _context.AgentWorkflows
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w =>
                w.WorkflowType == "ExportCompliance" &&
                w.RootEntityType == "ExportRequest" &&
                w.RootEntityId == exportRequestId &&
                w.Status == AgentWorkflowStatus.WaitingForApproval &&
                w.ApprovalStatus == AgentApprovalStatus.Pending);

        if (activeWorkflow == null)
        {
            return;
        }

        var existingStep4 = activeWorkflow.Steps.FirstOrDefault(s => s.StepNumber == 4);
        if (existingStep4 == null)
        {
            var step4 = new AgentWorkflowStep
            {
                Id = Guid.NewGuid(),
                WorkflowId = activeWorkflow.Id,
                StepNumber = 4,
                AgentName = "ExportOfficer",
                Action = "HumanReviewAndDecision",
                Status = AgentWorkflowStepStatus.Running,
                InputSummaryJson = JsonSerializer.Serialize(new
                {
                    humanReviewStarted = true,
                    exportRequestId = exportRequestId.ToString()
                }),
                StartedAt = DateTime.UtcNow
            };

            activeWorkflow.CurrentStep = 4;
            activeWorkflow.UpdatedAt = DateTime.UtcNow;
            _context.AgentWorkflowSteps.Add(step4);
        }
    }

    private async Task SynchronizeDecisionWorkflowAsync(
        Guid exportRequestId,
        ExportRequestStatus newStatus,
        bool notesPresent,
        DateTime now)
    {
        var activeWorkflow = await _context.AgentWorkflows
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w =>
                w.WorkflowType == "ExportCompliance" &&
                w.RootEntityType == "ExportRequest" &&
                w.RootEntityId == exportRequestId &&
                w.Status == AgentWorkflowStatus.WaitingForApproval &&
                w.ApprovalStatus == AgentApprovalStatus.Pending);

        if (activeWorkflow == null)
        {
            return;
        }

        var step4 = activeWorkflow.Steps.FirstOrDefault(s => s.StepNumber == 4);
        if (step4 == null)
        {
            step4 = new AgentWorkflowStep
            {
                Id = Guid.NewGuid(),
                WorkflowId = activeWorkflow.Id,
                StepNumber = 4,
                AgentName = "ExportOfficer",
                Action = "HumanReviewAndDecision",
                Status = AgentWorkflowStepStatus.Running,
                InputSummaryJson = JsonSerializer.Serialize(new
                {
                    humanReviewStarted = true,
                    exportRequestId = exportRequestId.ToString()
                }),
                StartedAt = now
            };

            _context.AgentWorkflowSteps.Add(step4);
        }

        step4.Status = AgentWorkflowStepStatus.Succeeded;
        step4.CompletedAt = now;

        string decisionStr;
        AgentApprovalStatus approvalStatus;
        string finalSummary;

        if (newStatus == ExportRequestStatus.Approved)
        {
            decisionStr = "Approved";
            approvalStatus = AgentApprovalStatus.Approved;
            finalSummary = "Export compliance workflow completed with human approval.";
        }
        else if (newStatus == ExportRequestStatus.Rejected)
        {
            decisionStr = "Rejected";
            approvalStatus = AgentApprovalStatus.Rejected;
            finalSummary = "Export compliance workflow completed with human rejection.";
        }
        else // RevisionRequired
        {
            decisionStr = "RevisionRequested";
            approvalStatus = AgentApprovalStatus.RevisionRequested;
            finalSummary = "Export compliance workflow completed with a human revision request.";
        }

        step4.OutputSummaryJson = JsonSerializer.Serialize(new
        {
            decision = decisionStr,
            notesPresent = notesPresent
        });

        step4.ValidationResultJson = JsonSerializer.Serialize(new
        {
            authorizedHumanDecision = true,
            finalDecisionRecorded = true,
            businessStateUpdated = true
        });

        activeWorkflow.Status = AgentWorkflowStatus.Completed;
        activeWorkflow.ApprovalStatus = approvalStatus;
        activeWorkflow.FinalSummary = finalSummary;
        activeWorkflow.CurrentStep = 4;
        activeWorkflow.CompletedAt = now;
        activeWorkflow.UpdatedAt = now;
    }
}

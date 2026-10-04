using Gemora.API.Services;
using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Domain.Helpers;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace Gemora.Tests;

public class ComplianceWorkflowAndValidationTests
{
    private class FakeComplianceAiClient : IComplianceAiClient
    {
        public bool ShouldFail { get; set; }
        public string ErrorCode { get; set; } = "AI_PROVIDER_ERROR";
        public string ErrorMessage { get; set; } = "Service Unavailable";
        public bool IsTransient { get; set; } = true;

        public Task<ComplianceAiClientResult> AnalyzeAsync(
            ComplianceAgentContextDto context,
            CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                return Task.FromResult(new ComplianceAiClientResult
                {
                    Success = false,
                    ErrorCode = ErrorCode,
                    Message = ErrorMessage,
                    IsTransientFailure = IsTransient
                });
            }

            var resultDto = new ComplianceAgentResultDto
            {
                Summary = "Fake AI advisory analysis complete.",
                DeterministicComplete = context.DeterministicCheck.IsComplete,
                MissingRequirements = context.DeterministicCheck.MissingRequirements ?? new List<string>(),
                DocumentFindings = new List<ComplianceAgentDocumentFindingDto>(),
                Inconsistencies = new List<string>(),
                Warnings = context.DeterministicCheck.Warnings ?? new List<string>(),
                RecommendedOfficerChecks = new List<string> { "Verify document authenticity" },
                RequiresOfficerAttention = !context.DeterministicCheck.IsComplete,
                Confidence = 0.92,
                Disclaimer = "AI-generated advisory assessment. Final export decisions must be made by an authorized Export Officer."
            };

            return Task.FromResult(new ComplianceAiClientResult
            {
                Success = true,
                Message = "Analysis succeeded.",
                Result = resultDto,
                ModelName = "gemini-3.8-flash"
            });
        }
    }

    private class FakeFileStorageService : IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
        {
            var key = $"{Guid.NewGuid():N}{extension}";
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _files[key] = ms.ToArray();
            return Task.FromResult(key);
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            if (_files.TryGetValue(storageKey, out var bytes))
            {
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            }
            return Task.FromResult<Stream?>(null);
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            _files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static (User requester, User officer) SeedUsers(ApplicationDbContext context)
    {
        var requester = new User
        {
            Id = Guid.NewGuid(),
            Email = "buyer@gemora.com",
            FullName = "Test Buyer",
            Role = UserRoles.Buyer
        };

        var officer = new User
        {
            Id = Guid.NewGuid(),
            Email = "officer@gemora.com",
            FullName = "Test Officer",
            Role = UserRoles.ExportOfficer
        };

        context.Users.AddRange(requester, officer);
        context.SaveChanges();

        return (requester, officer);
    }

    private static ExportRequest SeedExportRequest(
        ApplicationDbContext context,
        User requester,
        ExportRequestStatus status = ExportRequestStatus.Submitted)
    {
        var request = new ExportRequest
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = requester.Id,
            OriginCountry = "Sri Lanka",
            DestinationCountry = "United States",
            DeclaredValue = 5000,
            Currency = "USD",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.ExportRequests.Add(request);
        context.SaveChanges();

        return request;
    }

    // 1. Expired certificate -> deterministic result incomplete
    [Fact]
    public async Task Test1_ExpiredCertificate_DeterministicResultIncomplete()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var doc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            ExportRequestId = request.Id,
            UploadedByUserId = requester.Id,
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GEM-100",
            Issuer = "NGJA",
            IssueDate = DateTime.UtcNow.AddYears(-2),
            ExpiryDate = DateTime.UtcNow.AddDays(-10), // Expired
            Status = ComplianceDocumentStatus.Pending
        };
        context.ComplianceDocuments.Add(doc);
        await context.SaveChangesAsync();

        var rulesService = new ComplianceRulesService(context);
        var evalResult = await rulesService.EvaluateAsync(requester.Id, request.Id);

        Assert.True(evalResult.Success);
        Assert.False(evalResult.Check!.IsComplete);
        Assert.Contains(evalResult.Check.InvalidDocuments, d => d.Contains("expired"));
    }

    // 2. Expiry date earlier than issue date -> invalid
    [Fact]
    public async Task Test2_ExpiryBeforeIssueDate_Invalid()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var doc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            ExportRequestId = request.Id,
            UploadedByUserId = requester.Id,
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GEM-101",
            Issuer = "NGJA",
            IssueDate = DateTime.UtcNow.AddDays(10),
            ExpiryDate = DateTime.UtcNow.AddDays(5), // Earlier than issue date
            Status = ComplianceDocumentStatus.Pending
        };
        context.ComplianceDocuments.Add(doc);
        await context.SaveChangesAsync();

        var rulesService = new ComplianceRulesService(context);
        var evalResult = await rulesService.EvaluateAsync(requester.Id, request.Id);

        Assert.True(evalResult.Success);
        Assert.False(evalResult.Check!.IsComplete);
        Assert.Contains(evalResult.Check.InvalidDocuments, d => d.Contains("earlier than issue date"));
    }

    // 3. Required certificate issuer missing -> validation finding
    [Fact]
    public async Task Test3_RequiredIssuerMissing_ValidationFinding()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var doc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            ExportRequestId = request.Id,
            UploadedByUserId = requester.Id,
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GEM-102",
            Issuer = null, // Missing issuer
            IssueDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ComplianceDocumentStatus.Pending
        };
        context.ComplianceDocuments.Add(doc);
        await context.SaveChangesAsync();

        var rulesService = new ComplianceRulesService(context);
        var evalResult = await rulesService.EvaluateAsync(requester.Id, request.Id);

        Assert.True(evalResult.Success);
        Assert.False(evalResult.Check!.IsComplete);
        Assert.Contains(evalResult.Check.InvalidDocuments, d => d.Contains("issuer is required"));
    }

    // 4. Required certificate document number missing -> validation finding
    [Fact]
    public async Task Test4_RequiredDocumentNumberMissing_ValidationFinding()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var doc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            ExportRequestId = request.Id,
            UploadedByUserId = requester.Id,
            DocumentType = "GemologyCertificate",
            DocumentNumber = "   ", // Missing document number
            Issuer = "NGJA",
            IssueDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ComplianceDocumentStatus.Pending
        };
        context.ComplianceDocuments.Add(doc);
        await context.SaveChangesAsync();

        var rulesService = new ComplianceRulesService(context);
        var evalResult = await rulesService.EvaluateAsync(requester.Id, request.Id);

        Assert.True(evalResult.Success);
        Assert.False(evalResult.Check!.IsComplete);
        Assert.Contains(evalResult.Check.InvalidDocuments, d => d.Contains("document number is required"));
    }

    // 5. Effective status returns Expired for expired document even if stored status is Pending
    [Fact]
    public void Test5_EffectiveStatusReturnsExpiredForExpiredDoc_EvenIfStoredStatusIsPending()
    {
        var doc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GEM-103",
            Issuer = "NGJA",
            IssueDate = DateTime.UtcNow.AddYears(-2),
            ExpiryDate = DateTime.UtcNow.AddDays(-5), // Expired
            Status = ComplianceDocumentStatus.Pending // Stored in DB as Pending
        };

        var (effStatus, reason) = ComplianceDocumentStatusHelper.CalculateEffectiveStatus(doc);

        Assert.Equal("Expired", effStatus);
        Assert.NotNull(reason);
        Assert.Contains("expired", reason.ToLower());
        Assert.Equal(ComplianceDocumentStatus.Pending, doc.Status); // DB entity status preserved
    }

    // 6. Non-ExportOfficer cannot use officer decision endpoint
    [Fact]
    public async Task Test6_NonExportOfficerCannotUseOfficerDecisionEndpoint()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester, ExportRequestStatus.UnderOfficerReview);

        var fileStorage = new FakeFileStorageService();
        var rulesService = new ComplianceRulesService(context);
        var officerService = new ExportOfficerService(context, fileStorage, rulesService);

        var decisionDto = new ExportDecisionDto { Decision = "Approve", ReviewNotes = "Approved" };
        var result = await officerService.MakeDecisionAsync(requester.Id, request.Id, decisionDto);

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    // 7. Non-ExportOfficer cannot access officer private document endpoint
    [Fact]
    public async Task Test7_NonExportOfficerCannotAccessOfficerPrivateDocumentEndpoint()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var fileStorage = new FakeFileStorageService();
        var rulesService = new ComplianceRulesService(context);
        var officerService = new ExportOfficerService(context, fileStorage, rulesService);

        var result = await officerService.GetDocumentFileForReviewAsync(requester.Id, request.Id, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    // 8. ExportOfficer can access an authorized compliance document
    [Fact]
    public async Task Test8_ExportOfficerCanAccessAuthorizedComplianceDocument()
    {
        using var context = CreateDbContext();
        var (requester, officer) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var fileStorage = new FakeFileStorageService();
        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }); // PDF header
        var storageKey = await fileStorage.SaveAsync(ms, ".pdf");

        var doc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            ExportRequestId = request.Id,
            UploadedByUserId = requester.Id,
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GEM-104",
            Issuer = "NGJA",
            FileUrl = storageKey,
            Status = ComplianceDocumentStatus.Pending
        };
        context.ComplianceDocuments.Add(doc);
        await context.SaveChangesAsync();

        var rulesService = new ComplianceRulesService(context);
        var officerService = new ExportOfficerService(context, fileStorage, rulesService);

        var result = await officerService.GetDocumentFileForReviewAsync(officer.Id, request.Id, doc.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Content);
        Assert.Equal("application/pdf", result.ContentType);

        await result.Content.DisposeAsync();
    }

    // 9. Failed AI provider workflow can create a NEW retry workflow
    [Fact]
    public async Task Test9_FailedAiProviderWorkflow_CanCreateNewRetryWorkflow()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient { ShouldFail = true, ErrorCode = "AI_PROVIDER_ERROR" };
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        // Run initial analysis -> Fails due to AI_PROVIDER_ERROR
        var initialResult = await workflowService.RunComplianceAnalysisAsync(requester.Id, request.Id);
        Assert.False(initialResult.Success);
        Assert.Equal("AI_PROVIDER_ERROR", initialResult.ErrorCode);
        var oldWorkflowId = initialResult.WorkflowId;

        // Fix AI provider state
        fakeAi.ShouldFail = false;

        // Execute Retry
        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.True(retryResult.Success);
        Assert.NotEqual(Guid.Empty, retryResult.WorkflowId);
        Assert.NotEqual(oldWorkflowId, retryResult.WorkflowId); // Brand NEW workflow created!
    }

    // 10. Retry preserves the original failed workflow
    [Fact]
    public async Task Test10_RetryPreservesOriginalFailedWorkflow()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient { ShouldFail = true, ErrorCode = "AI_TIMEOUT" };
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var initialResult = await workflowService.RunComplianceAnalysisAsync(requester.Id, request.Id);
        var failedWorkflowId = initialResult.WorkflowId;

        fakeAi.ShouldFail = false;
        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        var oldWorkflow = await context.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == failedWorkflowId);
        Assert.NotNull(oldWorkflow);
        Assert.Equal(AgentWorkflowStatus.Failed, oldWorkflow.Status);
        Assert.Equal("AI_TIMEOUT", oldWorkflow.ErrorCode);

        var totalWorkflows = await context.AgentWorkflows.CountAsync(w => w.RootEntityId == request.Id);
        Assert.Equal(2, totalWorkflows); // Both old failed and new successful workflows exist!
    }

    // 11. Retry is rejected when an active workflow exists
    [Fact]
    public async Task Test11_RetryRejected_WhenActiveWorkflowExists()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var activeWorkflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "ExportCompliance",
            RootEntityType = "ExportRequest",
            RootEntityId = request.Id,
            TriggeredByUserId = requester.Id,
            Status = AgentWorkflowStatus.Running,
            CurrentStep = 2
        };
        context.AgentWorkflows.Add(activeWorkflow);
        await context.SaveChangesAsync();

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient();
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.False(retryResult.Success);
        Assert.Equal("WORKFLOW_ALREADY_ACTIVE", retryResult.ErrorCode);
    }

    // 12. Retry is rejected after Approved / Rejected / Cancelled status
    [Fact]
    public async Task Test12_RetryRejected_AfterApprovedRejectedCancelledStatus()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester, ExportRequestStatus.Approved);

        var failedWorkflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "ExportCompliance",
            RootEntityType = "ExportRequest",
            RootEntityId = request.Id,
            TriggeredByUserId = requester.Id,
            Status = AgentWorkflowStatus.Failed,
            ErrorCode = "AI_PROVIDER_ERROR"
        };
        context.AgentWorkflows.Add(failedWorkflow);
        await context.SaveChangesAsync();

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient();
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.False(retryResult.Success);
        Assert.Equal("RETRY_NOT_ELIGIBLE", retryResult.ErrorCode);
    }

    // 13. Retry is rejected for non-transient / non-provider failure
    [Fact]
    public async Task Test13_RetryRejected_ForNonTransientNonProviderFailure()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var nonRetryableWorkflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "ExportCompliance",
            RootEntityType = "ExportRequest",
            RootEntityId = request.Id,
            TriggeredByUserId = requester.Id,
            Status = AgentWorkflowStatus.Failed,
            ErrorCode = "AI_WORKFLOW_VALIDATION_FAILED" // Deterministic programming error, not provider failure
        };
        context.AgentWorkflows.Add(nonRetryableWorkflow);
        await context.SaveChangesAsync();

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient();
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.False(retryResult.Success);
        Assert.Equal("RETRY_NOT_ELIGIBLE", retryResult.ErrorCode);
    }

    // 14. Successful retry can proceed to output validation and human approval
    [Fact]
    public async Task Test14_SuccessfulRetry_CanProceedToOutputValidationAndHumanApproval()
    {
        using var context = CreateDbContext();
        var (requester, officer) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var failedWorkflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "ExportCompliance",
            RootEntityType = "ExportRequest",
            RootEntityId = request.Id,
            TriggeredByUserId = requester.Id,
            Status = AgentWorkflowStatus.Failed,
            ErrorCode = "AI_PROVIDER_ERROR",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.AgentWorkflows.Add(failedWorkflow);
        await context.SaveChangesAsync();

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient { ShouldFail = false };
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.True(retryResult.Success);
        Assert.NotNull(retryResult.Assessment);

        // Verify workflow reached WaitingForApproval
        var newWorkflow = await context.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == retryResult.WorkflowId);
        Assert.NotNull(newWorkflow);
        Assert.Equal(AgentWorkflowStatus.WaitingForApproval, newWorkflow.Status);
        Assert.Equal(AgentApprovalStatus.Pending, newWorkflow.ApprovalStatus);

        // Export Officer can now make a decision
        var fileStorage = new FakeFileStorageService();
        var officerService = new ExportOfficerService(context, fileStorage, new ComplianceRulesService(context));
        await officerService.StartReviewAsync(officer.Id, request.Id);

        var decisionResult = await officerService.MakeDecisionAsync(
            officer.Id,
            request.Id,
            new ExportDecisionDto { Decision = "Approve", ReviewNotes = "Human approval granted." });

        Assert.True(decisionResult.Success);
        Assert.Equal("Approved", decisionResult.Request!.Status);
    }

    // 15. AI can never directly Approve or Reject the export
    [Fact]
    public async Task Test15_AiCanNeverDirectlyApproveOrRejectExport()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient { ShouldFail = false };
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var result = await workflowService.RunComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.True(result.Success);

        // Verify export request remains UnderComplianceReview (NOT Approved or Rejected)
        var refreshedReq = await context.ExportRequests.FirstOrDefaultAsync(r => r.Id == request.Id);
        Assert.NotNull(refreshedReq);
        Assert.Equal(ExportRequestStatus.UnderComplianceReview, refreshedReq.Status);

        // Verify workflow status is WaitingForApproval (NOT Approved)
        var workflow = await context.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == result.WorkflowId);
        Assert.NotNull(workflow);
        Assert.Equal(AgentWorkflowStatus.WaitingForApproval, workflow.Status);
        Assert.Equal(AgentApprovalStatus.Pending, workflow.ApprovalStatus);
    }

    // 16. Human approval remains mandatory
    [Fact]
    public async Task Test16_HumanApprovalRemainsMandatory()
    {
        using var context = CreateDbContext();
        var (requester, officer) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient { ShouldFail = false };
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        await workflowService.RunComplianceAnalysisAsync(requester.Id, request.Id);

        // Human decision is required to transition to Approved
        var fileStorage = new FakeFileStorageService();
        var officerService = new ExportOfficerService(context, fileStorage, new ComplianceRulesService(context));

        await officerService.StartReviewAsync(officer.Id, request.Id);
        var decisionResult = await officerService.MakeDecisionAsync(
            officer.Id,
            request.Id,
            new ExportDecisionDto { Decision = "Approve", ReviewNotes = "Verified by officer" });

        Assert.True(decisionResult.Success);
        Assert.Equal("Approved", decisionResult.Request!.Status);
    }

    // 17. Retry is rejected for malformed/invalid AI output failures, auth errors, bad requests, or generic analysis failures
    [Theory]
    [InlineData("AI_BAD_REQUEST")]
    [InlineData("AI_AUTH_ERROR")]
    [InlineData("AI_INVALID_RESPONSE")]
    [InlineData("AI_VALIDATION_FAILED")]
    [InlineData("AI_ANALYSIS_FAILED")]
    [InlineData("AI_WORKFLOW_VALIDATION_FAILED")]
    public async Task Test17_RetryRejected_ForMalformedOrInvalidAiOutput(string nonRetryableErrorCode)
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var failedWorkflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "ExportCompliance",
            RootEntityType = "ExportRequest",
            RootEntityId = request.Id,
            TriggeredByUserId = requester.Id,
            Status = AgentWorkflowStatus.Failed,
            ErrorCode = nonRetryableErrorCode,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        context.AgentWorkflows.Add(failedWorkflow);
        await context.SaveChangesAsync();

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient();
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.False(retryResult.Success);
        Assert.Equal("RETRY_NOT_ELIGIBLE", retryResult.ErrorCode);
    }

    // 18. Retry is allowed for transient provider failures
    [Theory]
    [InlineData("AI_PROVIDER_UNAVAILABLE")]
    [InlineData("AI_RATE_LIMITED")]
    [InlineData("AI_TIMEOUT")]
    [InlineData("AI_PROVIDER_ERROR")]
    public async Task Test18_RetryAllowed_ForTransientProviderFailures(string transientErrorCode)
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester);

        var failedWorkflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "ExportCompliance",
            RootEntityType = "ExportRequest",
            RootEntityId = request.Id,
            TriggeredByUserId = requester.Id,
            Status = AgentWorkflowStatus.Failed,
            ErrorCode = transientErrorCode,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        context.AgentWorkflows.Add(failedWorkflow);
        await context.SaveChangesAsync();

        var toolService = new ComplianceAgentToolService(context, new ComplianceRulesService(context));
        var fakeAi = new FakeComplianceAiClient { ShouldFail = false };
        var workflowService = new ComplianceWorkflowService(context, toolService, fakeAi);

        var retryResult = await workflowService.RetryComplianceAnalysisAsync(requester.Id, request.Id);

        Assert.True(retryResult.Success);
        Assert.NotEqual(Guid.Empty, retryResult.WorkflowId);
    }

    // 19. Create export request with Sri Lanka origin -> accepted
    [Fact]
    public async Task Test19_CreateExportRequest_SriLankaOrigin_Accepted()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var fileStorage = new FakeFileStorageService();
        var service = new ExportComplianceService(context, fileStorage);

        var dto = new CreateExportRequestDto
        {
            OriginCountry = "Sri Lanka",
            DestinationCountry = "United States",
            DeclaredValue = 10000,
            Currency = "USD",
            Purpose = "Gem trade"
        };

        var result = await service.CreateExportRequestAsync(requester.Id, dto);

        Assert.True(result.Success);
        Assert.Equal("Sri Lanka", result.Request!.OriginCountry);
        Assert.Equal("United States", result.Request.DestinationCountry);
    }

    // 20. Create export request with another origin -> rejected
    [Theory]
    [InlineData("Australia")]
    [InlineData("Afghanistan")]
    [InlineData("United States")]
    public async Task Test20_CreateExportRequest_NonSriLankaOrigin_Rejected(string nonSriLankaOrigin)
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var fileStorage = new FakeFileStorageService();
        var service = new ExportComplianceService(context, fileStorage);

        var dto = new CreateExportRequestDto
        {
            OriginCountry = nonSriLankaOrigin,
            DestinationCountry = "United States",
            DeclaredValue = 5000,
            Currency = "USD"
        };

        var result = await service.CreateExportRequestAsync(requester.Id, dto);

        Assert.False(result.Success);
        Assert.Equal("INVALID_ORIGIN_COUNTRY", result.ErrorCode);
        Assert.Equal("Export requests must originate from Sri Lanka.", result.Message);
    }

    // 21. Update export request with another origin -> rejected
    [Fact]
    public async Task Test21_UpdateExportRequest_NonSriLankaOrigin_Rejected()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester, ExportRequestStatus.Draft);
        var fileStorage = new FakeFileStorageService();
        var service = new ExportComplianceService(context, fileStorage);

        var dto = new UpdateExportRequestDto
        {
            OriginCountry = "Australia",
            DestinationCountry = "Japan",
            DeclaredValue = 7500,
            Currency = "USD"
        };

        var result = await service.UpdateExportRequestAsync(requester.Id, request.Id, dto);

        Assert.False(result.Success);
        Assert.Equal("INVALID_ORIGIN_COUNTRY", result.ErrorCode);
        Assert.Equal("Export requests must originate from Sri Lanka.", result.Message);
    }

    // 22. Destination valid ISO country -> accepted
    [Fact]
    public async Task Test22_CreateExportRequest_ValidDestinationIsoCountry_Accepted()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var fileStorage = new FakeFileStorageService();
        var service = new ExportComplianceService(context, fileStorage);

        var dto = new CreateExportRequestDto
        {
            OriginCountry = "Sri Lanka",
            DestinationCountry = "Japan",
            DeclaredValue = 12000,
            Currency = "USD"
        };

        var result = await service.CreateExportRequestAsync(requester.Id, dto);

        Assert.True(result.Success);
        Assert.Equal("Japan", result.Request!.DestinationCountry);
    }

    // 23. Unsupported new document type "Certificate" -> rejected
    [Fact]
    public async Task Test23_AddComplianceDocument_UnsupportedDocumentType_Rejected()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester, ExportRequestStatus.Draft);
        var fileStorage = new FakeFileStorageService();
        var service = new ExportComplianceService(context, fileStorage);

        var dto = new CreateComplianceDocumentDto
        {
            DocumentType = "Certificate",
            DocumentNumber = "CERT-12345",
            Issuer = "Generic Lab"
        };

        var result = await service.AddComplianceDocumentAsync(requester.Id, request.Id, dto);

        Assert.False(result.Success);
        Assert.Equal("UNSUPPORTED_DOCUMENT_TYPE", result.ErrorCode);
    }

    // 24. Supported document type -> accepted
    [Fact]
    public async Task Test24_AddComplianceDocument_SupportedDocumentType_Accepted()
    {
        using var context = CreateDbContext();
        var (requester, _) = SeedUsers(context);
        var request = SeedExportRequest(context, requester, ExportRequestStatus.Draft);
        var fileStorage = new FakeFileStorageService();
        var service = new ExportComplianceService(context, fileStorage);

        var dto = new CreateComplianceDocumentDto
        {
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GIA-998877",
            Issuer = "GIA Gemological Institute",
            IssueDate = DateTime.UtcNow.AddMonths(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(2)
        };

        var result = await service.AddComplianceDocumentAsync(requester.Id, request.Id, dto);

        Assert.True(result.Success);
        Assert.Equal("GemologyCertificate", result.Document!.DocumentType);
    }

    // 25. Legacy Certificate document returns clear user-facing reason
    [Fact]
    public void Test25_CalculateEffectiveStatus_LegacyCertificateDocument_ReturnsClearUserReason()
    {
        var legacyDoc = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            DocumentType = "Certificate",
            DocumentNumber = "OLD-123",
            Issuer = "Legacy Issuer",
            Status = ComplianceDocumentStatus.Pending
        };

        var (status, reason) = ComplianceDocumentStatusHelper.CalculateEffectiveStatus(legacyDoc);

        Assert.Equal("Invalid", status);
        Assert.Equal(
            "This document uses an older unsupported document type. Add a new document using one of the supported compliance categories.",
            reason);
    }
}

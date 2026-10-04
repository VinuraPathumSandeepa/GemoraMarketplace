using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ExportComplianceService : IExportComplianceService
{
    private const long MaxComplianceFileSize = 10 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public ExportComplianceService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    // ==========================================
    // CREATE EXPORT REQUEST
    // ==========================================
    public async Task<ExportRequestOperationResult> CreateExportRequestAsync(
        Guid userId,
        CreateExportRequestDto dto)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user was not found.",
                ErrorCode = "USER_NOT_FOUND"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.OriginCountry) ||
            !string.Equals(dto.OriginCountry.Trim(), "Sri Lanka", StringComparison.OrdinalIgnoreCase))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export requests must originate from Sri Lanka.",
                ErrorCode = "INVALID_ORIGIN_COUNTRY"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.DestinationCountry) ||
            !ComplianceConstants.SupportedCountries.Any(c => string.Equals(c, dto.DestinationCountry.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Destination country is required and must be a valid supported country.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (dto.DeclaredValue <= 0)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Declared value must be greater than 0.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Currency is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var now = DateTime.UtcNow;
        var exportRequest = new ExportRequest
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = userId,
            OriginCountry = dto.OriginCountry.Trim(),
            DestinationCountry = dto.DestinationCountry.Trim(),
            DeclaredValue = dto.DeclaredValue,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
            Status = ExportRequestStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.ExportRequests.Add(exportRequest);
        await _context.SaveChangesAsync();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request created successfully.",
            Request = MapToResponseDto(exportRequest)
        };
    }

    // ==========================================
    // GET MY EXPORT REQUESTS
    // ==========================================
    public async Task<ExportRequestOperationResult> GetMyExportRequestsAsync(
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        var requests = await _context.ExportRequests
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var mapped = requests.Select(MapToResponseDto).ToList();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export requests retrieved successfully.",
            Requests = mapped
        };
    }

    // ==========================================
    // GET EXPORT REQUEST BY ID
    // ==========================================
    public async Task<ExportRequestOperationResult> GetExportRequestByIdAsync(
        Guid userId,
        Guid exportRequestId)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (request == null)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request retrieved successfully.",
            Details = MapToDetailsDto(request)
        };
    }

    // ==========================================
    // UPDATE EXPORT REQUEST
    // ==========================================
    public async Task<ExportRequestOperationResult> UpdateExportRequestAsync(
        Guid userId,
        Guid exportRequestId,
        UpdateExportRequestDto dto)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (request == null)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status != ExportRequestStatus.Draft &&
            request.Status != ExportRequestStatus.RevisionRequired)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Only draft or revision-required export requests can be edited.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.OriginCountry) ||
            !string.Equals(dto.OriginCountry.Trim(), "Sri Lanka", StringComparison.OrdinalIgnoreCase))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export requests must originate from Sri Lanka.",
                ErrorCode = "INVALID_ORIGIN_COUNTRY"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.DestinationCountry) ||
            !ComplianceConstants.SupportedCountries.Any(c => string.Equals(c, dto.DestinationCountry.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Destination country is required and must be a valid supported country.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (dto.DeclaredValue <= 0)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Declared value must be greater than 0.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Currency is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        request.OriginCountry = dto.OriginCountry.Trim();
        request.DestinationCountry = dto.DestinationCountry.Trim();
        request.DeclaredValue = dto.DeclaredValue;
        request.Currency = dto.Currency.Trim().ToUpperInvariant();
        request.Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request updated successfully.",
            Request = MapToResponseDto(request)
        };
    }

    // ==========================================
    // SUBMIT EXPORT REQUEST
    // ==========================================
    public async Task<ExportRequestOperationResult> SubmitExportRequestAsync(
        Guid userId,
        Guid exportRequestId)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (request == null)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status != ExportRequestStatus.Draft &&
            request.Status != ExportRequestStatus.RevisionRequired)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "This export request cannot be submitted in its current status.",
                ErrorCode = "SUBMISSION_NOT_ALLOWED"
            };
        }

        if (string.IsNullOrWhiteSpace(request.OriginCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Origin country is required for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(request.DestinationCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Destination country is required for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (request.DeclaredValue <= 0)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Declared value must be greater than 0 for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Currency is required for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var now = DateTime.UtcNow;
        request.Status = ExportRequestStatus.Submitted;
        request.SubmittedAt = now;
        request.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request submitted successfully.",
            Request = MapToResponseDto(request)
        };
    }

    // ==========================================
    // ADD COMPLIANCE DOCUMENT
    // ==========================================
    public async Task<ComplianceDocumentOperationResult> AddComplianceDocumentAsync(
        Guid userId,
        Guid exportRequestId,
        CreateComplianceDocumentDto dto)
    {
        if (userId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var exportRequest = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (exportRequest == null)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (exportRequest.Status != ExportRequestStatus.Draft &&
            exportRequest.Status != ExportRequestStatus.RevisionRequired)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Documents can only be added to draft or revision-required export requests.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.DocumentType))
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Document type is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var isSupportedType = ComplianceConstants.SupportedDocumentTypes.Any(t =>
            string.Equals(t, dto.DocumentType.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!isSupportedType)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Unsupported document type. Selected document category is not supported.",
                ErrorCode = "UNSUPPORTED_DOCUMENT_TYPE"
            };
        }

        if (dto.IssueDate.HasValue && dto.ExpiryDate.HasValue && dto.ExpiryDate.Value < dto.IssueDate.Value)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Expiry date cannot be earlier than issue date.",
                ErrorCode = "INVALID_DOCUMENT_DATE"
            };
        }

        var document = new ComplianceDocument
        {
            Id = Guid.NewGuid(),
            ExportRequestId = exportRequestId,
            UploadedByUserId = userId,
            DocumentType = dto.DocumentType.Trim(),
            DocumentNumber = string.IsNullOrWhiteSpace(dto.DocumentNumber) ? null : dto.DocumentNumber.Trim(),
            Issuer = string.IsNullOrWhiteSpace(dto.Issuer) ? null : dto.Issuer.Trim(),
            IssueDate = dto.IssueDate.HasValue
                ? DateTime.SpecifyKind(dto.IssueDate.Value, DateTimeKind.Utc)
                : null,
            ExpiryDate = dto.ExpiryDate.HasValue
                ? DateTime.SpecifyKind(dto.ExpiryDate.Value, DateTimeKind.Utc)
                : null,
            FileUrl = null,
            Status = ComplianceDocumentStatus.Pending,
            UploadedAt = DateTime.UtcNow
        };

        _context.ComplianceDocuments.Add(document);
        await _context.SaveChangesAsync();

        return new ComplianceDocumentOperationResult
        {
            Success = true,
            Message = "Compliance document added successfully.",
            Document = MapToComplianceDocumentResponseDto(document)
        };
    }

    // ==========================================
    // GET COMPLIANCE DOCUMENTS
    // ==========================================
    public async Task<ComplianceDocumentOperationResult> GetComplianceDocumentsAsync(
        Guid userId,
        Guid exportRequestId)
    {
        if (userId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var exportRequestExists = await _context.ExportRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (!exportRequestExists)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        var documents = await _context.ComplianceDocuments
            .AsNoTracking()
            .Where(d => d.ExportRequestId == exportRequestId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var mapped = documents.Select(MapToComplianceDocumentResponseDto).ToList();

        return new ComplianceDocumentOperationResult
        {
            Success = true,
            Message = "Compliance documents retrieved successfully.",
            Documents = mapped
        };
    }

    // ==========================================
    // UPLOAD DOCUMENT FILE
    // ==========================================
    public async Task<ComplianceDocumentOperationResult> UploadDocumentFileAsync(
        Guid userId,
        Guid exportRequestId,
        Guid documentId,
        Stream content,
        string extension,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_USER",
                Message = "Authenticated user is invalid."
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        if (documentId == Guid.Empty)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Compliance document ID is invalid."
            };
        }

        var exportRequest = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId, cancellationToken);

        if (exportRequest == null)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        if (exportRequest.Status != ExportRequestStatus.Draft &&
            exportRequest.Status != ExportRequestStatus.RevisionRequired)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_STATUS",
                Message = "Files can only be uploaded to draft or revision-required export requests."
            };
        }

        var document = await _context.ComplianceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ExportRequestId == exportRequestId, cancellationToken);

        if (document == null)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "DOCUMENT_NOT_FOUND",
                Message = "Compliance document was not found."
            };
        }

        if (!string.IsNullOrWhiteSpace(document.FileUrl))
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "FILE_ALREADY_EXISTS",
                Message = "A file has already been uploaded for this compliance document."
            };
        }

        if (content == null)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_FILE",
                Message = "A file is required."
            };
        }

        if (fileSize <= 0)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_FILE",
                Message = "The uploaded file is empty."
            };
        }

        if (fileSize > MaxComplianceFileSize)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "FILE_TOO_LARGE",
                Message = "The uploaded file cannot exceed 10 MB."
            };
        }

        if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrWhiteSpace(contentType))
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "UNSUPPORTED_FILE_TYPE",
                Message = "Only PDF, JPEG, and PNG compliance documents are allowed."
            };
        }

        var normalizedExtension = extension.Trim().ToLowerInvariant();
        if (!normalizedExtension.StartsWith("."))
        {
            normalizedExtension = "." + normalizedExtension;
        }

        var normalizedContentType = contentType.Trim().ToLowerInvariant();

        var isValidPair = (normalizedExtension, normalizedContentType) switch
        {
            (".pdf", "application/pdf") => true,
            (".jpg", "image/jpeg") => true,
            (".jpeg", "image/jpeg") => true,
            (".png", "image/png") => true,
            _ => false
        };

        if (!isValidPair)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "UNSUPPORTED_FILE_TYPE",
                Message = "Only PDF, JPEG, and PNG compliance documents are allowed."
            };
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        long totalBytesRead = 0;

        while (true)
        {
            var bytesRead = await content.ReadAsync(
                buffer.AsMemory(0, buffer.Length),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;

            if (totalBytesRead > MaxComplianceFileSize)
            {
                return new ComplianceDocumentOperationResult
                {
                    Success = false,
                    ErrorCode = "FILE_TOO_LARGE",
                    Message = "The uploaded file cannot exceed 10 MB."
                };
            }

            await memoryStream.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        if (totalBytesRead == 0)
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_FILE",
                Message = "The uploaded file is empty."
            };
        }

        memoryStream.Position = 0;

        if (!HasValidFileSignature(memoryStream, normalizedExtension))
        {
            return new ComplianceDocumentOperationResult
            {
                Success = false,
                ErrorCode = "INVALID_FILE_CONTENT",
                Message = "The uploaded file content does not match its declared file type."
            };
        }

        memoryStream.Position = 0;

        var storageKey = await _fileStorageService.SaveAsync(
            memoryStream,
            normalizedExtension,
            cancellationToken
        );

        document.FileUrl = storageKey;
        exportRequest.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await _fileStorageService.DeleteAsync(storageKey, cancellationToken);
            }
            catch
            {
                // Preserve original DB exception
            }

            throw;
        }

        return new ComplianceDocumentOperationResult
        {
            Success = true,
            Message = "Compliance document file uploaded successfully.",
            Document = MapToComplianceDocumentResponseDto(document)
        };
    }

    private static bool HasValidFileSignature(
        Stream stream,
        string extension)
    {
        if (stream == null || !stream.CanRead)
        {
            return false;
        }

        var originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            byte[] headerBuffer = new byte[8];
            int bytesRead = stream.Read(headerBuffer, 0, headerBuffer.Length);

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => bytesRead >= 5 &&
                          headerBuffer[0] == 0x25 &&
                          headerBuffer[1] == 0x50 &&
                          headerBuffer[2] == 0x44 &&
                          headerBuffer[3] == 0x46 &&
                          headerBuffer[4] == 0x2D,

                ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                    headerBuffer[0] == 0xFF &&
                                    headerBuffer[1] == 0xD8 &&
                                    headerBuffer[2] == 0xFF,

                ".png" => bytesRead >= 8 &&
                          headerBuffer[0] == 0x89 &&
                          headerBuffer[1] == 0x50 &&
                          headerBuffer[2] == 0x4E &&
                          headerBuffer[3] == 0x47 &&
                          headerBuffer[4] == 0x0D &&
                          headerBuffer[5] == 0x0A &&
                          headerBuffer[6] == 0x1A &&
                          headerBuffer[7] == 0x0A,

                _ => false
            };
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }

    // ==========================================
    // GET DOCUMENT FILE
    // ==========================================
    public async Task<ComplianceDocumentFileResult> GetDocumentFileAsync(
        Guid userId,
        Guid exportRequestId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "INVALID_USER",
                Message = "Authenticated user is invalid."
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

        var exportRequestExists = await _context.ExportRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId, cancellationToken);

        if (!exportRequestExists)
        {
            return new ComplianceDocumentFileResult
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
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
    // MAPPING HELPERS
    // ==========================================
    private static ExportRequestResponseDto MapToResponseDto(ExportRequest entity)
    {
        return new ExportRequestResponseDto
        {
            Id = entity.Id,
            OriginCountry = entity.OriginCountry,
            DestinationCountry = entity.DestinationCountry,
            DeclaredValue = entity.DeclaredValue,
            Currency = entity.Currency,
            Purpose = entity.Purpose,
            Status = entity.Status.ToString(),
            SubmittedAt = entity.SubmittedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static ExportRequestDetailsDto MapToDetailsDto(ExportRequest entity)
    {
        return new ExportRequestDetailsDto
        {
            Id = entity.Id,
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
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static ComplianceDocumentResponseDto MapToComplianceDocumentResponseDto(ComplianceDocument entity)
    {
        var (effectiveStatus, effectiveReason) = Gemora.Domain.Helpers.ComplianceDocumentStatusHelper.CalculateEffectiveStatus(entity);

        return new ComplianceDocumentResponseDto
        {
            Id = entity.Id,
            ExportRequestId = entity.ExportRequestId,
            DocumentType = entity.DocumentType,
            DocumentNumber = entity.DocumentNumber,
            Issuer = entity.Issuer,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            FileUrl = entity.FileUrl,
            Status = entity.Status.ToString(),
            EffectiveStatus = effectiveStatus,
            EffectiveStatusReason = effectiveReason,
            UploadedAt = entity.UploadedAt
        };
    }
}

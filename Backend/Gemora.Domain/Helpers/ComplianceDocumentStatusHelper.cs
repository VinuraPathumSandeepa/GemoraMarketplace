using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Helpers;

public static class ComplianceDocumentStatusHelper
{
    public static (string EffectiveStatus, string? Reason) CalculateEffectiveStatus(ComplianceDocument document)
    {
        if (document == null)
        {
            return ("Invalid", "Document is null.");
        }

        var todayUtc = DateTime.UtcNow.Date;

        // 1. Expiry date check
        if (document.ExpiryDate.HasValue && document.ExpiryDate.Value.Date < todayUtc)
        {
            return ("Expired", $"Certificate expired on {document.ExpiryDate.Value:dd/MM/yyyy}.");
        }

        // 2. Invalid date sequence check
        if (document.IssueDate.HasValue && document.ExpiryDate.HasValue && document.ExpiryDate.Value < document.IssueDate.Value)
        {
            return ("Invalid", "Certificate expiry date cannot be earlier than issue date.");
        }

        // 3. Document type allow-list check
        if (string.IsNullOrWhiteSpace(document.DocumentType) ||
            !ComplianceConstants.SupportedDocumentTypes.Any(t => string.Equals(t, document.DocumentType.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return ("Invalid", "Unsupported compliance document type.");
        }

        // 4. Required metadata check
        if (string.IsNullOrWhiteSpace(document.Issuer))
        {
            return ("Invalid", "Certificate issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(document.DocumentNumber))
        {
            return ("Invalid", "Certificate document number is required.");
        }

        // 5. Explicit DB status checks
        if (document.Status == ComplianceDocumentStatus.Invalid)
        {
            return ("Invalid", "Document is marked invalid.");
        }

        if (document.Status == ComplianceDocumentStatus.Expired)
        {
            return ("Expired", "Document is marked expired.");
        }

        return (document.Status.ToString(), null);
    }
}

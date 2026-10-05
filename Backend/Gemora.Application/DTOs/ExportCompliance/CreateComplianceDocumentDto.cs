using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.ExportCompliance;

public class CreateComplianceDocumentDto
{
    [Required(ErrorMessage = "Document type is required.")]
    [StringLength(100, ErrorMessage = "Document type cannot exceed 100 characters.")]
    public string DocumentType { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Document number cannot exceed 150 characters.")]
    public string? DocumentNumber { get; set; }

    [StringLength(150, ErrorMessage = "Issuer cannot exceed 150 characters.")]
    public string? Issuer { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.GemListings;

public class UpdateGemListingDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gem type is required.")]
    [StringLength(100)]
    public string GemType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000, ErrorMessage = "Carat weight must be greater than 0.")]
    public decimal CaratWeight { get; set; }

    [Required(ErrorMessage = "Color is required.")]
    [StringLength(100)]
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = "Clarity is required.")]
    [StringLength(100)]
    public string Clarity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cut is required.")]
    [StringLength(100)]
    public string Cut { get; set; } = string.Empty;

    [Range(0.01, 999999999999, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(10)]
    public string Currency { get; set; } = "LKR";


    // ============================================================
    // GEM IMAGE / CERTIFICATE EVIDENCE
    // ============================================================

    [StringLength(1000)]
    public string? PrimaryImageUrl { get; set; }

    [StringLength(200)]
    public string? CertificateNumber { get; set; }

    [StringLength(200)]
    public string? CertificateAuthority { get; set; }

    [StringLength(1000)]
    public string? CertificateUrl { get; set; }
}
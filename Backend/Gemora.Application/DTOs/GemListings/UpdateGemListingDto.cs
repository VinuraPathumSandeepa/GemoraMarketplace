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

    [Range(0.01, 100000)]
    public decimal CaratWeight { get; set; }

    [Required]
    [StringLength(100)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Clarity { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Cut { get; set; } = string.Empty;

    [Range(0.01, 999999999999)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "LKR";
}
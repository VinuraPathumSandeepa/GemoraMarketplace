using System.ComponentModel.DataAnnotations;

namespace Gemora.API.Models.Uploads;

public class GemImageUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
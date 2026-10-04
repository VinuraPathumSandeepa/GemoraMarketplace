using System.ComponentModel.DataAnnotations;

namespace Gemora.API.Models.Uploads;

public class CertificateUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
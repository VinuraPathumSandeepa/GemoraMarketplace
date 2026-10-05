using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.Orders;

public class OrderActionRequestDto
{
    [StringLength(500)]
    public string? Reason { get; set; }
}

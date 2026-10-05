using Gemora.Application.DTOs.Orders;

namespace Gemora.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateAsync(
        Guid buyerId,
        CreateOrderRequestDto dto);

    Task<OrderResponseDto?> GetForBuyerAsync(
        Guid id,
        Guid buyerId);

    Task<List<OrderResponseDto>> GetMyOrdersAsync(
        Guid buyerId);

    Task<OrderResponseDto?> GetForSellerAsync(
        Guid id,
        Guid sellerId);

    Task<List<OrderResponseDto>> GetSellerOrdersAsync(
        Guid sellerId);

    Task<List<OrderResponseDto>> GetAllForAdminAsync();

    Task<OrderResponseDto> CancelAsync(
        Guid id,
        Guid buyerId,
        string? reason);

    Task<OrderResponseDto> ConfirmAsync(
        Guid id,
        Guid actorId,
        string actorRole,
        string? reason);


    Task<PaymentResponseDto> PayAsync(
Guid orderId,
Guid buyerId,
CreatePaymentRequestDto dto);
}
using Gemora.Application.DTOs.Orders;

namespace Gemora.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> RejectAsync(Guid id, Guid sellerId, string? reason, bool alreadySold);
    Task ExpireUnpaidAsync();
    Task MarkMessageReadAsync(Guid id, Guid userId, bool seller, DateTime messageAt);

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




    Task<OrderResponseDto> UpdateDeliveryDetailsAsync(
        Guid orderId,
        Guid buyerId,
        DeliveryDetailsRequestDto dto);

    Task<OrderResponseDto> StartPreparingAsync(
        Guid orderId,
        Guid actorId,
        string actorRole,
        string? reason);

    Task<OrderResponseDto> MarkReadyForDispatchAsync(
        Guid orderId,
        Guid actorId,
        string actorRole,
        string? reason);

    Task<OrderResponseDto> HandOverToCourierAsync(
    Guid orderId,
    Guid actorId,
    string actorRole,
    CreateShipmentRequestDto dto);



Task<OrderResponseDto> MarkInTransitAsync(
    Guid orderId,
    Guid actorId,
    string actorRole,
    string? reason);

Task<OrderResponseDto> MarkOutForDeliveryAsync(
    Guid orderId,
    Guid actorId,
    string actorRole,
    string? reason);

Task<OrderResponseDto> MarkDeliveredAsync(
    Guid orderId,
    Guid actorId,
    string actorRole,
    string? reason);

Task<OrderResponseDto> CompleteAsync(
    Guid orderId,
    Guid buyerId,
    string? reason);





}
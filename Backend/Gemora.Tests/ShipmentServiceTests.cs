using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gemora.Tests;

public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _shipmentRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IShippingPlanRepository> _planRepoMock;
    private readonly Mock<IShipmentTrackingRepository> _trackingRepoMock;
    private readonly Mock<IInsuranceRepository> _insuranceRepoMock;
    private readonly Mock<IShippingAgentService> _agentServiceMock;
    private readonly Mock<ILogger<ShipmentService>> _loggerMock;
    private readonly ShipmentService _shipmentService;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    public ShipmentServiceTests()
    {
        _shipmentRepoMock = new Mock<IShipmentRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _planRepoMock = new Mock<IShippingPlanRepository>();
        _trackingRepoMock = new Mock<IShipmentTrackingRepository>();
        _insuranceRepoMock = new Mock<IInsuranceRepository>();
        _agentServiceMock = new Mock<IShippingAgentService>();
        _loggerMock = new Mock<ILogger<ShipmentService>>();

        _shipmentService = new ShipmentService(
            _shipmentRepoMock.Object,
            _planRepoMock.Object,
            _trackingRepoMock.Object,
            _insuranceRepoMock.Object,
            _orderRepoMock.Object,
            _agentServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithPaidOrder_ShouldSucceed()
    {
        // Arrange
        var order = new Order
        {
            Id = _orderId,
            BuyerUserId = _buyerId,
            SellerUserId = _sellerId,
            TotalAmount = 5000m,
            Currency = "USD",
            Status = OrderStatus.Paid
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Mock AddAsync to return the shipment with generated values
        _shipmentRepoMock.Setup(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment s, CancellationToken ct) => s);

        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            Origin = "Colombo",
            Destination = "Kandy",
            DeclaredValue = 5000m,
            Currency = "USD",
            PackageDescription = "Test package",
            SelectedService = "Express",
            CourierName = "DHL"
        };

        // Act
        var result = await _shipmentService.CreateShipmentAsync(request, _sellerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_buyerId, result.BuyerUserId);
        Assert.Equal(_sellerId, result.SellerUserId);
        Assert.Equal(5000m, result.DeclaredValue);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(ShipmentStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithPendingOrder_ShouldThrow()
    {
        // Arrange
        var order = new Order
        {
            Id = _orderId,
            BuyerUserId = _buyerId,
            SellerUserId = _sellerId,
            TotalAmount = 5000m,
            Currency = "USD",
            Status = OrderStatus.Pending
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Mock AddAsync to return the shipment with generated values
        _shipmentRepoMock.Setup(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment s, CancellationToken ct) => s);

        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            Origin = "Colombo",
            Destination = "Kandy",
            DeclaredValue = 5000m,
            Currency = "USD",
            PackageDescription = "Test",
            SelectedService = "Express",
            CourierName = "DHL"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateShipmentAsync(request, _sellerId));

        Assert.Contains("not eligible for shipment", ex.Message);
    }

    [Fact]
    public async Task CreateShipmentAsync_WrongSeller_ShouldThrow()
    {
        // Arrange
        var wrongSellerId = Guid.NewGuid();
        var order = new Order
        {
            Id = _orderId,
            BuyerUserId = _buyerId,
            SellerUserId = _sellerId,
            TotalAmount = 5000m,
            Currency = "USD",
            Status = OrderStatus.Paid
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Mock AddAsync to return the shipment with generated values
        _shipmentRepoMock.Setup(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment s, CancellationToken ct) => s);

        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            Origin = "Colombo",
            Destination = "Kandy",
            DeclaredValue = 5000m,
            Currency = "USD",
            PackageDescription = "Test",
            SelectedService = "Express",
            CourierName = "DHL"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateShipmentAsync(request, wrongSellerId));

        Assert.Contains("can only create shipments for orders you are selling", ex.Message);
    }

    [Fact]
    public async Task CreateShipmentAsync_DuplicateActiveShipment_ShouldThrow()
    {
        // Arrange
        var order = new Order
        {
            Id = _orderId,
            BuyerUserId = _buyerId,
            SellerUserId = _sellerId,
            TotalAmount = 5000m,
            Currency = "USD",
            Status = OrderStatus.Paid
        };

        var existingShipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = _orderId,
            Status = ShipmentStatus.InTransit
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _shipmentRepoMock.Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShipment);

        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            Origin = "Colombo",
            Destination = "Kandy",
            DeclaredValue = 5000m,
            Currency = "USD",
            PackageDescription = "Test",
            SelectedService = "Express",
            CourierName = "DHL"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateShipmentAsync(request, _sellerId));

        Assert.Contains("active shipment already exists", ex.Message);
    }
}

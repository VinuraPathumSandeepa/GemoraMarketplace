using Gemora.Application.DTOs;
using Gemora.Application.Services;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Gemora.Tests;

public class ShipmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IShippingProviderAdapter> _shippingProviderMock;
    private readonly ShipmentService _shipmentService;

    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    public ShipmentServiceTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _shippingProviderMock = new Mock<IShippingProviderAdapter>();

        _shipmentService = new ShipmentService(_context, _shippingProviderMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test buyer and seller users
        var buyer = new User
        {
            Id = _buyerId,
            Email = "buyer@test.com",
            FullName = "Test Buyer",
            Role = "Buyer"
        };

        var seller = new User
        {
            Id = _sellerId,
            Email = "seller@test.com",
            FullName = "Test Seller",
            Role = "Seller"
        };

        var admin = new User
        {
            Id = _adminId,
            Email = "admin@test.com",
            FullName = "Test Admin",
            Role = "Admin"
        };

        _context.Users.AddRange(buyer, seller, admin);

        // Create a test order (Paid status)
        var order = new Order
        {
            Id = _orderId,
            BuyerId = _buyerId,
            SellerId = _sellerId,
            TotalAmount = 5000m,
            Currency = "USD",
            Status = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateShipmentAsync_WithSellerRole_ShouldSucceed()
    {
        // Arrange
        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            OriginAddress = "123 Main St",
            OriginRegion = "Colombo",
            OriginCountryCode = "LK",
            DestinationAddress = "456 Market St",
            DestinationRegion = "Kandy",
            DestinationCountryCode = "LK",
            PackageDescription = "Test package",
            DeclaredValue = 5000m,
            Currency = "USD"
        };

        // Act
        var result = await _shipmentService.CreateShipmentAsync(_sellerId, "Seller", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_orderId, result.OrderId);
        Assert.Equal(_sellerId, result.SellerId);
        Assert.Equal(_buyerId, result.BuyerId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(5000m, result.DeclaredValue);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithNonSellerRole_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            OriginAddress = "123 Main St",
            OriginRegion = "Colombo",
            OriginCountryCode = "LK",
            DestinationAddress = "456 Market St",
            DestinationRegion = "Kandy",
            DestinationCountryCode = "LK",
            PackageDescription = "Test package"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.CreateShipmentAsync(_buyerId, "Buyer", request));

        Assert.Contains("Only sellers can create shipments", ex.Message);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithAdminRole_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            OriginAddress = "123 Main St",
            OriginRegion = "Colombo",
            OriginCountryCode = "LK",
            DestinationAddress = "456 Market St",
            DestinationRegion = "Kandy",
            DestinationCountryCode = "LK",
            PackageDescription = "Test package"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.CreateShipmentAsync(_adminId, "Admin", request));

        Assert.Contains("Only sellers can create shipments", ex.Message);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithDuplicateOrder_ShouldThrow()
    {
        // Arrange - First create a shipment
        var request1 = new CreateShipmentDto
        {
            OrderId = _orderId,
            OriginAddress = "123 Main St",
            OriginRegion = "Colombo",
            OriginCountryCode = "LK",
            DestinationAddress = "456 Market St",
            DestinationRegion = "Kandy",
            DestinationCountryCode = "LK",
            PackageDescription = "Test package"
        };

        await _shipmentService.CreateShipmentAsync(_sellerId, "Seller", request1);

        // Try to create another shipment for the same order
        var request2 = new CreateShipmentDto
        {
            OrderId = _orderId,
            OriginAddress = "789 Other St",
            OriginRegion = "Galle",
            OriginCountryCode = "LK",
            DestinationAddress = "321 Another St",
            DestinationRegion = "Matara",
            DestinationCountryCode = "LK",
            PackageDescription = "Another package"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateShipmentAsync(_sellerId, "Seller", request2));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_ByBuyer_ShouldThrowUnauthorized()
    {
        // Arrange - Create shipment first
        var shipment = await CreateTestShipmentAsync();
        var request = new UpdateShipmentStatusDto { Status = "InTransit" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _buyerId, "Buyer", request));

        Assert.Contains("Buyers cannot update shipment status", ex.Message);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_DeliveredByNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new UpdateShipmentStatusDto { Status = "Delivered" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _sellerId, "Seller", request));

        Assert.Contains("Only administrators can perform this operational status change", ex.Message);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_ExceptionByNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new UpdateShipmentStatusDto { Status = "Exception" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _sellerId, "Seller", request));

        Assert.Contains("Only administrators can perform this operational status change", ex.Message);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_CustomsHoldByNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new UpdateShipmentStatusDto { Status = "CustomsHold" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _sellerId, "Seller", request));

        Assert.Contains("Only administrators can perform this operational status change", ex.Message);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_DeliveryFailedByNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new UpdateShipmentStatusDto { Status = "DeliveryFailed" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _sellerId, "Seller", request));

        Assert.Contains("Only administrators can perform this operational status change", ex.Message);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_SellerCanUpdateOwnShipment_ShouldSucceed()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new UpdateShipmentStatusDto 
        { 
            Status = "PlanGenerated",
            Location = "Warehouse",
            Notes = "Plan generated successfully"
        };

        // Act
        var result = await _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _sellerId, "Seller", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PlanGenerated", result.Status);
    }

    [Fact]
    public async Task UpdateShipmentStatusAsync_InvalidTransition_ShouldThrow()
    {
        // Arrange - Create shipment with Pending status
        var shipment = await CreateTestShipmentAsync();
        
        // Try to transition from Pending to Delivered (invalid)
        var request = new UpdateShipmentStatusDto { Status = "Delivered" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.UpdateShipmentStatusAsync(shipment.Id, _adminId, "Admin", request));

        Assert.Contains("Cannot transition", ex.Message);
    }

    [Fact]
    public async Task BookShipmentAsync_ByNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.BookShipmentAsync(shipment.Id, _sellerId, "Seller"));

        Assert.Contains("Only administrators can book shipments with couriers", ex.Message);
    }

    [Fact]
    public async Task BookShipmentAsync_ByBuyer_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.BookShipmentAsync(shipment.Id, _buyerId, "Buyer"));

        Assert.Contains("Only administrators can book shipments with couriers", ex.Message);
    }

    [Fact]
    public async Task BookShipmentAsync_WithoutShippingPlan_ShouldThrow()
    {
        // Arrange - Create shipment without a shipping plan
        var shipment = await CreateTestShipmentAsync();
        shipment.Status = "ReadyForBooking";
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.BookShipmentAsync(shipment.Id, _adminId, "Admin"));

        Assert.Contains("No shipping plan has been generated", ex.Message);
    }

    [Fact]
    public async Task BookShipmentAsync_WithUnapprovedPlan_ShouldThrow()
    {
        // Arrange - Create shipment with unapproved plan
        var shipment = await CreateTestShipmentAsync();
        shipment.Status = "ReadyForBooking";
        
        var plan = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            IsApproved = false,
            RecommendedServiceType = "Standard",
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ShippingPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.BookShipmentAsync(shipment.Id, _adminId, "Admin"));

        Assert.Contains("has not been approved", ex.Message);
    }

    [Fact]
    public async Task BookShipmentAsync_WithWrongStatus_ShouldThrow()
    {
        // Arrange - Create shipment with approved plan but wrong status
        var shipment = await CreateTestShipmentAsync();
        shipment.Status = "Pending"; // Not ReadyForBooking
        
        var plan = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            IsApproved = true,
            RecommendedServiceType = "Standard",
            ApprovedBy = _adminId,
            ApprovedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ShippingPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.BookShipmentAsync(shipment.Id, _adminId, "Admin"));

        Assert.Contains("expected 'ReadyForBooking'", ex.Message);
    }

    [Fact]
    public async Task BookShipmentAsync_SuccessfulBooking_ShouldReturnResult()
    {
        // Arrange - Create shipment with approved plan and correct status
        var shipment = await CreateTestShipmentAsync();
        shipment.Status = "ReadyForBooking";
        
        var plan = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            IsApproved = true,
            RecommendedServiceType = "Express",
            ApprovedBy = _adminId,
            ApprovedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ShippingPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Mock successful booking response
        var bookingResult = new CourierBookingResult
        {
            Success = true,
            CourierName = "TestCourier",
            ExternalShipmentReference = "EXT-REF-123",
            TrackingNumber = "TRK-456",
            SelectedService = "Express"
        };

        _shippingProviderMock.Setup(p => p.BookShipmentAsync(
                It.Is<CourierBookingRequest>(r => r != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookingResult);

        // Act
        var result = await _shipmentService.BookShipmentAsync(shipment.Id, _adminId, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("TestCourier", result.CourierName);
        Assert.Equal("TRK-456", result.TrackingNumber);
        Assert.Equal("Booked", shipment.Status);
    }

    [Fact]
    public async Task BookShipmentAsync_IdempotentRetry_ShouldReturnExistingBooking()
    {
        // Arrange - Create already-booked shipment
        var shipment = await CreateTestShipmentAsync();
        shipment.Status = "Booked";
        shipment.TrackingNumber = "EXISTING-TRK";
        shipment.ExternalShipmentReference = "EXT-REF-EXISTING";
        shipment.CourierName = "ExistingCourier";
        shipment.SelectedService = "Standard";
        await _context.SaveChangesAsync();

        // Act - Try to book again (should return existing without calling provider)
        var result = await _shipmentService.BookShipmentAsync(shipment.Id, _adminId, "Admin");

        // Assert - Should return existing booking details
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("EXISTING-TRK", result.TrackingNumber);
        Assert.Equal("EXT-REF-EXISTING", result.ExternalShipmentReference);
        
        // Verify provider was NOT called (idempotency)
        _shippingProviderMock.Verify(p => p.BookShipmentAsync(
                It.IsAny<CourierBookingRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BookShipmentAsync_ProviderFailure_ShouldThrow()
    {
        // Arrange - Create shipment ready for booking
        var shipment = await CreateTestShipmentAsync();
        shipment.Status = "ReadyForBooking";
        
        var plan = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            IsApproved = true,
            RecommendedServiceType = "Standard",
            ApprovedBy = _adminId,
            ApprovedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ShippingPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Mock failed booking response
        var bookingResult = new CourierBookingResult
        {
            Success = false,
            ErrorMessage = "Provider service unavailable"
        };

        _shippingProviderMock.Setup(p => p.BookShipmentAsync(
                It.Is<CourierBookingRequest>(r => r != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookingResult);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.BookShipmentAsync(shipment.Id, _adminId, "Admin"));

        Assert.Contains("Courier booking failed", ex.Message);
    }

    [Fact]
    public async Task AddTrackingEventAsync_ByNonAdmin_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new AddTrackingEventDto
        {
            EventType = "CustomEvent",
            Location = "Test Location",
            Description = "Test event"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.AddTrackingEventAsync(shipment.Id, _sellerId, "Seller", request));

        Assert.Contains("Only administrators can add tracking events", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_BySeller_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = 5000m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _sellerId, "Seller", request));

        Assert.Contains("Only administrators can create insurance records", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_ByBuyer_ShouldThrowUnauthorized()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = 5000m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _buyerId, "Buyer", request));

        Assert.Contains("Only administrators can create insurance records", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_AdminCanCreate_ShouldSucceed()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = 5000m,
            Currency = "USD",
            CoverageType = "Standard"
        };

        // Act
        var result = await _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000m, result.CoverageAmount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("Active", result.Status);
        Assert.StartsWith("SIM-POL-", result.PolicyReference);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_WithZeroCoverageAmount_ShouldThrow()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = 0m,
            Currency = "USD"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request));

        Assert.Contains("Coverage amount must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_WithNegativeCoverageAmount_ShouldThrow()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = -100m,
            Currency = "USD"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request));

        Assert.Contains("Coverage amount must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_WithZeroDeclaredValue_ShouldThrow()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 0m,
            CoverageAmount = 5000m,
            Currency = "USD"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request));

        Assert.Contains("Declared value must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_WithCurrencyMismatch_ShouldThrow()
    {
        // Arrange - Create shipment with USD currency
        var shipment = await CreateTestShipmentAsync();
        
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = 5000m,
            Currency = "EUR" // Different from shipment currency
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request));

        Assert.Contains("Currency must match shipment currency", ex.Message);
    }

    [Fact]
    public async Task CreateInsuranceRecordAsync_Duplicate_ShouldThrow()
    {
        // Arrange - Create first insurance
        var shipment = await CreateTestShipmentAsync();
        var request = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 5000m,
            CoverageAmount = 5000m,
            Currency = "USD"
        };

        await _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request);

        // Try to create second insurance for same shipment
        var request2 = new CreateInsuranceRecordRequest
        {
            ShipmentId = shipment.Id,
            DeclaredValue = 6000m,
            CoverageAmount = 6000m,
            Currency = "USD"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.CreateInsuranceRecordAsync(shipment.Id, _adminId, "Admin", request2));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task GetShipmentByIdAsync_ExistingShipment_ShouldReturnDto()
    {
        // Arrange
        var shipment = await CreateTestShipmentAsync();

        // Act
        var result = await _shipmentService.GetShipmentByIdAsync(shipment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shipment.Id, result.Id);
        Assert.Equal(_orderId, result.OrderId);
    }

    [Fact]
    public async Task GetShipmentByIdAsync_NonExistent_ShouldThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentService.GetShipmentByIdAsync(nonExistentId));

        Assert.Contains("Shipment not found", ex.Message);
    }

    [Fact]
    public async Task GetUserShipmentsAsync_ForBuyer_ShouldFilterByBuyerId()
    {
        // Arrange - Create shipment for buyer
        await CreateTestShipmentAsync();

        // Act
        var results = await _shipmentService.GetUserShipmentsAsync(_buyerId, "Buyer");

        // Assert - Should return shipments where BuyerId matches
        Assert.NotNull(results);
        Assert.All(results, s => Assert.Equal(_buyerId, s.BuyerId));
    }

    [Fact]
    public async Task GetUserShipmentsAsync_ForSeller_ShouldFilterBySellerId()
    {
        // Arrange - Create shipment for seller
        await CreateTestShipmentAsync();

        // Act
        var results = await _shipmentService.GetUserShipmentsAsync(_sellerId, "Seller");

        // Assert - Should return shipments where SellerId matches
        Assert.NotNull(results);
        Assert.All(results, s => Assert.Equal(_sellerId, s.SellerId));
    }

    [Fact]
    public async Task GetUserShipmentsAsync_ForAdmin_ShouldReturnAll()
    {
        // Arrange - Create multiple shipments
        await CreateTestShipmentAsync();
        
        // Create another order with existing seller
        var seller2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller2@test.com",
            FullName = "Test Seller 2",
            Role = "Seller"
        };
        _context.Users.Add(seller2);
        
        var buyer2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "buyer2@test.com",
            FullName = "Test Buyer 2",
            Role = "Buyer"
        };
        _context.Users.Add(buyer2);
        await _context.SaveChangesAsync();
        
        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer2.Id,
            SellerId = seller2.Id,
            TotalAmount = 3000m,
            Currency = "USD",
            Status = "Paid",
            CreatedAt = DateTime.UtcNow
        };
        _context.Orders.Add(order2);
        await _context.SaveChangesAsync();

        var shipment2Request = new CreateShipmentDto
        {
            OrderId = order2.Id,
            OriginAddress = "Other Address",
            OriginRegion = "Galle",
            OriginCountryCode = "LK",
            DestinationAddress = "Dest Address",
            DestinationRegion = "Matara",
            DestinationCountryCode = "LK",
            PackageDescription = "Second shipment"
        };

        await _shipmentService.CreateShipmentAsync(seller2.Id, "Seller", shipment2Request);

        // Act - Admin should see all shipments
        var results = await _shipmentService.GetUserShipmentsAsync(_adminId, "Admin");

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Count >= 2, "Admin should see at least 2 shipments");
    }

    [Fact]
    public async Task GetTrackingEventsAsync_ShouldReturnChronologicalEvents()
    {
        // Arrange - Create shipment (which creates initial tracking event)
        var shipment = await CreateTestShipmentAsync();

        // Act
        var events = await _shipmentService.GetTrackingEventsAsync(shipment.Id, _sellerId, "Seller");

        // Assert
        Assert.NotNull(events);
        Assert.NotEmpty(events);
        Assert.Equal("Created", events.First().EventType);
    }

    // Helper method to create test shipment
    private async Task<Shipment> CreateTestShipmentAsync()
    {
        var request = new CreateShipmentDto
        {
            OrderId = _orderId,
            OriginAddress = "123 Main St",
            OriginRegion = "Colombo",
            OriginCountryCode = "LK",
            DestinationAddress = "456 Market St",
            DestinationRegion = "Kandy",
            DestinationCountryCode = "LK",
            PackageDescription = "Test package",
            DeclaredValue = 5000m,
            Currency = "USD"
        };

        var result = await _shipmentService.CreateShipmentAsync(_sellerId, "Seller", request);
        
        return await _context.Shipments.FindAsync(result.Id) ?? throw new InvalidOperationException("Shipment creation failed");
    }
}

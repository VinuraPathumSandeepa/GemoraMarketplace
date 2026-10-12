
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Gemora.API.Controllers;
using Gemora.Application.DTOs.Orders;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Gemora.Tests;

public class ShipmentControllerTests
{
    [Fact]
    public async Task C3_01_ValidHandover_ReturnsShipment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        var request = new CreateShipmentRequestDto
        {
            CourierName = "QA Courier",
            TrackingNumber = "QA-C3-01-001",
            DispatchNote = "Test shipment"
        };

        var expected = new OrderResponseDto
        {
            Id = orderId,
            Status = OrderStatuses.Paid,
            FulfillmentStatus =
                FulfillmentStatuses.HandedOverToCourier,
            Shipment = new ShipmentResponseDto
            {
                Id = shipmentId,
                CourierName = "QA Courier",
                TrackingNumber = "QA-C3-01-001",
                Status =
                    FulfillmentStatuses.HandedOverToCourier
            }
        };

        var mock = new Mock<IOrderService>();
        var exportComplianceServiceMock = new Mock<IExportComplianceService>();

        mock.Setup(x => x.HandOverToCourierAsync(
                orderId,
                sellerId,
                UserRoles.Seller,
                request))
            .ReturnsAsync(expected);

        var controller = new OrdersController(
            mock.Object,
            exportComplianceServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        sellerId.ToString()),
                    new Claim(
                        ClaimTypes.Role,
                        UserRoles.Seller)
                },
                "TestAuthentication"
            )
        );

        // Act
        var result = await controller.HandOver(
            orderId,
            request
        );

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<OrderResponseDto>(ok.Value);

        Assert.Equal(orderId, order.Id);
        Assert.NotNull(order.Shipment);

        Assert.Equal(
            FulfillmentStatuses.HandedOverToCourier,
            order.FulfillmentStatus);

        Assert.Equal(
            "QA-C3-01-001",
            order.Shipment.TrackingNumber);

        mock.Verify(x => x.HandOverToCourierAsync(
            orderId,
            sellerId,
            UserRoles.Seller,
            request), Times.Once);
    }
}


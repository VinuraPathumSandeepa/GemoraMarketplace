
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

public class CreateOrderControllerTests
{
    [Fact]
    public async Task C2_04_ValidBuyerOrder_Returns201Created()
    {
        // Arrange - Test buyer and gemstone
        var buyerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const int gemId = 4001;

        var request = new CreateOrderRequestDto
        {
            GemListingId = gemId,
            DeliveryDetails = new DeliveryDetailsRequestDto
            {
                RecipientName = "QA Test Buyer",
                RecipientPhone = "0700000000",
                AddressLine1 = "15 QA Test Road",
                City = "Colombo",
                District = "Colombo",
                Region = "Western",
                PostalCode = "00100",
                CountryCode = "LK"
            }
        };

        // Simulated successful response from service
        var expectedOrder = new OrderResponseDto
        {
            Id = orderId,
            BuyerId = buyerId,
            GemListingId = gemId,
            GemTitle = "Test Blue Sapphire",
            AgreedPrice = 150000m,
            Currency = "LKR",
            Status = OrderStatuses.Pending
        };

        var serviceMock = new Mock<IOrderService>();
        var exportComplianceServiceMock = new Mock<IExportComplianceService>();

        serviceMock
            .Setup(service => service.CreateAsync(
                buyerId,
                request))
            .ReturnsAsync(expectedOrder);

        var controller = new OrdersController(
            serviceMock.Object,
            exportComplianceServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Simulate authenticated Buyer identity
        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        buyerId.ToString()),

                    new Claim(
                        ClaimTypes.Role,
                        UserRoles.Buyer)
                },
                "TestAuthentication"
            )
        );

        // Act - Execute actual OrdersController.Create
        var result = await controller.Create(request);

        // Assert - HTTP 201 response from controller
        var created = Assert.IsType<CreatedAtActionResult>(
            result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            created.StatusCode);

        Assert.Equal(
            nameof(OrdersController.GetById),
            created.ActionName);

        Assert.Equal(
            orderId,
            created.RouteValues!["id"]);

        var returnedOrder =
            Assert.IsType<OrderResponseDto>(created.Value);

        Assert.Equal(orderId, returnedOrder.Id);
        Assert.Equal(buyerId, returnedOrder.BuyerId);
        Assert.Equal(gemId, returnedOrder.GemListingId);
        Assert.Equal(OrderStatuses.Pending,
                     returnedOrder.Status);
        Assert.Equal(150000m, returnedOrder.AgreedPrice);

        // Service must be called exactly once
        serviceMock.Verify(
            service => service.CreateAsync(
                buyerId,
                request),
            Times.Once);
    }
}

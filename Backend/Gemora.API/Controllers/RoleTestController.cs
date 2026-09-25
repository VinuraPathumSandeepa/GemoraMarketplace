using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleTestController : ControllerBase
{
    // Any authenticated user
    // GET: /api/RoleTest/authenticated
    [HttpGet("authenticated")]
    public IActionResult AuthenticatedUser()
    {
        return Ok(new
        {
            message = "You are authenticated."
        });
    }

    // Buyer only
    // GET: /api/RoleTest/buyer
    [Authorize(Roles = UserRoles.Buyer)]
    [HttpGet("buyer")]
    public IActionResult BuyerOnly()
    {
        return Ok(new
        {
            message = "Buyer access granted."
        });
    }

    // Seller only
    // GET: /api/RoleTest/seller
    [Authorize(Roles = UserRoles.Seller)]
    [HttpGet("seller")]
    public IActionResult SellerOnly()
    {
        return Ok(new
        {
            message = "Seller access granted."
        });
    }

    // Gemologist only
    // GET: /api/RoleTest/gemologist
    [Authorize(Roles = UserRoles.Gemologist)]
    [HttpGet("gemologist")]
    public IActionResult GemologistOnly()
    {
        return Ok(new
        {
            message = "Gemologist access granted."
        });
    }

    // Export Officer only
    // GET: /api/RoleTest/export-officer
    [Authorize(Roles = UserRoles.ExportOfficer)]
    [HttpGet("export-officer")]
    public IActionResult ExportOfficerOnly()
    {
        return Ok(new
        {
            message = "Export Officer access granted."
        });
    }

    // Admin only
    // GET: /api/RoleTest/admin
    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            message = "Admin access granted."
        });
    }

    // Example of multiple allowed roles
    // GET: /api/RoleTest/staff
    [Authorize(
        Roles = UserRoles.Admin + "," +
                UserRoles.Gemologist + "," +
                UserRoles.ExportOfficer
    )]
    [HttpGet("staff")]
    public IActionResult StaffOnly()
    {
        return Ok(new
        {
            message = "Staff access granted."
        });
    }
}
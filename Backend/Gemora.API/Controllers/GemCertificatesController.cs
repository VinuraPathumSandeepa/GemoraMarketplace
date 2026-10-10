
using System.Security.Claims;
using System.Text.RegularExpressions;

using Gemora.Domain.Constants;
using Gemora.Infrastructure.Data;
using Gemora.Infrastructure.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/GemCertificates")]
[Authorize(
    Roles = UserRoles.Seller + "," + UserRoles.Gemologist
)]
public sealed class GemCertificatesController : ControllerBase
{
    // ============================================================
    // CONSTANTS
    // ============================================================

    private const string PrivateCertificatePrefix =
        "supabase-private://gem-certificates/";

    private const string LegacyCertificatePrefix =
        "/uploads/certificates/";

    // LocalFileStorageService generates filenames using
    // Guid.NewGuid():N followed by a permitted extension.

    private static readonly Regex SafeLegacyFileName = new(
        @"\A[a-fA-F0-9]{32}\.(pdf|jpg|jpeg|png)\z",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase
    );

    // ============================================================
    // DEPENDENCIES
    // ============================================================

    private readonly ApplicationDbContext _context;

    private readonly SupabaseStorageClient _supabaseStorage;

    private readonly IWebHostEnvironment _environment;

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public GemCertificatesController(
        ApplicationDbContext context,
        SupabaseStorageClient supabaseStorage,
        IWebHostEnvironment environment)
    {
        _context = context;

        _supabaseStorage = supabaseStorage;

        _environment = environment;
    }

    // ============================================================
    // GET AUTHORIZED CERTIFICATE ACCESS
    //
    // GET:
    // /api/GemCertificates/listings/{listingId}/access
    //
    // Returns:
    //
    // Supabase certificate:
    // {
    //   "kind": "signed",
    //   "url": "https://...signed-url...",
    //   "expiresInSeconds": 60
    // }
    //
    // Legacy certificate:
    // {
    //   "kind": "legacy",
    //   "url": "/api/GemCertificates/listings/5/legacy"
    // }
    // ============================================================

    [HttpGet("listings/{listingId:int}/access")]
    public async Task<IActionResult> GetCertificateAccess(
        int listingId,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";

        // --------------------------------------------------------
        // 1. Check the authenticated user's permissions.
        // --------------------------------------------------------

        var (certificateReference, rejection) =
            await ResolveAuthorizedCertificateAsync(
                listingId,
                cancellationToken
            );

        if (rejection != null)
        {
            return rejection;
        }

        if (string.IsNullOrWhiteSpace(certificateReference))
        {
            return NotFound(new
            {
                message = "Certificate was not found."
            });
        }

        // --------------------------------------------------------
        // 2. NEW PRIVATE SUPABASE CERTIFICATE
        // --------------------------------------------------------

        if (certificateReference.StartsWith(
                PrivateCertificatePrefix,
                StringComparison.Ordinal))
        {
            // Permission checking has already succeeded.
            // Only now do we generate the signed URL.

            var signedUrl =
                await _supabaseStorage
                    .CreateCertificateSignedUrlAsync(
                        certificateReference,
                        cancellationToken
                    );

            return Ok(new
            {
                kind = "signed",

                url = signedUrl,

                expiresInSeconds = 60
            });
        }

        // --------------------------------------------------------
        // 3. LEGACY LOCAL CERTIFICATE
        // --------------------------------------------------------

        if (TryResolveLegacyCertificate(
                certificateReference,
                out var physicalPath,
                out _))
        {
            // Check that the original file is still available.

            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound(new
                {
                    message =
                        "The original certificate file is no longer available."
                });
            }

            // Return a protected API route, never a direct
            // /uploads/certificates/... public link.

            return Ok(new
            {
                kind = "legacy",

                url =
                    $"/api/GemCertificates/listings/{listingId}/legacy"
            });
        }

        return NotFound(new
        {
            message = "Certificate storage reference is invalid."
        });
    }

    // ============================================================
    // READ AN EXISTING LOCAL CERTIFICATE
    //
    // GET:
    // /api/GemCertificates/listings/{listingId}/legacy
    //
    // This route is protected by JWT authentication.
    //
    // The original physical file path is NEVER accepted
    // directly from the request.
    // ============================================================

    [HttpGet("listings/{listingId:int}/legacy")]
    public async Task<IActionResult> GetLegacyCertificate(
        int listingId,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";

        // --------------------------------------------------------
        // 1. Check ownership / Gemologist permissions again.
        //
        // Every request must independently enforce access.
        // --------------------------------------------------------

        var (certificateReference, rejection) =
            await ResolveAuthorizedCertificateAsync(
                listingId,
                cancellationToken
            );

        if (rejection != null)
        {
            return rejection;
        }

        // --------------------------------------------------------
        // 2. Accept only valid legacy certificate references.
        // --------------------------------------------------------

        if (!TryResolveLegacyCertificate(
                certificateReference,
                out var physicalPath,
                out var contentType))
        {
            return NotFound(new
            {
                message = "Legacy certificate was not found."
            });
        }

        // --------------------------------------------------------
        // 3. Check physical file.
        // --------------------------------------------------------

        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound(new
            {
                message =
                    "The original certificate file is no longer available."
            });
        }

        // --------------------------------------------------------
        // 4. Return certificate through the authenticated API.
        //
        // ASP.NET Core disposes this stream after responding.
        // --------------------------------------------------------

        var fileStream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );

        return File(
            fileStream,
            contentType
        );
    }

    // ============================================================
    // RESOLVE CERTIFICATE WITH AUTHORIZATION
    //
    // Seller:
    // Must own the listing.
    //
    // Gemologist:
    // A verification record must exist for the listing.
    //
    // Return NotFound for inaccessible listings to avoid
    // disclosing another Seller's listing information.
    // ============================================================

    private async Task<(
        string? CertificateReference,
        IActionResult? Rejection)>
        ResolveAuthorizedCertificateAsync(
            int listingId,
            CancellationToken cancellationToken)
    {
        // --------------------------------------------------------
        // 1. Read authenticated user ID from JWT.
        // --------------------------------------------------------

        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!Guid.TryParse(
                userIdValue,
                out var currentUserId))
        {
            return (
                null,
                Unauthorized(new
                {
                    message =
                        "The authenticated user identifier is invalid."
                })
            );
        }

        // --------------------------------------------------------
        // 2. Get requested listing.
        // --------------------------------------------------------

        var listing = await _context.GemListings
            .AsNoTracking()
            .Where(g => g.Id == listingId)
            .Select(g => new
            {
                g.Id,

                g.SellerId,

                g.CertificateUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (listing == null)
        {
            return (
                null,
                NotFound(new
                {
                    message = "Certificate was not found."
                })
            );
        }

        // --------------------------------------------------------
        // 3. SELLER AUTHORIZATION
        // --------------------------------------------------------

        if (User.IsInRole(UserRoles.Seller))
        {
            if (listing.SellerId != currentUserId)
            {
                return (
                    null,
                    NotFound(new
                    {
                        message = "Certificate was not found."
                    })
                );
            }
        }

        // --------------------------------------------------------
        // 4. GEMOLOGIST AUTHORIZATION
        //
        // The existing workflow uses a shared verification
        // queue. A Gemologist may access certificate evidence
        // for a listing with a verification record.
        // --------------------------------------------------------

        else if (User.IsInRole(UserRoles.Gemologist))
        {
            var hasVerification =
                await _context.GemVerifications
                    .AsNoTracking()
                    .AnyAsync(
                        v => v.GemListingId == listingId,
                        cancellationToken
                    );

            if (!hasVerification)
            {
                return (
                    null,
                    NotFound(new
                    {
                        message = "Certificate was not found."
                    })
                );
            }
        }
        else
        {
            return (
                null,
                Forbid()
            );
        }

        // --------------------------------------------------------
        // 5. Check whether certificate evidence exists.
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                listing.CertificateUrl))
        {
            return (
                null,
                NotFound(new
                {
                    message =
                        "No certificate has been uploaded for this listing."
                })
            );
        }

        // --------------------------------------------------------
        // 6. Return the database reference.
        //
        // This value is only returned internally.
        // --------------------------------------------------------

        return (
            listing.CertificateUrl,
            null
        );
    }

    // ============================================================
    // RESOLVE SAFE LEGACY CERTIFICATE PATH
    //
    // Prevents arbitrary file access and path traversal.
    // ============================================================

    private bool TryResolveLegacyCertificate(
        string? reference,
        out string physicalPath,
        out string contentType)
    {
        physicalPath = "";

        contentType = "";

        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        // --------------------------------------------------------
        // Only accept this exact prefix.
        // --------------------------------------------------------

        if (!reference.StartsWith(
                LegacyCertificatePrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // --------------------------------------------------------
        // Extract filename without accepting nested paths.
        // --------------------------------------------------------

        var fileName = reference.Substring(
            LegacyCertificatePrefix.Length
        );

        if (!SafeLegacyFileName.IsMatch(fileName))
        {
            return false;
        }

        // --------------------------------------------------------
        // Resolve the existing web-root directory.
        // --------------------------------------------------------

        var webRootPath =
            _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot"
            );
        }

        physicalPath = Path.Combine(
            webRootPath,
            "uploads",
            "certificates",
            fileName
        );

        // --------------------------------------------------------
        // Determine the correct response content type.
        // --------------------------------------------------------

        var extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        contentType = extension switch
        {
            ".pdf" => "application/pdf",

            ".jpg" => "image/jpeg",

            ".jpeg" => "image/jpeg",

            ".png" => "image/png",

            _ => ""
        };

        return !string.IsNullOrWhiteSpace(contentType);
    }
}

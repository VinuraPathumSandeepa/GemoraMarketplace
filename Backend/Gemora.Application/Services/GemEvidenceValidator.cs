using Gemora.Application.DTOs.GemAI;
using Gemora.Application.Interfaces;
using Gemora.Domain.Entities;

namespace Gemora.Application.Services;

public class GemEvidenceValidator : IGemEvidenceValidator
{
    public GemEvidenceValidationResultDto Validate(
        GemListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        var result =
            new GemEvidenceValidationResultDto();


        // ========================================================
        // CHECK 1 — BASIC LISTING INFORMATION
        // ========================================================

        result.ChecksPerformed.Add(
            "Basic listing information checked.");

        if (string.IsNullOrWhiteSpace(
                listing.Title))
        {
            result.Issues.Add(
                "Listing title is missing.");
        }

        if (string.IsNullOrWhiteSpace(
                listing.GemType))
        {
            result.Issues.Add(
                "Declared gemstone type is missing.");
        }

        if (listing.CaratWeight <= 0)
        {
            result.Issues.Add(
                "Carat weight must be greater than zero.");
        }

        if (listing.Price <= 0)
        {
            result.Issues.Add(
                "Listing price must be greater than zero.");
        }


        // ========================================================
        // CHECK 2 — GEMSTONE IMAGE
        // ========================================================

        result.ChecksPerformed.Add(
            "Gemstone image evidence checked.");

        if (string.IsNullOrWhiteSpace(
                listing.PrimaryImageUrl))
        {
            result.Issues.Add(
                "A gemstone image has not been provided.");
        }


        // ========================================================
        // CHECK 3 — CERTIFICATE EVIDENCE
        // ========================================================

        result.ChecksPerformed.Add(
            "Certificate evidence checked.");

        var hasCertificateUrl =
            !string.IsNullOrWhiteSpace(
                listing.CertificateUrl);

        var hasCertificateNumber =
            !string.IsNullOrWhiteSpace(
                listing.CertificateNumber);

        var hasCertificateAuthority =
            !string.IsNullOrWhiteSpace(
                listing.CertificateAuthority);


        // If the seller provides certificate evidence,
        // the associated certificate metadata should also exist.
        if (hasCertificateUrl)
        {
            if (!hasCertificateNumber)
            {
                result.Warnings.Add(
                    "A certificate file was provided, but the certificate number is missing.");
            }

            if (!hasCertificateAuthority)
            {
                result.Warnings.Add(
                    "A certificate file was provided, but the certificate authority is missing.");
            }
        }
        else
        {
            result.Warnings.Add(
                "No gemstone certificate file has been provided.");
        }


        // Metadata without the actual certificate is suspicious.
        if (!hasCertificateUrl &&
            (hasCertificateNumber ||
             hasCertificateAuthority))
        {
            result.Warnings.Add(
                "Certificate information was entered, but no certificate file was uploaded.");
        }


        // ========================================================
        // CHECK 4 — GEMSTONE CHARACTERISTICS
        // ========================================================

        result.ChecksPerformed.Add(
            "Gemstone characteristics checked.");

        if (string.IsNullOrWhiteSpace(
                listing.Color))
        {
            result.Warnings.Add(
                "Gemstone color has not been provided.");
        }

        if (string.IsNullOrWhiteSpace(
                listing.Clarity))
        {
            result.Warnings.Add(
                "Gemstone clarity has not been provided.");
        }

        if (string.IsNullOrWhiteSpace(
                listing.Cut))
        {
            result.Warnings.Add(
                "Gemstone cut has not been provided.");
        }


        // ========================================================
        // FINAL RESULT
        // ========================================================

        result.IsValid =
            result.Issues.Count == 0;

        return result;
    }
}
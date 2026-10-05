namespace Gemora.Domain.Enums;

public enum ExportRequestStatus
{
    Draft = 0,
    Submitted = 1,
    UnderComplianceReview = 2,
    UnderOfficerReview = 3,
    RevisionRequired = 4,
    Approved = 5,
    Rejected = 6,
    Cancelled = 7
}

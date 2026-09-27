namespace Gemora.Application.AgentTools;

public static class ComplianceAgentToolNames
{
    public const string ReadExportRequest = "readExportRequest";
    public const string ReadComplianceDocumentMetadata = "readComplianceDocumentMetadata";
    public const string RunComplianceRulesValidator = "runComplianceRulesValidator";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>
    {
        ReadExportRequest,
        ReadComplianceDocumentMetadata,
        RunComplianceRulesValidator
    };
}

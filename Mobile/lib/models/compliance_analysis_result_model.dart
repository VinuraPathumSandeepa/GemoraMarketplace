class ComplianceAgentResultModel {
  final String summary;
  final bool deterministicComplete;
  final List<String> missingRequirements;
  final List<String> warnings;
  final bool requiresOfficerAttention;
  final double confidence;
  final String disclaimer;

  ComplianceAgentResultModel({
    required this.summary,
    required this.deterministicComplete,
    required this.missingRequirements,
    required this.warnings,
    required this.requiresOfficerAttention,
    required this.confidence,
    required this.disclaimer,
  });

  factory ComplianceAgentResultModel.fromJson(Map<String, dynamic> json) {
    return ComplianceAgentResultModel(
      summary: json['summary']?.toString() ?? '',
      deterministicComplete: json['deterministicComplete'] == true,
      missingRequirements: (json['missingRequirements'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      warnings: (json['warnings'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      requiresOfficerAttention: json['requiresOfficerAttention'] == true,
      confidence: (json['confidence'] as num?)?.toDouble() ?? 0.0,
      disclaimer: json['disclaimer']?.toString() ?? '',
    );
  }
}

class ComplianceWorkflowAnalysisResultModel {
  final bool success;
  final String message;
  final String? errorCode;
  final String? workflowId;
  final ComplianceAgentResultModel? assessment;

  ComplianceWorkflowAnalysisResultModel({
    required this.success,
    required this.message,
    this.errorCode,
    this.workflowId,
    this.assessment,
  });

  factory ComplianceWorkflowAnalysisResultModel.fromJson(
      Map<String, dynamic> json) {
    return ComplianceWorkflowAnalysisResultModel(
      success: json['success'] == true,
      message: json['message']?.toString() ?? '',
      errorCode: json['errorCode']?.toString(),
      workflowId: json['workflowId']?.toString(),
      assessment: json['assessment'] != null
          ? ComplianceAgentResultModel.fromJson(
              json['assessment'] as Map<String, dynamic>)
          : null,
    );
  }
}

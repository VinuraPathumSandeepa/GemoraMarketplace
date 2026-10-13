class GemAiAnalysisModel {
  final String status;
  final String? suggestedGemType;
  final double? confidenceScore;
  final String findings;
  final bool imageAnalyzed;
  final List<String> visualObservations;
  final List<String> evidenceWarnings;
  final List<String> riskFlags;
  final List<String> validationIssues;
  final List<String> stepsCompleted;
  final List<GemAiWorkflowStepModel> workflowSteps;
  final String? errorCategory;
  final int? retryAfterSeconds;

  const GemAiAnalysisModel({
    required this.status,
    required this.suggestedGemType,
    required this.confidenceScore,
    required this.findings,
    required this.imageAnalyzed,
    required this.visualObservations,
    this.evidenceWarnings = const [],
    required this.riskFlags,
    required this.validationIssues,
    required this.stepsCompleted,
    this.workflowSteps = const [],
    this.errorCategory,
    this.retryAfterSeconds,
  });

  factory GemAiAnalysisModel.fromJson(Map<String, dynamic> json) {
    return GemAiAnalysisModel(
      status: json['status']?.toString() ?? '',
      suggestedGemType: _nullableString(json['suggestedGemType']),
      confidenceScore: _nullableDouble(json['confidenceScore']),
      findings: json['findings']?.toString() ?? '',
      imageAnalyzed: json['imageAnalyzed'] == true,
      visualObservations: _stringList(json['visualObservations']),
      evidenceWarnings: _stringList(json['evidenceWarnings']),
      riskFlags: _stringList(json['riskFlags']),
      validationIssues: _stringList(json['validationIssues']),
      stepsCompleted: _stringList(json['stepsCompleted']),
      workflowSteps: (json['workflowSteps'] as List?)
              ?.whereType<Map<String, dynamic>>()
              .map(GemAiWorkflowStepModel.fromJson)
              .toList() ??
          const [],
      errorCategory: _nullableString(json['errorCategory']),
      retryAfterSeconds: (json['retryAfterSeconds'] as num?)?.toInt(),
    );
  }

  static String? _nullableString(dynamic value) {
    if (value == null) {
      return null;
    }

    final text = value.toString().trim();

    return text.isEmpty ? null : text;
  }

  static double? _nullableDouble(dynamic value) {
    if (value == null) {
      return null;
    }

    if (value is num) {
      return value.toDouble();
    }

    return double.tryParse(value.toString());
  }

  static List<String> _stringList(dynamic value) {
    if (value is! List) {
      return const [];
    }

    return value
        .map((item) => item.toString().trim())
        .where((item) => item.isNotEmpty)
        .toList();
  }
}

class GemAiWorkflowStepModel {
  final String id;
  final String status;

  const GemAiWorkflowStepModel({required this.id, required this.status});

  factory GemAiWorkflowStepModel.fromJson(Map<String, dynamic> json) =>
      GemAiWorkflowStepModel(
        id: json['id']?.toString() ?? '',
        status: json['status']?.toString() ?? 'Pending',
      );
}

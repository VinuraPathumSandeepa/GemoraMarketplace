class GemAiAnalysisModel {
  final String status;
  final String? suggestedGemType;
  final double? confidenceScore;
  final String findings;
  final bool imageAnalyzed;
  final List<String> visualObservations;
  final List<String> riskFlags;
  final List<String> validationIssues;
  final List<String> stepsCompleted;

  const GemAiAnalysisModel({
    required this.status,
    required this.suggestedGemType,
    required this.confidenceScore,
    required this.findings,
    required this.imageAnalyzed,
    required this.visualObservations,
    required this.riskFlags,
    required this.validationIssues,
    required this.stepsCompleted,
  });

  factory GemAiAnalysisModel.fromJson(Map<String, dynamic> json) {
    return GemAiAnalysisModel(
      status: json['status']?.toString() ?? '',
      suggestedGemType: _nullableString(json['suggestedGemType']),
      confidenceScore: _nullableDouble(json['confidenceScore']),
      findings: json['findings']?.toString() ?? '',
      imageAnalyzed: json['imageAnalyzed'] == true,
      visualObservations: _stringList(json['visualObservations']),
      riskFlags: _stringList(json['riskFlags']),
      validationIssues: _stringList(json['validationIssues']),
      stepsCompleted: _stringList(json['stepsCompleted']),
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

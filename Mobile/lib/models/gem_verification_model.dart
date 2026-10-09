class GemVerificationModel {
  final int verificationId;

  final String decision;

  final String? reviewNotes;

  final DateTime? createdAt;

  final DateTime? reviewedAt;

  final int gemListingId;

  final String sellerId;

  final String sellerName;

  final String title;

  final String gemType;

  final String description;

  final double caratWeight;

  final String color;

  final String clarity;

  final String cut;

  final double price;

  final String currency;

  final String? primaryImageUrl;

  final String? certificateNumber;

  final String? certificateAuthority;

  final String? certificateUrl;

  final String listingStatus;

  final String aiStatus;

  final String? aiSuggestedGemType;

  final double? aiConfidenceScore;

  final String? aiFindings;

  final String? aiRiskFlags;

  final DateTime? aiProcessedAt;

  final String? gemologistId;

  final String? gemologistName;

  const GemVerificationModel({
    required this.verificationId,
    required this.decision,
    required this.reviewNotes,
    required this.createdAt,
    required this.reviewedAt,
    required this.gemListingId,
    required this.sellerId,
    required this.sellerName,
    required this.title,
    required this.gemType,
    required this.description,
    required this.caratWeight,
    required this.color,
    required this.clarity,
    required this.cut,
    required this.price,
    required this.currency,
    required this.primaryImageUrl,
    required this.certificateNumber,
    required this.certificateAuthority,
    required this.certificateUrl,
    required this.listingStatus,
    required this.aiStatus,
    required this.aiSuggestedGemType,
    required this.aiConfidenceScore,
    required this.aiFindings,
    required this.aiRiskFlags,
    required this.aiProcessedAt,
    required this.gemologistId,
    required this.gemologistName,
  });

  factory GemVerificationModel.fromJson(Map<String, dynamic> json) {
    return GemVerificationModel(
      verificationId: _toInt(json['verificationId']),
      decision: _toString(json['decision']),
      reviewNotes: _nullableString(json['reviewNotes']),
      createdAt: _toDateTime(json['createdAt']),
      reviewedAt: _toDateTime(json['reviewedAt']),
      gemListingId: _toInt(json['gemListingId']),
      sellerId: _toString(json['sellerId']),
      sellerName: _toString(json['sellerName']),
      title: _toString(json['title']),
      gemType: _toString(json['gemType']),
      description: _toString(json['description']),
      caratWeight: _toDouble(json['caratWeight']),
      color: _toString(json['color']),
      clarity: _toString(json['clarity']),
      cut: _toString(json['cut']),
      price: _toDouble(json['price']),
      currency: _toString(json['currency']),
      primaryImageUrl: _nullableString(json['primaryImageUrl']),
      certificateNumber: _nullableString(json['certificateNumber']),
      certificateAuthority: _nullableString(json['certificateAuthority']),
      certificateUrl: _nullableString(json['certificateUrl']),
      listingStatus: _toString(json['listingStatus']),
      aiStatus: _toString(json['aiStatus']),
      aiSuggestedGemType: _nullableString(json['aiSuggestedGemType']),
      aiConfidenceScore: _nullableDouble(json['aiConfidenceScore']),
      aiFindings: _nullableString(json['aiFindings']),
      aiRiskFlags: _nullableString(json['aiRiskFlags']),
      aiProcessedAt: _toDateTime(json['aiProcessedAt']),
      gemologistId: _nullableString(json['gemologistId']),
      gemologistName: _nullableString(json['gemologistName']),
    );
  }

  bool get isPending {
    return decision.toLowerCase() == 'pending';
  }

  bool get hasCertificate {
    return certificateNumber != null ||
        certificateAuthority != null ||
        certificateUrl != null;
  }

  static int _toInt(dynamic value) {
    if (value is int) {
      return value;
    }

    return int.tryParse(value?.toString() ?? '') ?? 0;
  }

  static double _toDouble(dynamic value) {
    if (value is num) {
      return value.toDouble();
    }

    return double.tryParse(value?.toString() ?? '') ?? 0;
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

  static String _toString(dynamic value) {
    return value?.toString() ?? '';
  }

  static String? _nullableString(dynamic value) {
    if (value == null) {
      return null;
    }

    final text = value.toString().trim();

    if (text.isEmpty) {
      return null;
    }

    return text;
  }

  static DateTime? _toDateTime(dynamic value) {
    if (value == null) {
      return null;
    }

    return DateTime.tryParse(value.toString());
  }
}

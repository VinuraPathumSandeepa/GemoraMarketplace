class ExportRequestModel {
  final String id;
  final String originCountry;
  final String destinationCountry;
  final double declaredValue;
  final String currency;
  final String? purpose;
  final String status;
  final String? reviewNotes;
  final DateTime? submittedAt;
  final DateTime? reviewedAt;
  final DateTime createdAt;
  final DateTime updatedAt;

  ExportRequestModel({
    required this.id,
    required this.originCountry,
    required this.destinationCountry,
    required this.declaredValue,
    required this.currency,
    this.purpose,
    required this.status,
    this.reviewNotes,
    this.submittedAt,
    this.reviewedAt,
    required this.createdAt,
    required this.updatedAt,
  });

  factory ExportRequestModel.fromJson(Map<String, dynamic> json) {
    return ExportRequestModel(
      id: json['id']?.toString() ?? '',
      originCountry: json['originCountry']?.toString() ?? '',
      destinationCountry: json['destinationCountry']?.toString() ?? '',
      declaredValue: (json['declaredValue'] is num)
          ? (json['declaredValue'] as num).toDouble()
          : double.tryParse(json['declaredValue']?.toString() ?? '0') ?? 0.0,
      currency: json['currency']?.toString() ?? 'USD',
      purpose: json['purpose']?.toString(),
      status: json['status']?.toString() ?? 'Draft',
      reviewNotes: json['reviewNotes']?.toString(),
      submittedAt: json['submittedAt'] != null
          ? DateTime.tryParse(json['submittedAt'].toString())
          : null,
      reviewedAt: json['reviewedAt'] != null
          ? DateTime.tryParse(json['reviewedAt'].toString())
          : null,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'originCountry': originCountry,
      'destinationCountry': destinationCountry,
      'declaredValue': declaredValue,
      'currency': currency,
      'purpose': purpose,
      'status': status,
      'reviewNotes': reviewNotes,
      'submittedAt': submittedAt?.toIso8601String(),
      'reviewedAt': reviewedAt?.toIso8601String(),
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt.toIso8601String(),
    };
  }
}

class GemListingModel {
  final int id;

  final String title;
  final String description;
  final String gemType;

  final double caratWeight;

  final String color;
  final String clarity;
  final String cut;

  final double price;
  final String currency;

  final String status;

  final String? primaryImageUrl;

  final String? certificateNumber;
  final String? certificateAuthority;
  final String? certificateUrl;

  final String? countryCode;
  final String? region;

  final DateTime? createdAt;
  final DateTime? updatedAt;

  const GemListingModel({
    required this.id,
    required this.title,
    required this.description,
    required this.gemType,
    required this.caratWeight,
    required this.color,
    required this.clarity,
    required this.cut,
    required this.price,
    required this.currency,
    required this.status,
    this.primaryImageUrl,
    this.certificateNumber,
    this.certificateAuthority,
    this.certificateUrl,
    this.countryCode,
    this.region,
    this.createdAt,
    this.updatedAt,
  });

  // ==========================================
  // FROM JSON
  // ==========================================

  factory GemListingModel.fromJson(Map<String, dynamic> json) {
    return GemListingModel(
      id: _toInt(json['id']),

      title: json['title']?.toString() ?? '',

      description: json['description']?.toString() ?? '',

      gemType: json['gemType']?.toString() ?? '',

      caratWeight: _toDouble(json['caratWeight']),

      color: json['color']?.toString() ?? '',

      clarity: json['clarity']?.toString() ?? '',

      cut: json['cut']?.toString() ?? '',

      price: _toDouble(json['price']),

      currency: json['currency']?.toString() ?? 'USD',

      status: json['status']?.toString() ?? 'Draft',

      primaryImageUrl: _nullableString(json['primaryImageUrl']),

      certificateNumber: _nullableString(json['certificateNumber']),

      certificateAuthority: _nullableString(json['certificateAuthority']),

      certificateUrl: _nullableString(json['certificateUrl']),

      countryCode: _nullableString(json['countryCode']),

      region: _nullableString(json['region']),

      createdAt: _toDateTime(json['createdAt']),

      updatedAt: _toDateTime(json['updatedAt']),
    );
  }

  // ==========================================
  // STATUS HELPERS
  // ==========================================

  bool get isDraft => status.toLowerCase() == 'draft';

  bool get isPendingVerification =>
      status.toLowerCase() == 'pendingverification';

  bool get isApproved => status.toLowerCase() == 'approved';

  bool get isChangesRequested => status.toLowerCase() == 'changesrequested';

  bool get isRejected => status.toLowerCase() == 'rejected';

  bool get canEdit => isDraft || isChangesRequested;

  // ==========================================
  // PARSING HELPERS
  // ==========================================

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

  static String? _nullableString(dynamic value) {
    final text = value?.toString().trim();

    if (text == null || text.isEmpty) {
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

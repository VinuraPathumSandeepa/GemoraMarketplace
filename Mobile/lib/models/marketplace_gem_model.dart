import '../config/api_config.dart';

class MarketplaceGemModel {
  final int id;

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

  final String countryCode;
  final String region;

  final bool isAvailable;

  final String status;

  final DateTime? createdAt;
  final DateTime? updatedAt;

  const MarketplaceGemModel({
    required this.id,
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
    required this.countryCode,
    required this.region,
    required this.isAvailable,
    this.status = '',
    this.createdAt,
    this.updatedAt,
  });

  factory MarketplaceGemModel.fromJson(Map<String, dynamic> json) {
    return MarketplaceGemModel(
      id: _toInt(json['id']),
      sellerName: json['sellerName']?.toString() ?? '',
      title: json['title']?.toString() ?? '',
      gemType: json['gemType']?.toString() ?? '',
      description: json['description']?.toString() ?? '',
      caratWeight: _toDouble(json['caratWeight']),
      color: json['color']?.toString() ?? '',
      clarity: json['clarity']?.toString() ?? '',
      cut: json['cut']?.toString() ?? '',
      price: _toDouble(json['price']),
      currency: json['currency']?.toString() ?? 'LKR',
      primaryImageUrl: _nullableString(json['primaryImageUrl']),
      certificateNumber: _nullableString(json['certificateNumber']),
      certificateAuthority: _nullableString(json['certificateAuthority']),
      countryCode: json['countryCode']?.toString() ?? '',
      region: json['region']?.toString() ?? '',
      isAvailable: _toBool(json['isAvailable']),
      status: json['status']?.toString() ?? '',
      createdAt: _toDateTime(json['createdAt']),
      updatedAt: _toDateTime(json['updatedAt']),
    );
  }

  bool get isCertified {
    return certificateNumber != null || certificateAuthority != null;
  }

  String? get resolvedImageUrl => ApiConfig.resolveImageUrl(primaryImageUrl);

  static bool _toBool(dynamic value) {
    if (value is bool) return value;
    if (value is String) {
      final normalized = value.trim().toLowerCase();
      if (normalized == 'true') return true;
      if (normalized == 'false') return false;
    }
    return true;
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

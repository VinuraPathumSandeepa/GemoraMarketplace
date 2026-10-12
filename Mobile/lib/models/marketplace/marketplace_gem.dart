import '../../config/api_config.dart';

class MarketplaceGem {
  final int id;
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
  final String sellerName;
  final bool isAvailable;

  MarketplaceGem({
    required this.id,
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
    required this.sellerName,
    required this.isAvailable,
  });

  bool get isCertified =>
      (certificateNumber != null && certificateNumber!.trim().isNotEmpty) ||
      (certificateAuthority != null && certificateAuthority!.trim().isNotEmpty);

  String? get resolvedImageUrl => ApiConfig.resolveImageUrl(primaryImageUrl);

  static double _toDouble(dynamic value, [double fallback = 0.0]) {
    if (value is num) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? fallback;
    return fallback;
  }

  static int _toInt(dynamic value, [int fallback = 0]) {
    if (value is int) return value;
    if (value is num) return value.toInt();
    if (value is String) return int.tryParse(value) ?? fallback;
    return fallback;
  }

  static bool _toBool(dynamic value, [bool fallback = true]) {
    if (value is bool) return value;
    if (value is String) {
      final lower = value.trim().toLowerCase();
      if (lower == 'true') return true;
      if (lower == 'false') return false;
    }
    return fallback;
  }

  factory MarketplaceGem.fromJson(Map<String, dynamic> json) => MarketplaceGem(
    id: _toInt(json['id']),
    title: json['title']?.toString() ?? '',
    gemType: json['gemType']?.toString() ?? '',
    description: json['description']?.toString() ?? '',
    caratWeight: _toDouble(json['caratWeight']),
    color: json['color']?.toString() ?? '',
    clarity: json['clarity']?.toString() ?? '',
    cut: json['cut']?.toString() ?? '',
    price: _toDouble(json['price']),
    currency: json['currency']?.toString() ?? 'LKR',
    primaryImageUrl: json['primaryImageUrl']?.toString(),
    certificateNumber: json['certificateNumber']?.toString(),
    certificateAuthority: json['certificateAuthority']?.toString(),
    countryCode: json['countryCode']?.toString() ?? '',
    region: json['region']?.toString() ?? '',
    sellerName: json['sellerName']?.toString() ?? '',
    isAvailable: _toBool(json['isAvailable'], true),
  );
}

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

  factory MarketplaceGem.fromJson(Map<String, dynamic> json) => MarketplaceGem(
    id: json['id'] is int
        ? json['id'] as int
        : int.tryParse(json['id']?.toString() ?? '') ?? 0,
    title: json['title']?.toString() ?? '',
    gemType: json['gemType']?.toString() ?? '',
    description: json['description']?.toString() ?? '',
    caratWeight: (json['caratWeight'] as num?)?.toDouble() ?? 0,
    color: json['color']?.toString() ?? '',
    clarity: json['clarity']?.toString() ?? '',
    cut: json['cut']?.toString() ?? '',
    price: (json['price'] as num?)?.toDouble() ?? 0,
    currency: json['currency']?.toString() ?? 'LKR',
    primaryImageUrl: json['primaryImageUrl']?.toString(),
    certificateNumber: json['certificateNumber']?.toString(),
    certificateAuthority: json['certificateAuthority']?.toString(),
    countryCode: json['countryCode']?.toString() ?? '',
    region: json['region']?.toString() ?? '',
    sellerName: json['sellerName']?.toString() ?? '',
    isAvailable: json['isAvailable'] as bool? ?? true,
  );
}

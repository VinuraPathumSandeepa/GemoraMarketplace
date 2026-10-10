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

  factory MarketplaceGem.fromJson(Map<String, dynamic> json) => MarketplaceGem(
        id: json['id'] as int,
        title: json['title'] ?? '',
        gemType: json['gemType'] ?? '',
        description: json['description'] ?? '',
        caratWeight: (json['caratWeight'] as num?)?.toDouble() ?? 0,
        color: json['color'] ?? '',
        clarity: json['clarity'] ?? '',
        cut: json['cut'] ?? '',
        price: (json['price'] as num?)?.toDouble() ?? 0,
        currency: json['currency'] ?? 'LKR',
        primaryImageUrl: json['primaryImageUrl'],
        certificateNumber: json['certificateNumber'],
        certificateAuthority: json['certificateAuthority'],
        countryCode: json['countryCode'] ?? '',
        region: json['region'] ?? '',
        sellerName: json['sellerName'] ?? '',
        isAvailable: json['isAvailable'] ?? false,
      );
}

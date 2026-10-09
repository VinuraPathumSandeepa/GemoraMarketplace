class CertificateAccessModel {
  final String kind;
  final String url;
  final int? expiresInSeconds;

  const CertificateAccessModel({
    required this.kind,
    required this.url,
    required this.expiresInSeconds,
  });

  factory CertificateAccessModel.fromJson(Map<String, dynamic> json) {
    return CertificateAccessModel(
      kind: json['kind']?.toString() ?? '',
      url: json['url']?.toString() ?? '',
      expiresInSeconds: _nullableInt(json['expiresInSeconds']),
    );
  }

  bool get isSigned => kind.toLowerCase() == 'signed';

  bool get isLegacy => kind.toLowerCase() == 'legacy';

  static int? _nullableInt(dynamic value) {
    if (value == null) {
      return null;
    }

    if (value is int) {
      return value;
    }

    return int.tryParse(value.toString());
  }
}

class ShipmentModel {
  final Map<String, dynamic> data;
  ShipmentModel.fromJson(this.data);
  String text(String key) => data[key]?.toString() ?? '';
  String get id => text('id');
  String get orderId => text('orderId');
  String get status => text('status');
  String get currency => text('currency');
  double? get value => number(data['declaredValue']);
  DateTime? get createdAt => parseDate(data['createdAt']);
  String route(String prefix) => ['Address', 'Region', 'CountryCode']
      .map((suffix) => text('$prefix$suffix'))
      .where((v) => v.isNotEmpty)
      .join(', ');
  static double? number(dynamic value) =>
      value is num ? value.toDouble() : double.tryParse('$value');
  static DateTime? parseDate(dynamic value) {
    if (value == null || '$value'.isEmpty) return null;
    var raw = '$value';
    if (!RegExp(r'(Z|[+-]\d{2}:\d{2})$').hasMatch(raw)) raw += 'Z';
    return DateTime.tryParse(raw)?.toLocal();
  }
}

class ShipmentOrder {
  final Map<String, dynamic> data;
  ShipmentOrder.fromJson(this.data);
  String text(String key) => data[key]?.toString() ?? '';
  String get id => text('id');
  double? get amount =>
      ShipmentModel.number(data['totalAmount'] ?? data['agreedPrice']);
  bool get unread =>
      text('status') == 'Pending' && data['sellerReadAt'] == null;
}

class ShipmentValidation {
  static String? requiredText(String? value, {int max = 500}) {
    if (value == null || value.trim().isEmpty) return 'This field is required.';
    if (value.trim().length > max) return 'Use $max characters or fewer.';
    return null;
  }

  static String? country(String? value) =>
      RegExp(r'^[A-Za-z]{2}$').hasMatch(value?.trim() ?? '')
      ? null
      : 'Enter a two-letter code, such as LK or US.';
  static String? weight(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final parsed = double.tryParse(value.trim());
    return parsed != null && parsed.isFinite && parsed > 0
        ? null
        : 'Enter a weight greater than zero.';
  }
}

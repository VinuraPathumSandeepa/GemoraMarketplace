class OrderHistoryItem {
  final String? previousStatus;
  final String newStatus;
  final String changedByName;
  final String? reason;
  final DateTime createdAt;

  const OrderHistoryItem({
    required this.previousStatus,
    required this.newStatus,
    required this.changedByName,
    required this.reason,
    required this.createdAt,
  });

  factory OrderHistoryItem.fromJson(Map<String, dynamic> json) {
    return OrderHistoryItem(
      previousStatus: _asNullableString(json['previousStatus']),
      newStatus: json['newStatus']?.toString() ?? '',
      changedByName: json['changedByName']?.toString() ?? '',
      reason: _asNullableString(json['reason']),
      createdAt: _parseDate(json['createdAt']),
    );
  }
}

class OrderModel {
  final String id;
  final String orderNumber;
  final int? gemListingId;
  final String gemTitle;
  final String? gemImageUrl;
  final String buyerName;
  final String sellerName;
  final double agreedPrice;
  final String currency;
  final String status;
  final String? shippingCountryCode;
  final String? exportStatus;
  final String? exportRequestId;
  final DateTime? paymentDueAt;
  final DateTime? paidAt;
  final String fulfillmentStatus;
  final DateTime? handedOverAt;
  final DateTime? deliveredAt;
  final String? shipmentStatus;
  final String? trackingNumber;
  final DateTime createdAt;
  final List<OrderHistoryItem> statusHistory;

  const OrderModel({
    required this.id,
    required this.orderNumber,
    required this.gemListingId,
    required this.gemTitle,
    required this.gemImageUrl,
    required this.buyerName,
    required this.sellerName,
    required this.agreedPrice,
    required this.currency,
    required this.status,
    this.shippingCountryCode,
    this.exportStatus,
    this.exportRequestId,
    this.paymentDueAt,
    this.paidAt,
    this.fulfillmentStatus = 'Pending',
    this.handedOverAt,
    this.deliveredAt,
    this.shipmentStatus,
    this.trackingNumber,
    required this.createdAt,
    required this.statusHistory,
  });

  factory OrderModel.fromJson(Map<String, dynamic> json) {
    final history = json['statusHistory'];
    final shipment = json['shipment'];

    return OrderModel(
      id: json['id']?.toString() ?? '',
      orderNumber: json['orderNumber']?.toString() ?? '',
      gemListingId: _asInt(json['gemListingId']),
      gemTitle: json['gemTitle']?.toString() ?? '',
      gemImageUrl: _asNullableString(json['gemImageUrl']),
      buyerName: json['buyerName']?.toString() ?? '',
      sellerName: json['sellerName']?.toString() ?? '',
      agreedPrice: _asDouble(json['agreedPrice']),
      currency: json['currency']?.toString() ?? '',
      status: json['status']?.toString() ?? '',
      shippingCountryCode: _asNullableString(json['shippingCountryCode']),
      exportStatus: _asNullableString(json['exportStatus']),
      exportRequestId: _asNullableString(json['exportRequestId']),
      paymentDueAt: _parseNullableDate(json['paymentDueAt']),
      paidAt: _parseNullableDate(json['paidAt']),
      fulfillmentStatus:
          _asNullableString(json['fulfillmentStatus']) ?? 'Pending',
      handedOverAt: _parseNullableDate(json['handedOverAt']),
      deliveredAt: _parseNullableDate(json['deliveredAt']),
      shipmentStatus: shipment is Map
          ? _asNullableString(shipment['status'])
          : null,
      trackingNumber: shipment is Map
          ? _asNullableString(shipment['trackingNumber'])
          : null,
      createdAt: _parseDate(json['createdAt']),
      statusHistory: history is List
          ? history
                .whereType<Map<String, dynamic>>()
                .map(OrderHistoryItem.fromJson)
                .toList()
          : const [],
    );
  }
}

String? _asNullableString(dynamic value) {
  final text = value?.toString().trim();
  return text == null || text.isEmpty ? null : text;
}

int? _asInt(dynamic value) {
  if (value is int) return value;
  return int.tryParse(value?.toString() ?? '');
}

double _asDouble(dynamic value) {
  if (value is num) return value.toDouble();
  return double.tryParse(value?.toString() ?? '') ?? 0;
}

DateTime _parseDate(dynamic value) {
  return DateTime.tryParse(value?.toString() ?? '') ??
      DateTime.fromMillisecondsSinceEpoch(0);
}

DateTime? _parseNullableDate(dynamic value) {
  final text = value?.toString();
  return text == null || text.isEmpty ? null : DateTime.tryParse(text);
}

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

class FulfillmentHistoryItem {
  final String? previousStatus;
  final String newStatus;
  final String changedByName;
  final String? note;
  final DateTime createdAt;

  const FulfillmentHistoryItem({
    required this.previousStatus,
    required this.newStatus,
    required this.changedByName,
    required this.note,
    required this.createdAt,
  });

  factory FulfillmentHistoryItem.fromJson(Map<String, dynamic> json) =>
      FulfillmentHistoryItem(
        previousStatus: json['previousStatus']?.toString(),
        newStatus: json['newStatus']?.toString() ?? '',
        changedByName: json['changedByName']?.toString() ?? '',
        note: json['note']?.toString(),
        createdAt:
            DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
            DateTime.now(),
      );
}

class OrderDeliveryDetails {
  final String recipientName;
  final String recipientPhone;
  final String alternatePhone;
  final String addressLine1;
  final String addressLine2;
  final String city;
  final String district;
  final String region;
  final String postalCode;
  final String countryCode;
  final String nearestLandmark;
  final String deliveryInstructions;
  final bool isLocked;

  const OrderDeliveryDetails({
    required this.recipientName,
    required this.recipientPhone,
    this.alternatePhone = '',
    required this.addressLine1,
    this.addressLine2 = '',
    required this.city,
    required this.district,
    required this.region,
    required this.postalCode,
    required this.countryCode,
    this.nearestLandmark = '',
    this.deliveryInstructions = '',
    required this.isLocked,
  });

  factory OrderDeliveryDetails.fromJson(Map<String, dynamic> json) =>
      OrderDeliveryDetails(
        recipientName: json['recipientName']?.toString() ?? '',
        recipientPhone: json['recipientPhone']?.toString() ?? '',
        alternatePhone: json['alternatePhone']?.toString() ?? '',
        addressLine1: json['addressLine1']?.toString() ?? '',
        addressLine2: json['addressLine2']?.toString() ?? '',
        city: json['city']?.toString() ?? '',
        district: json['district']?.toString() ?? '',
        region: json['region']?.toString() ?? '',
        postalCode: json['postalCode']?.toString() ?? '',
        countryCode: json['countryCode']?.toString() ?? '',
        nearestLandmark: json['nearestLandmark']?.toString() ?? '',
        deliveryInstructions: json['deliveryInstructions']?.toString() ?? '',
        isLocked: json['isLocked'] == true,
      );
}

class OrderShipment {
  final String id;
  final String courierName;
  final String trackingNumber;
  final String? trackingUrl;
  final String status;
  final DateTime? expectedDeliveryDate;

  const OrderShipment({
    required this.id,
    required this.courierName,
    required this.trackingNumber,
    required this.trackingUrl,
    required this.status,
    required this.expectedDeliveryDate,
  });

  factory OrderShipment.fromJson(Map<String, dynamic> json) => OrderShipment(
    id: json['id']?.toString() ?? '',
    courierName: json['courierName']?.toString() ?? '',
    trackingNumber: json['trackingNumber']?.toString() ?? '',
    trackingUrl: json['trackingUrl']?.toString(),
    status: json['status']?.toString() ?? '',
    expectedDeliveryDate: DateTime.tryParse(
      json['expectedDeliveryDate']?.toString() ?? '',
    ),
  );
}

class PaymentIntentModel {
  final String paymentIntentId;
  final String provider;
  final double orderTotalAmount;
  final double advancePercentage;
  final double amount;
  final double remainingAmount;
  final String currency;
  final DateTime? expiresAt;

  const PaymentIntentModel({
    required this.paymentIntentId,
    required this.provider,
    required this.orderTotalAmount,
    required this.advancePercentage,
    required this.amount,
    required this.remainingAmount,
    required this.currency,
    required this.expiresAt,
  });

  factory PaymentIntentModel.fromJson(Map<String, dynamic> json) =>
      PaymentIntentModel(
        paymentIntentId: json['paymentIntentId']?.toString() ?? '',
        provider: json['provider']?.toString() ?? '',
        orderTotalAmount: (json['orderTotalAmount'] as num?)?.toDouble() ?? 0,
        advancePercentage: (json['advancePercentage'] as num?)?.toDouble() ?? 0,
        amount: (json['amount'] as num?)?.toDouble() ?? 0,
        remainingAmount: (json['remainingAmount'] as num?)?.toDouble() ?? 0,
        currency: json['currency']?.toString() ?? '',
        expiresAt: DateTime.tryParse(json['expiresAt']?.toString() ?? ''),
      );
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
  final String fulfillmentStatus;
  final DateTime? paymentDueAt;
  final DateTime? paidAt;
  final DateTime? handedOverAt;
  final DateTime? deliveredAt;
  final OrderDeliveryDetails? deliveryDetails;
  final OrderShipment? shipment;
  final String? shippingCountryCode;
  final String? exportStatus;
  final String? exportRequestId;
  final DateTime createdAt;
  final List<OrderHistoryItem> statusHistory;
  final List<FulfillmentHistoryItem> fulfillmentHistory;

  String? get shipmentStatus => shipment?.status;
  String? get trackingNumber => shipment?.trackingNumber;

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
    this.fulfillmentStatus = 'Pending',
    this.paymentDueAt,
    this.paidAt,
    this.handedOverAt,
    this.deliveredAt,
    this.deliveryDetails,
    this.shipment,
    this.shippingCountryCode,
    this.exportStatus,
    this.exportRequestId,
    required this.createdAt,
    required this.statusHistory,
    this.fulfillmentHistory = const [],
  });

  factory OrderModel.fromJson(Map<String, dynamic> json) {
    final history = json['statusHistory'];
    final fulfillmentHistory = json['fulfillmentHistory'];
    final deliveryDetails = json['deliveryDetails'];
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
      fulfillmentStatus:
          _asNullableString(json['fulfillmentStatus']) ?? 'Pending',
      paymentDueAt: _parseNullableDate(json['paymentDueAt']),
      paidAt: _parseNullableDate(json['paidAt']),
      handedOverAt: _parseNullableDate(json['handedOverAt']),
      deliveredAt: _parseNullableDate(json['deliveredAt']),
      deliveryDetails: deliveryDetails is Map<String, dynamic>
          ? OrderDeliveryDetails.fromJson(deliveryDetails)
          : null,
      shipment: shipment is Map<String, dynamic>
          ? OrderShipment.fromJson(shipment)
          : null,
      createdAt: _parseDate(json['createdAt']),
      statusHistory: history is List
          ? history
                .whereType<Map<String, dynamic>>()
                .map(OrderHistoryItem.fromJson)
                .toList()
          : const [],
      fulfillmentHistory: fulfillmentHistory is List
          ? fulfillmentHistory
                .whereType<Map<String, dynamic>>()
                .map(FulfillmentHistoryItem.fromJson)
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

class OrderHistoryItem {
  final String? previousStatus;
  final String newStatus;
  final String changedByName;
  final String? reason;
  final DateTime createdAt;

  OrderHistoryItem({
    required this.previousStatus,
    required this.newStatus,
    required this.changedByName,
    required this.reason,
    required this.createdAt,
  });

  factory OrderHistoryItem.fromJson(Map<String, dynamic> json) =>
      OrderHistoryItem(
        previousStatus: json['previousStatus'],
        newStatus: json['newStatus'] ?? '',
        changedByName: json['changedByName'] ?? '',
        reason: json['reason'],
        createdAt: json['createdAt'] != null
            ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
            : DateTime.now(),
      );
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
  final String addressLine1;
  final String city;
  final String district;
  final String region;
  final String postalCode;
  final String countryCode;
  final bool isLocked;

  const OrderDeliveryDetails({
    required this.recipientName,
    required this.recipientPhone,
    required this.addressLine1,
    required this.city,
    required this.district,
    required this.region,
    required this.postalCode,
    required this.countryCode,
    required this.isLocked,
  });

  factory OrderDeliveryDetails.fromJson(Map<String, dynamic> json) =>
      OrderDeliveryDetails(
        recipientName: json['recipientName']?.toString() ?? '',
        recipientPhone: json['recipientPhone']?.toString() ?? '',
        addressLine1: json['addressLine1']?.toString() ?? '',
        city: json['city']?.toString() ?? '',
        district: json['district']?.toString() ?? '',
        region: json['region']?.toString() ?? '',
        postalCode: json['postalCode']?.toString() ?? '',
        countryCode: json['countryCode']?.toString() ?? '',
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

  OrderModel({
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

  factory OrderModel.fromJson(Map<String, dynamic> json) => OrderModel(
    id: json['id']?.toString() ?? '',
    orderNumber: json['orderNumber'] ?? '',
    gemListingId: json['gemListingId'] as int?,
    gemTitle: json['gemTitle'] ?? '',
    gemImageUrl: json['gemImageUrl'],
    buyerName: json['buyerName'] ?? '',
    sellerName: json['sellerName'] ?? '',
    agreedPrice: (json['agreedPrice'] as num?)?.toDouble() ?? 0,
    currency: json['currency'] ?? '',
    status: json['status'] ?? '',
    fulfillmentStatus: json['fulfillmentStatus']?.toString() ?? 'Pending',
    paymentDueAt: DateTime.tryParse(json['paymentDueAt']?.toString() ?? ''),
    paidAt: DateTime.tryParse(json['paidAt']?.toString() ?? ''),
    handedOverAt: DateTime.tryParse(json['handedOverAt']?.toString() ?? ''),
    deliveredAt: DateTime.tryParse(json['deliveredAt']?.toString() ?? ''),
    deliveryDetails: json['deliveryDetails'] is Map
        ? OrderDeliveryDetails.fromJson(
            Map<String, dynamic>.from(json['deliveryDetails'] as Map),
          )
        : null,
    shipment: json['shipment'] is Map
        ? OrderShipment.fromJson(
            Map<String, dynamic>.from(json['shipment'] as Map),
          )
        : null,
    shippingCountryCode: json['shippingCountryCode']?.toString(),
    exportStatus: json['exportStatus']?.toString(),
    exportRequestId: json['exportRequestId']?.toString(),
    createdAt: json['createdAt'] != null
        ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
        : DateTime.now(),
    statusHistory: ((json['statusHistory'] ?? []) as List)
        .map((e) => OrderHistoryItem.fromJson(e as Map<String, dynamic>))
        .toList(),
    fulfillmentHistory: ((json['fulfillmentHistory'] ?? []) as List)
        .whereType<Map>()
        .map(
          (e) => FulfillmentHistoryItem.fromJson(Map<String, dynamic>.from(e)),
        )
        .toList(),
  );
}

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
        createdAt: DateTime.parse(json['createdAt']),
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
  final DateTime createdAt;
  final List<OrderHistoryItem> statusHistory;

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
    required this.createdAt,
    required this.statusHistory,
  });

  factory OrderModel.fromJson(Map<String, dynamic> json) => OrderModel(
    id: json['id'].toString(),
    orderNumber: json['orderNumber'] ?? '',
    gemListingId: json['gemListingId'],
    gemTitle: json['gemTitle'] ?? '',
    gemImageUrl: json['gemImageUrl'],
    buyerName: json['buyerName'] ?? '',
    sellerName: json['sellerName'] ?? '',
    agreedPrice: (json['agreedPrice'] as num?)?.toDouble() ?? 0,
    currency: json['currency'] ?? '',
    status: json['status'] ?? '',
    createdAt: DateTime.parse(json['createdAt']),
    statusHistory: ((json['statusHistory'] ?? []) as List)
        .map((e) => OrderHistoryItem.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
}

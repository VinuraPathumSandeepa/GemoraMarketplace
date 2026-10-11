class BuyerExportTrackingModel {
  final String orderId;
  final String orderNumber;
  final int? gemListingId;
  final String gemTitle;
  final String? gemImageUrl;
  final String? exportRequestId;
  final String status;
  final String stageDescription;
  final String originCountry;
  final String destinationCountry;
  final DateTime? submittedAt;
  final DateTime? reviewedAt;
  final DateTime? updatedAt;
  final bool isApproved;
  final bool requiresExport;

  BuyerExportTrackingModel({
    required this.orderId,
    required this.orderNumber,
    this.gemListingId,
    required this.gemTitle,
    this.gemImageUrl,
    this.exportRequestId,
    required this.status,
    required this.stageDescription,
    required this.originCountry,
    required this.destinationCountry,
    this.submittedAt,
    this.reviewedAt,
    this.updatedAt,
    required this.isApproved,
    required this.requiresExport,
  });

  factory BuyerExportTrackingModel.fromJson(Map<String, dynamic> json) {
    return BuyerExportTrackingModel(
      orderId: json['orderId']?.toString() ?? '',
      orderNumber: json['orderNumber']?.toString() ?? '',
      gemListingId: json['gemListingId'] as int?,
      gemTitle: json['gemTitle']?.toString() ?? '',
      gemImageUrl: json['gemImageUrl']?.toString(),
      exportRequestId: json['exportRequestId']?.toString(),
      status: json['status']?.toString() ?? '',
      stageDescription: json['stageDescription']?.toString() ?? '',
      originCountry: json['originCountry']?.toString() ?? '',
      destinationCountry: json['destinationCountry']?.toString() ?? '',
      submittedAt: json['submittedAt'] != null
          ? DateTime.tryParse(json['submittedAt'].toString())
          : null,
      reviewedAt: json['reviewedAt'] != null
          ? DateTime.tryParse(json['reviewedAt'].toString())
          : null,
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString())
          : null,
      isApproved: json['isApproved'] == true,
      requiresExport: json['requiresExport'] == true,
    );
  }
}

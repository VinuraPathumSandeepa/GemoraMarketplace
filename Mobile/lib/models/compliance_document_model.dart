class ComplianceDocumentModel {
  final String id;
  final String exportRequestId;
  final String documentType;
  final String? documentNumber;
  final String? issuer;
  final DateTime? issueDate;
  final DateTime? expiryDate;
  final bool hasUploadedFile;
  final String status;
  final DateTime uploadedAt;

  ComplianceDocumentModel({
    required this.id,
    required this.exportRequestId,
    required this.documentType,
    this.documentNumber,
    this.issuer,
    this.issueDate,
    this.expiryDate,
    required this.hasUploadedFile,
    required this.status,
    required this.uploadedAt,
  });

  factory ComplianceDocumentModel.fromJson(Map<String, dynamic> json) {
    final fileUrlStr = json['fileUrl']?.toString();
    return ComplianceDocumentModel(
      id: json['id']?.toString() ?? '',
      exportRequestId: json['exportRequestId']?.toString() ?? '',
      documentType: json['documentType']?.toString() ?? '',
      documentNumber: json['documentNumber']?.toString(),
      issuer: json['issuer']?.toString(),
      issueDate: json['issueDate'] != null
          ? DateTime.tryParse(json['issueDate'].toString())
          : null,
      expiryDate: json['expiryDate'] != null
          ? DateTime.tryParse(json['expiryDate'].toString())
          : null,
      hasUploadedFile: fileUrlStr != null && fileUrlStr.trim().isNotEmpty,
      status: json['status']?.toString() ?? 'Pending',
      uploadedAt: json['uploadedAt'] != null
          ? DateTime.tryParse(json['uploadedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'exportRequestId': exportRequestId,
      'documentType': documentType,
      'documentNumber': documentNumber,
      'issuer': issuer,
      'issueDate': issueDate?.toIso8601String(),
      'expiryDate': expiryDate?.toIso8601String(),
      'hasUploadedFile': hasUploadedFile,
      'status': status,
      'uploadedAt': uploadedAt.toIso8601String(),
    };
  }
}

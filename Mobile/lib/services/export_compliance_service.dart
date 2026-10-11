import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

import '../config/api_config.dart';
import '../models/buyer_export_tracking_model.dart';
import '../models/compliance_analysis_result_model.dart';
import '../models/compliance_document_model.dart';
import '../models/export_request_model.dart';
import 'token_storage_service.dart';

class ExportComplianceService {
  final TokenStorageService _tokenStorage = TokenStorageService();
  final http.Client? client;

  ExportComplianceService({this.client});

  Future<http.Response> _get(Uri url, Map<String, String> headers) =>
      client?.get(url, headers: headers) ?? http.get(url, headers: headers);

  Future<Map<String, String>> _getHeaders() async {
    final token = await _tokenStorage.getToken();
    if (token == null || token.isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    return {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };
  }

  String _parseErrorResponse(http.Response response, String fallbackMessage) {
    if (response.statusCode == 401) {
      return 'Your session has expired. Please sign in again.';
    }
    if (response.statusCode == 403) {
      return "You don't have permission to modify this export request.";
    }
    if (response.statusCode == 404) {
      return "This export request or document could not be found.";
    }
    if (response.statusCode == 409) {
      return "This request can't be changed in its current state.";
    }
    if (response.statusCode == 413) {
      return "The selected file is too large. Maximum file size is 10 MB.";
    }
    if (response.statusCode == 415) {
      return "Only PDF, JPEG, and PNG compliance documents are allowed.";
    }
    if (response.statusCode >= 500) {
      return "We couldn't upload the compliance document. Please try again.";
    }

    try {
      if (response.body.isNotEmpty) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        if (data.containsKey('message') && data['message'] != null) {
          return data['message'].toString();
        }
      }
    } catch (_) {}

    return fallbackMessage;
  }

  // ==========================================
  // GET MY EXPORT REQUESTS
  // ==========================================
  Future<List<ExportRequestModel>> getMyExportRequests() async {
    final headers = await _getHeaders();

    final response = await http.get(
      Uri.parse(ApiConfig.myExportRequests),
      headers: headers,
    );

    if (response.statusCode != 200) {
      if (response.statusCode == 403) {
        throw Exception(
          "You don't have permission to view these export requests.",
        );
      }
      throw Exception(
        _parseErrorResponse(
          response,
          "We couldn't load your export requests. Please try again.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawList = data['requests'] as List<dynamic>? ?? [];

    return rawList
        .map(
          (item) => ExportRequestModel.fromJson(item as Map<String, dynamic>),
        )
        .toList();
  }

  // ==========================================
  // GET EXPORT REQUEST BY ID
  // ==========================================
  Future<ExportRequestModel> getExportRequestById(String id) async {
    final headers = await _getHeaders();

    final response = await http.get(
      Uri.parse('${ApiConfig.exportRequests}/$id'),
      headers: headers,
    );

    if (response.statusCode != 200) {
      throw Exception(
        _parseErrorResponse(
          response,
          "This export request could not be found.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawRequest = data['request'] as Map<String, dynamic>;

    return ExportRequestModel.fromJson(rawRequest);
  }

  // ==========================================
  // CREATE EXPORT REQUEST
  // ==========================================
  Future<ExportRequestModel> createExportRequest({
    required String originCountry,
    required String destinationCountry,
    required double declaredValue,
    String currency = 'USD',
    String? purpose,
  }) async {
    final headers = await _getHeaders();

    final bodyPayload = <String, dynamic>{
      'originCountry': originCountry.trim(),
      'destinationCountry': destinationCountry.trim(),
      'declaredValue': declaredValue,
      'currency': currency.trim(),
    };

    if (purpose != null && purpose.trim().isNotEmpty) {
      bodyPayload['purpose'] = purpose.trim();
    }

    final response = await http.post(
      Uri.parse(ApiConfig.exportRequests),
      headers: headers,
      body: jsonEncode(bodyPayload),
    );

    if (response.statusCode != 201 && response.statusCode != 200) {
      throw Exception(
        _parseErrorResponse(
          response,
          "We couldn't create your export request. Please try again.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawRequest = data['request'] as Map<String, dynamic>;

    return ExportRequestModel.fromJson(rawRequest);
  }

  // ==========================================
  // GET COMPLIANCE DOCUMENTS
  // ==========================================
  Future<List<ComplianceDocumentModel>> getComplianceDocuments(
    String exportRequestId,
  ) async {
    final headers = await _getHeaders();

    final response = await http.get(
      Uri.parse('${ApiConfig.exportRequests}/$exportRequestId/documents'),
      headers: headers,
    );

    if (response.statusCode != 200) {
      throw Exception(
        _parseErrorResponse(
          response,
          "We couldn't load compliance documents. Please try again.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawList = data['documents'] as List<dynamic>? ?? [];

    return rawList
        .map(
          (item) =>
              ComplianceDocumentModel.fromJson(item as Map<String, dynamic>),
        )
        .toList();
  }

  // ==========================================
  // ADD COMPLIANCE DOCUMENT METADATA
  // ==========================================
  Future<ComplianceDocumentModel> addComplianceDocument(
    String exportRequestId, {
    required String documentType,
    String? documentNumber,
    String? issuer,
    DateTime? issueDate,
    DateTime? expiryDate,
  }) async {
    final headers = await _getHeaders();

    final bodyPayload = <String, dynamic>{'documentType': documentType.trim()};

    if (documentNumber != null && documentNumber.trim().isNotEmpty) {
      bodyPayload['documentNumber'] = documentNumber.trim();
    }
    if (issuer != null && issuer.trim().isNotEmpty) {
      bodyPayload['issuer'] = issuer.trim();
    }
    if (issueDate != null) {
      bodyPayload['issueDate'] = issueDate.toIso8601String();
    }
    if (expiryDate != null) {
      bodyPayload['expiryDate'] = expiryDate.toIso8601String();
    }

    final response = await http.post(
      Uri.parse('${ApiConfig.exportRequests}/$exportRequestId/documents'),
      headers: headers,
      body: jsonEncode(bodyPayload),
    );

    if (response.statusCode != 201 && response.statusCode != 200) {
      throw Exception(
        _parseErrorResponse(
          response,
          "We couldn't add document details. Please try again.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawDocument = data['document'] as Map<String, dynamic>;

    return ComplianceDocumentModel.fromJson(rawDocument);
  }

  // ==========================================
  // UPLOAD COMPLIANCE DOCUMENT FILE
  // ==========================================
  Future<ComplianceDocumentModel> uploadComplianceDocumentFile(
    String exportRequestId,
    String documentId,
    List<int> fileBytes,
    String fileName,
  ) async {
    // 1. Pre-validation: file size <= 10MB
    const maxBytes = 10 * 1024 * 1024;
    if (fileBytes.isEmpty) {
      throw Exception('The selected file is empty.');
    }
    if (fileBytes.length > maxBytes) {
      throw Exception(
        'The selected file is too large. Maximum file size is 10 MB.',
      );
    }

    // 2. Pre-validation: extension check
    final lowerName = fileName.toLowerCase();
    String? contentType;
    if (lowerName.endsWith('.pdf')) {
      contentType = 'application/pdf';
    } else if (lowerName.endsWith('.jpg') || lowerName.endsWith('.jpeg')) {
      contentType = 'image/jpeg';
    } else if (lowerName.endsWith('.png')) {
      contentType = 'image/png';
    } else {
      throw Exception('Please select a PDF, JPG, JPEG, or PNG file.');
    }

    final token = await _tokenStorage.getToken();
    if (token == null || token.isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }

    final uri = Uri.parse(
      '${ApiConfig.exportRequests}/$exportRequestId/documents/$documentId/file',
    );

    final request = http.MultipartRequest('POST', uri);
    request.headers['Authorization'] = 'Bearer $token';

    final mediaTypeParts = contentType.split('/');
    request.files.add(
      http.MultipartFile.fromBytes(
        'file', // Exact multipart field name required by controller
        fileBytes,
        filename: fileName,
        contentType: MediaType(mediaTypeParts[0], mediaTypeParts[1]),
      ),
    );

    final streamedResponse = await request.send();
    final response = await http.Response.fromStream(streamedResponse);

    if (response.statusCode != 200 && response.statusCode != 201) {
      throw Exception(
        _parseErrorResponse(
          response,
          "We couldn't upload the compliance document file. Please try again.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawDocument = data['document'] as Map<String, dynamic>;

    return ComplianceDocumentModel.fromJson(rawDocument);
  }

  // ==========================================
  // SUBMIT EXPORT REQUEST
  // ==========================================
  Future<ExportRequestModel> submitExportRequest(String requestId) async {
    final headers = await _getHeaders();

    final response = await http.post(
      Uri.parse('${ApiConfig.exportRequests}/$requestId/submit'),
      headers: headers,
    );

    if (response.statusCode != 200) {
      if (response.statusCode == 401) {
        throw Exception('Your session has expired. Please sign in again.');
      }
      if (response.statusCode == 403) {
        throw Exception(
          "You don't have permission to submit this export request.",
        );
      }
      if (response.statusCode == 404) {
        throw Exception("This export request could not be found.");
      }
      if (response.statusCode == 409) {
        throw Exception(
          "This request can't be submitted in its current state.",
        );
      }

      try {
        if (response.body.isNotEmpty) {
          final data = jsonDecode(response.body) as Map<String, dynamic>;
          if (data.containsKey('message') && data['message'] != null) {
            throw Exception(data['message'].toString());
          }
        }
      } catch (e) {
        if (e is Exception && !e.toString().contains('FormatException')) {
          rethrow;
        }
      }

      throw Exception(
        "We couldn't submit your export request. Please try again.",
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawRequest = data['request'] as Map<String, dynamic>;

    return ExportRequestModel.fromJson(rawRequest);
  }

  // ==========================================
  // RUN COMPLIANCE ANALYSIS
  // ==========================================
  Future<ComplianceWorkflowAnalysisResultModel> runComplianceAnalysis(
    String requestId,
  ) async {
    final headers = await _getHeaders();

    final response = await http.post(
      Uri.parse('${ApiConfig.exportRequests}/$requestId/compliance-analysis'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return ComplianceWorkflowAnalysisResultModel.fromJson(data);
    }

    String? errorCode;
    String? serverMessage;

    try {
      if (response.body.isNotEmpty) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        errorCode = data['errorCode']?.toString();
        serverMessage = data['message']?.toString();
      }
    } catch (_) {}

    if (response.statusCode == 401) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    if (response.statusCode == 403) {
      throw Exception(
        "You don't have permission to perform compliance analysis on this export request.",
      );
    }

    return ComplianceWorkflowAnalysisResultModel(
      success: false,
      message:
          serverMessage ??
          "The automated compliance service could not complete the analysis.",
      errorCode: errorCode ?? 'AI_PROVIDER_ERROR',
    );
  }

  // ==========================================
  // RETRY COMPLIANCE ANALYSIS
  // ==========================================
  Future<ComplianceWorkflowAnalysisResultModel> retryComplianceAnalysis(
    String requestId,
  ) async {
    final headers = await _getHeaders();

    final response = await http.post(
      Uri.parse(
        '${ApiConfig.exportRequests}/$requestId/compliance-analysis/retry',
      ),
      headers: headers,
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return ComplianceWorkflowAnalysisResultModel.fromJson(data);
    }

    String? errorCode;
    String? serverMessage;

    try {
      if (response.body.isNotEmpty) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        errorCode = data['errorCode']?.toString();
        serverMessage = data['message']?.toString();
      }
    } catch (_) {}

    if (response.statusCode == 401) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    if (response.statusCode == 403) {
      throw Exception(
        "You don't have permission to retry compliance analysis on this export request.",
      );
    }

    return ComplianceWorkflowAnalysisResultModel(
      success: false,
      message:
          serverMessage ??
          "AI assessment retry could not be completed at this time.",
      errorCode: errorCode ?? 'AI_PROVIDER_ERROR',
    );
  }

  // ==========================================
  // UPDATE EXPORT REQUEST
  // ==========================================
  Future<ExportRequestModel> updateExportRequest(
    String requestId, {
    required String originCountry,
    required String destinationCountry,
    required double declaredValue,
    String currency = 'USD',
    String? purpose,
  }) async {
    final headers = await _getHeaders();

    final bodyPayload = <String, dynamic>{
      'originCountry': originCountry.trim(),
      'destinationCountry': destinationCountry.trim(),
      'declaredValue': declaredValue,
      'currency': currency.trim(),
    };

    if (purpose != null && purpose.trim().isNotEmpty) {
      bodyPayload['purpose'] = purpose.trim();
    }

    final response = await http.put(
      Uri.parse('${ApiConfig.exportRequests}/$requestId'),
      headers: headers,
      body: jsonEncode(bodyPayload),
    );

    if (response.statusCode != 200) {
      if (response.statusCode == 401) {
        throw Exception('Your session has expired. Please sign in again.');
      }
      if (response.statusCode == 403) {
        throw Exception(
          "You don't have permission to update this export request.",
        );
      }
      if (response.statusCode == 404) {
        throw Exception("This export request could not be found.");
      }
      if (response.statusCode == 409) {
        throw Exception("This request can't be edited in its current state.");
      }

      try {
        if (response.body.isNotEmpty) {
          final data = jsonDecode(response.body) as Map<String, dynamic>;
          if (data.containsKey('message') && data['message'] != null) {
            throw Exception(data['message'].toString());
          }
        }
      } catch (e) {
        if (e is Exception && !e.toString().contains('FormatException')) {
          rethrow;
        }
      }

      throw Exception("We couldn't save your changes. Please try again.");
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final rawRequest = data['request'] as Map<String, dynamic>;

    return ExportRequestModel.fromJson(rawRequest);
  }

  // ==========================================
  // CREATE EXPORT REQUEST FROM ORDER (SELLER)
  // ==========================================
  Future<ExportRequestModel> createExportRequestFromOrder(
    dynamic orderId,
  ) async {
    final headers = await _getHeaders();

    final response = await http.post(
      Uri.parse(ApiConfig.exportRequestFromOrder(orderId)),
      headers: headers,
    );

    if (response.statusCode != 201) {
      throw Exception(
        _parseErrorResponse(
          response,
          "Failed to start export clearance for this order.",
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final reqJson = data['request'] as Map<String, dynamic>;
    return ExportRequestModel.fromJson(reqJson);
  }

  // ==========================================
  // GET EXPORT TRACKING FOR ORDER (BUYER-SAFE)
  // ==========================================
  Future<BuyerExportTrackingModel> getOrderExportTracking(
    dynamic orderId,
  ) async {
    final headers = await _getHeaders();

    final response = await _get(
      Uri.parse(ApiConfig.orderExportTracking(orderId)),
      headers,
    );

    if (response.statusCode == 404) {
      final orders = await _getTrackingFromOrders(headers);
      for (final order in orders) {
        if (order.orderId == orderId.toString()) return order;
      }
      throw Exception('This order could not be found in your account.');
    }

    if (response.statusCode != 200) {
      throw Exception(
        _trackingError(
          response,
          'Export tracking could not be loaded for this order.',
        ),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return BuyerExportTrackingModel.fromJson(data);
  }

  // ==========================================
  // GET ALL BUYER EXPORT TRACKING (BUYER-SAFE)
  // ==========================================
  Future<List<BuyerExportTrackingModel>> getMyExportTracking() async {
    final headers = await _getHeaders();

    final response = await _get(Uri.parse(ApiConfig.myExportTracking), headers);

    if (response.statusCode == 404) {
      return _getTrackingFromOrders(headers);
    }

    if (response.statusCode != 200) {
      throw Exception(
        _trackingError(response, 'Failed to load export tracking records.'),
      );
    }

    final data = jsonDecode(response.body);
    if (data is! List) {
      return [];
    }

    return data
        .whereType<Map<String, dynamic>>()
        .map(BuyerExportTrackingModel.fromJson)
        .toList();
  }

  String _trackingError(http.Response response, String fallback) {
    if (response.statusCode >= 500) {
      return 'Export tracking is temporarily unavailable. Please try again.';
    }
    return _parseErrorResponse(response, fallback);
  }

  Future<List<BuyerExportTrackingModel>> _getTrackingFromOrders(
    Map<String, String> headers,
  ) async {
    final response = await _get(Uri.parse(ApiConfig.myOrders), headers);
    if (response.statusCode == 404) {
      throw Exception(
        'Export tracking is not available on this server. Restart the updated Gemora API and try again.',
      );
    }
    if (response.statusCode != 200) {
      throw Exception(
        _trackingError(
          response,
          'Could not load your orders for export tracking.',
        ),
      );
    }

    final data = jsonDecode(response.body);
    if (data is! List) {
      throw Exception('The server returned an invalid order list.');
    }

    final orders = data.whereType<Map<String, dynamic>>().toList();
    if (orders.any((order) => order['exportStatus'] == null)) {
      throw Exception(
        'This server does not provide export statuses. Restart the updated Gemora API and try again.',
      );
    }

    return orders.map((order) {
      final status = order['exportStatus'].toString();
      final country = order['shippingCountryCode']?.toString() ?? '';
      final domestic =
          country.toUpperCase() == 'LK' || country.toLowerCase() == 'sri lanka';
      final description = switch (status.toLowerCase()) {
        'notrequired' =>
          'Domestic order — international export clearance not required.',
        'notstarted' =>
          'Export clearance has not yet been initiated by the seller.',
        'draft' => 'The seller is preparing export clearance.',
        _ => 'Export clearance status: $status.',
      };

      return BuyerExportTrackingModel(
        orderId: order['id']?.toString() ?? '',
        orderNumber: order['orderNumber']?.toString() ?? '',
        gemListingId: order['gemListingId'] as int?,
        gemTitle: order['gemTitle']?.toString() ?? 'Gemstone',
        gemImageUrl: order['gemImageUrl']?.toString(),
        exportRequestId: order['exportRequestId']?.toString(),
        status: status,
        stageDescription: description,
        originCountry: 'Sri Lanka',
        destinationCountry: domestic ? 'Sri Lanka' : country,
        updatedAt: DateTime.tryParse(
          order['updatedAt']?.toString() ??
              order['createdAt']?.toString() ??
              '',
        ),
        isApproved: status.toLowerCase() == 'approved',
        requiresExport: !domestic,
      );
    }).toList();
  }
}

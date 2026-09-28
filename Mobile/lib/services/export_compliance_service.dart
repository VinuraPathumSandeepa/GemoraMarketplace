import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

import '../config/api_config.dart';
import '../models/compliance_analysis_result_model.dart';
import '../models/compliance_document_model.dart';
import '../models/export_request_model.dart';
import 'token_storage_service.dart';

class ExportComplianceService {
  final TokenStorageService _tokenStorage = TokenStorageService();

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
        .map((item) => ExportRequestModel.fromJson(item as Map<String, dynamic>))
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
        .map((item) => ComplianceDocumentModel.fromJson(item as Map<String, dynamic>))
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

    final bodyPayload = <String, dynamic>{
      'documentType': documentType.trim(),
    };

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
      throw Exception('The selected file is too large. Maximum file size is 10 MB.');
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
        throw Exception("You don't have permission to submit this export request.");
      }
      if (response.statusCode == 404) {
        throw Exception("This export request could not be found.");
      }
      if (response.statusCode == 409) {
        throw Exception("This request can't be submitted in its current state.");
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

      throw Exception("We couldn't submit your export request. Please try again.");
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

    // Handle error codes safely
    String? errorCode;
    String? serverMessage;

    try {
      if (response.body.isNotEmpty) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        errorCode = data['errorCode']?.toString();
        serverMessage = data['message']?.toString();
      }
    } catch (_) {}

    if (errorCode == 'INVALID_EXPORT_STATUS' ||
        errorCode == 'WORKFLOW_ALREADY_ACTIVE' ||
        errorCode == 'WORKFLOW_STATE_INVALID' ||
        response.statusCode == 409) {
      throw Exception('The request state has changed. Refreshing the latest information.');
    }

    if (errorCode == 'AI_NOT_CONFIGURED') {
      throw Exception(
        'Automated compliance analysis is temporarily unavailable. Your request status has been refreshed.',
      );
    }
    if (errorCode == 'AI_TIMEOUT') {
      throw Exception(
        'The automated compliance analysis timed out. Your request status has been refreshed.',
      );
    }
    if (errorCode == 'AI_RATE_LIMITED') {
      throw Exception(
        'Automated compliance analysis is temporarily busy. Your request status has been refreshed.',
      );
    }
    if (errorCode == 'AI_PROVIDER_ERROR') {
      throw Exception(
        'The automated compliance service could not complete the analysis. Your request status has been refreshed.',
      );
    }
    if (errorCode == 'AI_INVALID_RESPONSE' ||
        errorCode == 'AI_VALIDATION_FAILED' ||
        errorCode == 'AI_WORKFLOW_VALIDATION_FAILED' ||
        errorCode == 'AI_ANALYSIS_FAILED') {
      throw Exception(
        'The automated assessment could not be completed safely. Your request status has been refreshed.',
      );
    }

    if (response.statusCode == 401) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    if (response.statusCode == 403) {
      throw Exception('You don\'t have permission to perform compliance analysis on this export request.');
    }
    if (response.statusCode == 404) {
      throw Exception('This export request could not be found.');
    }

    if (serverMessage != null && serverMessage.trim().isNotEmpty) {
      throw Exception(serverMessage);
    }

    throw Exception(
      'The automated compliance service could not complete the analysis. Your request status has been refreshed.',
    );
  }
}


import 'dart:convert';
import 'package:http/http.dart' as http;

import '../config/api_config.dart';
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
      return "You don't have permission to perform this action.";
    }
    if (response.statusCode == 404) {
      return "This export request could not be found.";
    }
    if (response.statusCode == 409) {
      return "This request can't be changed in its current state.";
    }
    if (response.statusCode >= 500) {
      return "We couldn't complete your request. Please try again.";
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
}

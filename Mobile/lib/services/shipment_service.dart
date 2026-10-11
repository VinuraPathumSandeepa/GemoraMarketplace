import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import 'token_storage_service.dart';

class ShipmentService {
  final http.Client _client;
  final Future<String?> Function() _token;
  ShipmentService({
    http.Client? client,
    Future<String?> Function()? tokenLoader,
  }) : _client = client ?? http.Client(),
       _token = tokenLoader ?? TokenStorageService().getToken;
  Future<dynamic> request(
    String path, {
    String method = 'GET',
    Map<String, dynamic>? body,
    bool optional = false,
  }) async {
    final token = await _token();
    if (token == null || token.trim().isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    final uri = Uri.parse('${ApiConfig.baseUrl}$path');
    final headers = {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };
    http.Response response;
    try {
      response =
          await (method == 'POST'
                  ? _client.post(
                      uri,
                      headers: headers,
                      body: body == null ? null : jsonEncode(body),
                    )
                  : _client.get(uri, headers: headers))
              .timeout(const Duration(seconds: 90));
    } on TimeoutException {
      throw Exception(
        'The request timed out. Refresh to check its status before trying again.',
      );
    } on http.ClientException {
      throw Exception(
        'Cannot reach Gemora. Check your connection and that the backend is running.',
      );
    }
    if (optional && response.statusCode == 404) return null;
    dynamic decoded;
    try {
      decoded = response.body.isEmpty ? null : jsonDecode(response.body);
    } catch (_) {
      decoded = null;
    }
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (decoded == null && response.statusCode != 204) {
        throw Exception('The server returned an invalid response.');
      }
      return decoded;
    }
    if (response.statusCode == 401) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    if (response.statusCode == 403) {
      throw Exception('You do not have permission to perform this action.');
    }
    if (decoded is Map) {
      final errors = decoded['errors'];
      if (errors is Map) {
        throw Exception(
          errors.entries
              .map(
                (e) =>
                    '${e.key}: ${e.value is List ? (e.value as List).join(' ') : e.value}',
              )
              .join('\n'),
        );
      }
      throw Exception(
        decoded['message'] ??
            decoded['title'] ??
            'Request failed (HTTP ${response.statusCode}).',
      );
    }
    throw Exception(
      'Request failed (HTTP ${response.statusCode}). Please try again.',
    );
  }

  Future<List<dynamic>> getMyShipments() async =>
      (await request('/shipments/my')) as List<dynamic>;
  Future<Map<String, dynamic>> getShipmentById(String id) async =>
      Map<String, dynamic>.from(await request('/shipments/$id'));
  Future<List<dynamic>> getTrackingEvents(String id) async =>
      (await request('/shipments/$id/tracking')) as List<dynamic>;
  Future<Map<String, dynamic>?> getInsuranceRecord(String id) async =>
      await request('/shipments/$id/insurance', optional: true);
  Future<Map<String, dynamic>?> getShippingPlan(String id) async =>
      await request('/shipments/$id/plan', optional: true);
  Future<Map<String, dynamic>> generateShippingPlan(String id) async =>
      await request('/shipments/$id/plan', method: 'POST');
  Future<List<dynamic>> getShipmentEligibleOrders() async =>
      (await request('/orders/my-shipment-eligible')) as List<dynamic>;
  Future<List<dynamic>> getSellerOrders() async =>
      (await request('/seller/orders')) as List<dynamic>;
  Future<Map<String, dynamic>> getSellerOrder(String id) async =>
      await request('/seller/orders/$id');
  Future<void> orderAction(
    String id,
    String action,
    Map<String, dynamic> body,
  ) async {
    await request('/seller/orders/$id/$action', method: 'POST', body: body);
  }

  Future<Map<String, dynamic>> createShipment({
    required String orderId,
    required String originAddress,
    required String originRegion,
    required String originCountryCode,
    required String destinationAddress,
    required String destinationRegion,
    required String destinationCountryCode,
    required String packageDescription,
    String? preferredService,
    String specialHandlingNotes = '',
    bool exportRequired = false,
    double? packageWeight,
    String? packageDimensions,
  }) async => await request(
    '/shipments',
    method: 'POST',
    body: {
      'orderId': orderId,
      'originAddress': originAddress.trim(),
      'originRegion': originRegion.trim(),
      'originCountryCode': originCountryCode.trim().toUpperCase(),
      'destinationAddress': destinationAddress.trim(),
      'destinationRegion': destinationRegion.trim(),
      'destinationCountryCode': destinationCountryCode.trim().toUpperCase(),
      'packageDescription': packageDescription.trim(),
      'preferredService': preferredService ?? 'Standard',
      'specialHandlingNotes': specialHandlingNotes.trim(),
      'exportRequired': exportRequired,
      'packageWeight': ?packageWeight,
      'packageDimensions': packageDimensions?.trim(),
    },
  );
  void dispose() => _client.close();
}

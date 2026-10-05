import 'dart:convert';
import 'package:http/http.dart' as http;
import '../config/api_config.dart';
import 'token_storage_service.dart';

class ShipmentService {
  final TokenStorageService _tokenStorage = TokenStorageService();

  Future<Map<String, String>> _getHeaders() async {
    final token = await _tokenStorage.getToken();
    return {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };
  }

  /// Get all shipments for the authenticated user
  Future<List<dynamic>> getMyShipments() async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/shipments/my'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      throw Exception('Failed to load shipments: ${response.body}');
    }
  }

  /// Get shipment by ID
  Future<Map<String, dynamic>> getShipmentById(String id) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/shipments/$id'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      throw Exception('Failed to load shipment: ${response.body}');
    }
  }

  /// Create a new shipment (Seller only)
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
  }) async {
    final headers = await _getHeaders();
    final body = jsonEncode({
      'orderId': orderId,
      'originAddress': originAddress,
      'originRegion': originRegion,
      'originCountryCode': originCountryCode,
      'destinationAddress': destinationAddress,
      'destinationRegion': destinationRegion,
      'destinationCountryCode': destinationCountryCode,
      'packageDescription': packageDescription,
      'preferredService': preferredService ?? 'Standard',
      'specialHandlingNotes': specialHandlingNotes,
      'exportRequired': exportRequired,
    });

    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/shipments'),
      headers: headers,
      body: body,
    );

    if (response.statusCode == 201) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      final error = jsonDecode(response.body);
      throw Exception(error['message'] ?? 'Failed to create shipment');
    }
  }

  /// Get tracking events for a shipment
  Future<List<dynamic>> getTrackingEvents(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/shipments/$shipmentId/tracking'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      throw Exception('Failed to load tracking events: ${response.body}');
    }
  }

  /// Get insurance record for a shipment (returns single object or null)
  Future<Map<String, dynamic>?> getInsuranceRecord(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/shipments/$shipmentId/insurance'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else if (response.statusCode == 404) {
      // No insurance record found - this is OK
      return null;
    } else {
      throw Exception('Failed to load insurance record: ${response.body}');
    }
  }

  /// Generate shipping plan
  Future<Map<String, dynamic>> generateShippingPlan(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/shipments/$shipmentId/plan'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      final error = jsonDecode(response.body);
      throw Exception(error['message'] ?? 'Failed to generate shipping plan');
    }
  }

  /// Get shipping plan
  Future<Map<String, dynamic>?> getShippingPlan(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/shipments/$shipmentId/plan'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else if (response.statusCode == 404) {
      // No plan generated yet - this is OK
      return null;
    } else {
      throw Exception('Failed to load shipping plan: ${response.body}');
    }
  }

  /// Get seller's eligible orders for shipment creation
  Future<List<dynamic>> getShipmentEligibleOrders() async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/orders/my-shipment-eligible'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else if (response.statusCode == 401) {
      throw Exception('Authentication required. Please login again.');
    } else if (response.statusCode == 403) {
      throw Exception('Only sellers can access eligible orders.');
    } else {
      throw Exception('Failed to load eligible orders: ${response.body}');
    }
  }
}

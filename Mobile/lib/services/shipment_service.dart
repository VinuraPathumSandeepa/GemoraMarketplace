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
      Uri.parse('${ApiConfig.baseUrl}/Shipment/my'),
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
      Uri.parse('${ApiConfig.baseUrl}/Shipment/$id'),
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
    required String origin,
    required String destination,
    required String packageDescription,
    required String selectedService,
    required String courierName,
  }) async {
    final headers = await _getHeaders();
    final body = jsonEncode({
      'orderId': orderId,
      'origin': origin,
      'destination': destination,
      'declaredValue': 0, // Will be derived from order on backend
      'currency': '', // Will be derived from order on backend
      'packageDescription': packageDescription,
      'selectedService': selectedService,
      'courierName': courierName,
    });

    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/Shipment'),
      headers: headers,
      body: body,
    );

    if (response.statusCode == 201) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      final error = jsonDecode(response.body);
      throw Exception(error['error'] ?? 'Failed to create shipment');
    }
  }

  /// Get tracking events for a shipment
  Future<List<dynamic>> getTrackingEvents(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/Shipment/$shipmentId/tracking'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      throw Exception('Failed to load tracking events: ${response.body}');
    }
  }

  /// Get insurance records for a shipment
  Future<List<dynamic>> getInsuranceRecords(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/Shipment/$shipmentId/insurance'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      throw Exception('Failed to load insurance records: ${response.body}');
    }
  }

  /// Generate shipping plan
  Future<Map<String, dynamic>> generateShippingPlan(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/Shipment/$shipmentId/plan'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      final error = jsonDecode(response.body);
      throw Exception(error['error'] ?? 'Failed to generate shipping plan');
    }
  }

  /// Get shipping plan
  Future<Map<String, dynamic>> getShippingPlan(String shipmentId) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/Shipment/$shipmentId/plan'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else {
      throw Exception('Failed to load shipping plan: ${response.body}');
    }
  }
}

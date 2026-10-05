import 'dart:convert';
import 'package:http/http.dart' as http;
import '../config/api_config.dart';
import '../models/marketplace/marketplace_gem.dart';
import '../models/marketplace/order_model.dart';
import 'token_storage_service.dart';

class MarketplaceService {
  final TokenStorageService _tokens = TokenStorageService();

  Future<Map<String, String>> _authHeaders() async {
    final token = await _tokens.getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<List<MarketplaceGem>> getGems({String search = ''}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/marketplace/gems').replace(
      queryParameters: {'search': search, 'page': '1', 'pageSize': '30'},
    );
    final response = await http.get(uri);
    if (response.statusCode != 200) throw Exception('Unable to load marketplace gems.');
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return (data['items'] as List).map((e) => MarketplaceGem.fromJson(e)).toList();
  }

  Future<MarketplaceGem> getGem(int id) async {
    final response = await http.get(Uri.parse('${ApiConfig.baseUrl}/marketplace/gems/$id'));
    if (response.statusCode != 200) throw Exception('Gem is unavailable.');
    return MarketplaceGem.fromJson(jsonDecode(response.body));
  }

  Future<OrderModel> createOrder(int gemListingId) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/orders'),
      headers: await _authHeaders(),
      body: jsonEncode({'gemListingId': gemListingId}),
    );
    if (response.statusCode != 201) throw Exception(_message(response.body, 'Unable to create order.'));
    return OrderModel.fromJson(jsonDecode(response.body));
  }

  Future<List<OrderModel>> getMyOrders() async {
    final response = await http.get(Uri.parse('${ApiConfig.baseUrl}/orders/my'), headers: await _authHeaders());
    if (response.statusCode != 200) throw Exception('Unable to load orders.');
    return (jsonDecode(response.body) as List).map((e) => OrderModel.fromJson(e)).toList();
  }

  Future<OrderModel> cancelOrder(int id) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/orders/$id/cancel'),
      headers: await _authHeaders(),
      body: jsonEncode({'reason': 'Cancelled by Buyer from mobile app.'}),
    );
    if (response.statusCode != 200) throw Exception(_message(response.body, 'Unable to cancel order.'));
    return OrderModel.fromJson(jsonDecode(response.body));
  }

  Future<Map<String, dynamic>> askAgent(String query) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/agent/assist'),
      headers: await _authHeaders(),
      body: jsonEncode({'query': query, 'maxRecommendations': 3}),
    );
    if (response.statusCode != 200) throw Exception(_message(response.body, 'Buyer assistant is unavailable.'));
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  String _message(String body, String fallback) {
    try { return (jsonDecode(body) as Map<String, dynamic>)['message'] ?? fallback; } catch (_) { return fallback; }
  }
}

import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/marketplace/marketplace_gem.dart';
import '../models/marketplace/order_model.dart';
import 'token_storage_service.dart';

class MarketplaceService {
  final TokenStorageService _tokenStorage = TokenStorageService();
  final http.Client? client;

  MarketplaceService({this.client});

  Future<Map<String, String>> _authHeaders({bool isJson = false}) async {
    final token = await _tokenStorage.getToken();

    final headers = <String, String>{
      'Accept': 'application/json',
      if (isJson) 'Content-Type': 'application/json',
      if (token != null && token.trim().isNotEmpty)
        'Authorization': 'Bearer ${token.trim()}',
    };

    return headers;
  }

  Future<List<MarketplaceGem>> getGems({
    String? search,
    String? gemType,
  }) async {
    final queryParameters = <String, String>{};

    final cleanSearch = search?.trim() ?? '';

    final cleanGemType = gemType?.trim() ?? '';

    if (cleanSearch.isNotEmpty) {
      queryParameters['search'] = cleanSearch;
    }

    if (cleanGemType.isNotEmpty && cleanGemType.toLowerCase() != 'all') {
      queryParameters['gemType'] = cleanGemType;
    }

    final uri = Uri.parse('${ApiConfig.baseUrl}/Marketplace/gems').replace(
      queryParameters: queryParameters.isEmpty ? null : queryParameters,
    );

    final httpClient = client;
    final response = httpClient != null
        ? await httpClient.get(
            uri,
            headers: const {'Accept': 'application/json'},
          )
        : await http.get(uri, headers: const {'Accept': 'application/json'});

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      final List<dynamic> items;
      if (decoded is List) {
        items = decoded;
      } else if (decoded is Map && decoded['items'] is List) {
        items = decoded['items'] as List<dynamic>;
      } else {
        throw Exception('The marketplace returned an unexpected response.');
      }

      return items
          .whereType<Map>()
          .map(
            (item) => MarketplaceGem.fromJson(Map<String, dynamic>.from(item)),
          )
          .toList();
    }

    throw Exception(
      _errorMessage(response, 'We could not load the gemstone marketplace.'),
    );
  }

  Future<MarketplaceGem> getGem(int id) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/Marketplace/gems/$id');
    final httpClient = client;
    final response = httpClient != null
        ? await httpClient.get(
            uri,
            headers: const {'Accept': 'application/json'},
          )
        : await http.get(uri, headers: const {'Accept': 'application/json'});

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map) {
        throw Exception(
          'The marketplace returned an unexpected gemstone response.',
        );
      }

      return MarketplaceGem.fromJson(Map<String, dynamic>.from(decoded));
    }

    throw Exception(
      _errorMessage(response, 'This gemstone is not currently available.'),
    );
  }

  Future<Map<String, dynamic>> askAgent(String message) async {
    final headers = await _authHeaders(isJson: true);

    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/agent/assist'),
      headers: headers,
      body: jsonEncode({'message': message.trim()}),
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        return decoded;
      }

      throw Exception(
        'The AI assistant returned an unexpected response format.',
      );
    }

    throw Exception(
      _errorMessage(response, 'Could not get response from AI assistant.'),
    );
  }

  Future<OrderModel> createOrder(int gemListingId) async {
    final headers = await _authHeaders(isJson: true);

    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/orders'),
      headers: headers,
      body: jsonEncode({'gemListingId': gemListingId}),
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        return OrderModel.fromJson(decoded);
      }

      throw Exception('Order creation returned an invalid response.');
    }

    throw Exception(_errorMessage(response, 'Could not create order.'));
  }

  Future<List<OrderModel>> getMyOrders() async {
    final headers = await _authHeaders();

    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/orders/my'),
      headers: headers,
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is List) {
        return decoded
            .whereType<Map<String, dynamic>>()
            .map(OrderModel.fromJson)
            .toList();
      }

      throw Exception('My orders returned an unexpected response.');
    }

    throw Exception(_errorMessage(response, 'Could not load your orders.'));
  }

  Future<List<OrderModel>> getSellerOrders() async {
    final headers = await _authHeaders();

    final response = await http.get(
      Uri.parse(ApiConfig.sellerOrders),
      headers: headers,
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is List) {
        return decoded
            .whereType<Map<String, dynamic>>()
            .map(OrderModel.fromJson)
            .toList();
      }

      throw Exception('Seller orders returned an unexpected response.');
    }

    throw Exception(_errorMessage(response, 'Could not load seller orders.'));
  }

  Future<void> cancelOrder(dynamic orderId) async {
    final headers = await _authHeaders(isJson: true);

    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/cancel'),
      headers: headers,
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      return;
    }

    throw Exception(_errorMessage(response, 'Could not cancel order.'));
  }

  String _errorMessage(http.Response response, String fallback) {
    try {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        final message = decoded['message']?.toString().trim();

        if (message != null && message.isNotEmpty) {
          return message;
        }
      }
    } catch (_) {}

    return fallback;
  }
}

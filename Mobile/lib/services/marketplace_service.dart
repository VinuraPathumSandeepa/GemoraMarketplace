import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/marketplace_gem_model.dart';
import '../models/marketplace/order_model.dart';
import 'token_storage_service.dart';

class MarketplaceService {
  final TokenStorageService _tokenStorage = TokenStorageService();

  Future<List<MarketplaceGemModel>> getGems({
    String? search,
    String? gemType,
  }) async {
    final queryParameters = <String, String>{
      'page': '1',
      'pageSize': '50',
    };

    final cleanSearch = search?.trim() ?? '';
    final cleanGemType = gemType?.trim() ?? '';

    if (cleanSearch.isNotEmpty) {
      queryParameters['search'] = cleanSearch;
    }

    if (cleanGemType.isNotEmpty && cleanGemType.toLowerCase() != 'all') {
      queryParameters['gemType'] = cleanGemType;
    }

    final uri = Uri.parse('${ApiConfig.baseUrl}/marketplace/gems').replace(
      queryParameters: queryParameters,
    );

    final response = await http.get(
      uri,
      headers: const {'Accept': 'application/json'},
    );

    _ensureSuccess(
      response,
      'We could not load the gemstone marketplace.',
    );

    final decoded = _decode(response.body);
    final items = decoded is Map<String, dynamic>
        ? decoded['items']
        : decoded;

    if (items is! List) {
      throw Exception('The marketplace returned an unexpected response.');
    }

    return items
        .whereType<Map<String, dynamic>>()
        .map(MarketplaceGemModel.fromJson)
        .toList();
  }

  Future<MarketplaceGemModel> getGem(int id) async {
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/gems/$id'),
      headers: const {'Accept': 'application/json'},
    );

    _ensureSuccess(response, 'This gemstone is not currently available.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception(
        'The marketplace returned an unexpected gemstone response.',
      );
    }

    return MarketplaceGemModel.fromJson(decoded);
  }

  Future<Map<String, dynamic>> askAgent(
    String message, {
    String? conversationId,
  }) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/agent/assist'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'message': message,
        if (conversationId != null) 'conversationId': conversationId,
      }),
    );

    _ensureSuccess(response, 'The buyer assistant is unavailable.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The buyer assistant returned an unexpected response.');
    }

    return decoded;
  }

  Future<List<OrderModel>> getMyOrders() async {
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/orders/my'),
      headers: await _authenticatedHeaders(),
    );

    _ensureSuccess(response, 'We could not load your orders.');

    final decoded = _decode(response.body);

    if (decoded is! List) {
      throw Exception('The orders service returned an unexpected response.');
    }

    return decoded
        .whereType<Map<String, dynamic>>()
        .map(OrderModel.fromJson)
        .toList();
  }

  Future<OrderModel> createOrder(
    int gemListingId, {
    Map<String, dynamic>? deliveryDetails,
  }) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/orders'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'gemListingId': gemListingId,
        if (deliveryDetails != null) 'deliveryDetails': deliveryDetails,
      }),
    );

    _ensureSuccess(response, 'We could not create the order.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The order service returned an unexpected response.');
    }

    return OrderModel.fromJson(decoded);
  }

  Future<OrderModel> cancelOrder(
    String orderId, {
    String reason = 'Cancelled by buyer.',
  }) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/cancel'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({'reason': reason}),
    );

    _ensureSuccess(response, 'We could not cancel this order.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The order service returned an unexpected response.');
    }

    return OrderModel.fromJson(decoded);
  }

  Future<Map<String, String>> _authenticatedHeaders() async {
    final token = await _tokenStorage.getToken();

    return {
      'Accept': 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };
  }

  dynamic _decode(String body) {
    try {
      return jsonDecode(body);
    } catch (_) {
      throw Exception('The server returned an invalid response.');
    }
  }

  void _ensureSuccess(http.Response response, String fallback) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return;
    }

    throw Exception(_errorMessage(response, fallback));
  }

  String _errorMessage(http.Response response, String fallback) {
    try {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        final message = (decoded['message'] ??
                decoded['detail'] ??
                decoded['title'])
            ?.toString()
            .trim();

        if (message != null && message.isNotEmpty) {
          return message;
        }
      }
    } catch (_) {
      // Use the endpoint-specific fallback for non-JSON errors.
    }

    return fallback;
  }
}

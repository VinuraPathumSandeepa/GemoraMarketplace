import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/marketplace_gem_model.dart';
import '../models/marketplace/marketplace_gem.dart';
import '../models/marketplace/order_model.dart';
import 'token_storage_service.dart';

class MarketplaceService {
  Future<List<MarketplaceGemModel>> getGems({
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

    final response = await http.get(
      uri,
      headers: const {'Accept': 'application/json'},
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is! List) {
        throw Exception('The marketplace returned an unexpected response.');
      }

      return decoded
          .whereType<Map<String, dynamic>>()
          .map(MarketplaceGemModel.fromJson)
          .toList();
    }

    throw Exception(
      _errorMessage(response, 'We could not load the gemstone marketplace.'),
    );
  }

  Future<MarketplaceGemModel> getGem(int id) async {
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/Marketplace/gems/$id'),
      headers: const {'Accept': 'application/json'},
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception(
          'The marketplace returned an unexpected gemstone response.',
        );
      }

      return MarketplaceGemModel.fromJson(decoded);
    }

    throw Exception(
      _errorMessage(response, 'This gemstone is not currently available.'),
    );
  }

  Future<List<MarketplaceGem>> getBuyerGems({String? search}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/Marketplace/gems').replace(
      queryParameters: search != null && search.trim().isNotEmpty
          ? {'search': search.trim()}
          : null,
    );
    final data = await _request(uri, authenticated: false);
    if (data is! List) throw Exception('Unexpected marketplace response.');
    return data
        .map((item) => MarketplaceGem.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<OrderModel> createOrder(int gemListingId) async {
    final data = await _request(
      Uri.parse('${ApiConfig.baseUrl}/orders'),
      body: {'gemListingId': gemListingId},
    );
    return OrderModel.fromJson(data as Map<String, dynamic>);
  }

  Future<List<OrderModel>> getMyOrders() async {
    final data = await _request(Uri.parse('${ApiConfig.baseUrl}/orders/my'));
    if (data is! List) throw Exception('Unexpected orders response.');
    return data
        .map((item) => OrderModel.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<void> cancelOrder(String orderId) async {
    await _request(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/cancel'),
      body: {},
    );
  }

  Future<Map<String, dynamic>> askAgent(String message) async {
    final data = await _request(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/agent/assist'),
      body: {'message': message},
    );
    return data as Map<String, dynamic>;
  }

  Future<dynamic> _request(
    Uri uri, {
    Map<String, dynamic>? body,
    bool authenticated = true,
  }) async {
    final headers = {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    };
    if (authenticated) {
      final token = await TokenStorageService().getToken();
      if (token == null || token.isEmpty) {
        throw Exception('Please sign in to continue.');
      }
      headers['Authorization'] = 'Bearer $token';
    }
    final response =
        await (body == null
                ? http.get(uri, headers: headers)
                : http.post(uri, headers: headers, body: jsonEncode(body)))
            .timeout(const Duration(seconds: 60));
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        _errorMessage(response, 'The request failed (${response.statusCode}).'),
      );
    }
    return response.body.isEmpty ? null : jsonDecode(response.body);
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

import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/marketplace_gem_model.dart';

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

import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/gem_listing_model.dart';
import 'token_storage_service.dart';

class AdminGemListingService {
  final TokenStorageService _tokens = TokenStorageService();

  Future<Map<String, String>> _headers() async {
    final token = await _tokens.getToken();
    if (token == null || token.isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }
    return {'Accept': 'application/json', 'Authorization': 'Bearer $token'};
  }

  Future<List<GemListingModel>> getAll() async {
    final response = await http.get(
      Uri.parse(ApiConfig.adminGemListings),
      headers: await _headers(),
    );
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('Could not load gem listings (${response.statusCode}).');
    }
    final data = jsonDecode(response.body);
    if (data is! List) throw Exception('Unexpected gem listings response.');
    return data
        .whereType<Map<String, dynamic>>()
        .map(GemListingModel.fromJson)
        .toList();
  }

  Future<void> delete(int id) async {
    final response = await http.delete(
      Uri.parse(ApiConfig.adminGemListing(id)),
      headers: await _headers(),
    );
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('Could not delete gem listing (${response.statusCode}).');
    }
  }
}

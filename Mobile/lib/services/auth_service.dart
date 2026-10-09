import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/user_model.dart';
import 'token_storage_service.dart';

class AuthService {
  final TokenStorageService _tokenStorage = TokenStorageService();

  // ==========================================
  // LOGIN
  // ==========================================

  Future<UserModel> login(String email, String password) async {
    final response = await http.post(
      Uri.parse(ApiConfig.login),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email.trim(), 'password': password}),
    );

    final responseData = _decodeResponse(response.body);

    if (response.statusCode != 200) {
      throw Exception(responseData['message'] ?? 'Login failed.');
    }

    final token = responseData['token'] as String?;

    if (token == null || token.isEmpty) {
      throw Exception('Authentication token was not returned.');
    }

    await _tokenStorage.saveToken(token);

    try {
      return await getCurrentUser();
    } catch (_) {
      await _tokenStorage.deleteToken();
      rethrow;
    }
  }

  // ==========================================
  // REGISTER
  // ==========================================

  Future<void> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String countryCode,
    required String region,
    required String password,
    required String role,
  }) async {
    final response = await http.post(
      Uri.parse(ApiConfig.register),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'fullName': fullName.trim(),
        'email': email.trim(),
        'phoneNumber': phoneNumber.trim(),
        'countryCode': countryCode.trim().toUpperCase(),
        'region': region.trim(),
        'password': password,
        'role': role,
      }),
    );

    final responseData = _decodeResponse(response.body);

    if (response.statusCode != 201) {
      throw Exception(responseData['message'] ?? 'Registration failed.');
    }
  }

  // ==========================================
  // VERIFY EMAIL OTP
  // ==========================================

  Future<void> verifyEmail({
    required String email,
    required String code,
  }) async {
    final response = await http.post(
      Uri.parse(ApiConfig.verifyEmail),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email.trim(), 'code': code.trim()}),
    );

    final responseData = _decodeResponse(response.body);

    if (response.statusCode != 200) {
      throw Exception(responseData['message'] ?? 'Email verification failed.');
    }
  }

  // ==========================================
  // RESEND VERIFICATION OTP
  // ==========================================

  Future<void> resendVerificationCode({required String email}) async {
    final response = await http.post(
      Uri.parse(ApiConfig.resendVerificationCode),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email.trim()}),
    );

    final responseData = _decodeResponse(response.body);

    if (response.statusCode != 200) {
      throw Exception(
        responseData['message'] ?? 'Unable to resend verification code.',
      );
    }
  }

  // ==========================================
  // GET CURRENT USER
  // ==========================================

  Future<UserModel> getCurrentUser() async {
    final token = await _tokenStorage.getToken();

    if (token == null || token.isEmpty) {
      throw Exception('Authentication token not found.');
    }

    final response = await http.get(
      Uri.parse(ApiConfig.currentUser),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode != 200) {
      throw Exception('Unable to load current user.');
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;

    return UserModel.fromJson(data);
  }

  // ==========================================
  // LOGOUT
  // ==========================================

  Future<void> logout() async {
    await _tokenStorage.deleteToken();
  }

  // ==========================================
  // CHECK SAVED TOKEN
  // ==========================================

  Future<bool> hasToken() async {
    return await _tokenStorage.hasToken();
  }

  // ==========================================
  // SAFE JSON RESPONSE
  // ==========================================

  Map<String, dynamic> _decodeResponse(String body) {
    if (body.isEmpty) {
      return {};
    }

    try {
      final decoded = jsonDecode(body);

      if (decoded is Map<String, dynamic>) {
        return decoded;
      }
    } catch (_) {
      // Use an empty response if the server did not return JSON.
    }

    return {};
  }
}

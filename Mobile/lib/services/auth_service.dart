import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/user_model.dart';
import 'token_storage_service.dart';

class AuthService {
  final http.Client _client;
  final TokenStorageService _tokenStorage = TokenStorageService();

  AuthService({http.Client? client}) : _client = client ?? http.Client();

  // ==========================================
  // LOGIN
  // ==========================================

  Future<UserModel> login(String email, String password) async {
<<<<<<< HEAD
    final response =
        await _post(
=======
    final response = await _client
        .post(
>>>>>>> origin/dev
          Uri.parse(ApiConfig.login),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({'email': email.trim(), 'password': password}),
        ).timeout(
          const Duration(seconds: 10),
          onTimeout: () {
            throw TimeoutException(
              'Connection timed out (10s). Unable to reach server at ${ApiConfig.baseUrl}.',
            );
          },
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
      final user = await getCurrentUser();
      return user;
    } catch (_) {
      await _tokenStorage.deleteToken();
      rethrow;
    }
  }

  // ==========================================
  // REGISTER
  // ==========================================

  Future<bool> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String countryCode,
    required String region,
    required String password,
    required String role,
  }) async {
<<<<<<< HEAD
    final response = await _post(
=======
    final response = await _client.post(
>>>>>>> origin/dev
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

    return responseData['verificationEmailSent'] != false;
  }

  // ==========================================
  // VERIFY EMAIL OTP
  // ==========================================

  Future<void> verifyEmail({
    required String email,
    required String code,
  }) async {
<<<<<<< HEAD
    final response = await _post(
=======
    final response = await _client.post(
>>>>>>> origin/dev
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
<<<<<<< HEAD
    final response = await _post(
=======
    final response = await _client.post(
>>>>>>> origin/dev
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

<<<<<<< HEAD
    final response =
        await _get(
=======
    final response = await _client
        .get(
>>>>>>> origin/dev
          Uri.parse(ApiConfig.currentUser),
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer $token',
          },
        ).timeout(
          const Duration(seconds: 10),
          onTimeout: () {
            throw TimeoutException(
              'Connection timed out (10s). Unable to load current user at ${ApiConfig.baseUrl}.',
            );
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

  Future<http.Response> _post(
    Uri uri, {
    required Map<String, String> headers,
    required String body,
  }) async {
    try {
      return await http.post(uri, headers: headers, body: body);
    } on http.ClientException {
      throw Exception(
        'Unable to connect to the Gemora API at ${ApiConfig.serverUrl}. '
        'Start the backend API and try again.',
      );
    }
  }

  Future<http.Response> _get(
    Uri uri, {
    required Map<String, String> headers,
  }) async {
    try {
      return await http.get(uri, headers: headers);
    } on http.ClientException {
      throw Exception(
        'Unable to connect to the Gemora API at ${ApiConfig.serverUrl}. '
        'Start the backend API and try again.',
      );
    }
  }
}

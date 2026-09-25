import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class TokenStorageService {
  static const FlutterSecureStorage _storage =
      FlutterSecureStorage();

  static const String _tokenKey = 'gemora_token';

  // Save JWT
  Future<void> saveToken(String token) async {
    await _storage.write(
      key: _tokenKey,
      value: token,
    );
  }

  // Read JWT
  Future<String?> getToken() async {
    return await _storage.read(
      key: _tokenKey,
    );
  }

  // Delete JWT during logout
  Future<void> deleteToken() async {
    await _storage.delete(
      key: _tokenKey,
    );
  }

  // Check whether a token exists
  Future<bool> hasToken() async {
    final token = await getToken();

    return token != null && token.isNotEmpty;
  }
}
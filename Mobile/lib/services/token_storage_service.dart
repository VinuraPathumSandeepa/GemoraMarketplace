import 'package:shared_preferences/shared_preferences.dart';

class TokenStorageService {
  static const String _tokenKey = 'auth_token';

  // Kept for compatibility with older builds that may have
  // saved the JWT using the simpler "token" key.
  static const String _legacyTokenKey = 'token';

  Future<void> saveToken(String token) async {
    final preferences = await SharedPreferences.getInstance();

    final cleanToken = token.trim();

    await preferences.setString(_tokenKey, cleanToken);

    await preferences.remove(_legacyTokenKey);
  }

  Future<String?> getToken() async {
    final preferences = await SharedPreferences.getInstance();

    final currentToken = preferences.getString(_tokenKey);

    if (currentToken != null && currentToken.trim().isNotEmpty) {
      return currentToken.trim();
    }

    final legacyToken = preferences.getString(_legacyTokenKey);

    if (legacyToken != null && legacyToken.trim().isNotEmpty) {
      return legacyToken.trim();
    }

    return null;
  }

  Future<bool> hasToken() async {
    final token = await getToken();

    return token != null && token.trim().isNotEmpty;
  }

  Future<void> clearToken() async {
    final preferences = await SharedPreferences.getInstance();

    await preferences.remove(_tokenKey);

    await preferences.remove(_legacyTokenKey);
  }

  Future<void> deleteToken() async {
    await clearToken();
  }
}

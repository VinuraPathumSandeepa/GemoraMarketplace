import 'package:flutter/material.dart';

import '../models/user_model.dart';
import '../services/auth_service.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService = AuthService();

  UserModel? _user;
  bool _isLoading = true;
  String? _errorMessage;

  // ==========================================
  // GETTERS
  // ==========================================

  UserModel? get user => _user;

  bool get isLoading => _isLoading;

  String? get errorMessage => _errorMessage;

  bool get isAuthenticated => _user != null;

  // ==========================================
  // INITIALIZE AUTHENTICATION
  // ==========================================

  Future<void> initialize() async {
    _isLoading = true;
    _errorMessage = null;

    notifyListeners();

    try {
      final hasToken = await _authService.hasToken();

      if (!hasToken) {
        _user = null;
        return;
      }

      _user = await _authService.getCurrentUser();
    } catch (_) {
      _user = null;

      await _authService.logout();
    } finally {
      _isLoading = false;

      notifyListeners();
    }
  }

  // ==========================================
  // LOGIN
  // ==========================================

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _errorMessage = null;

    notifyListeners();

    try {
      _user = await _authService.login(email.trim(), password);

      return true;
    } catch (error) {
      _user = null;
      _errorMessage = _cleanErrorMessage(error);

      return false;
    } finally {
      _isLoading = false;

      notifyListeners();
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
    _isLoading = true;
    _errorMessage = null;

    notifyListeners();

    try {
      await _authService.register(
        fullName: fullName.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        countryCode: countryCode.trim().toUpperCase(),
        region: region.trim(),
        password: password,
        role: role,
      );

      return true;
    } catch (error) {
      _errorMessage = _cleanErrorMessage(error);

      return false;
    } finally {
      _isLoading = false;

      notifyListeners();
    }
  }

  // ==========================================
  // VERIFY EMAIL
  // ==========================================

  Future<bool> verifyEmail({
    required String email,
    required String code,
  }) async {
    _isLoading = true;
    _errorMessage = null;

    notifyListeners();

    try {
      await _authService.verifyEmail(email: email.trim(), code: code.trim());

      return true;
    } catch (error) {
      _errorMessage = _cleanErrorMessage(error);

      return false;
    } finally {
      _isLoading = false;

      notifyListeners();
    }
  }

  // ==========================================
  // RESEND VERIFICATION CODE
  // ==========================================

  Future<bool> resendVerificationCode({required String email}) async {
    _isLoading = true;
    _errorMessage = null;

    notifyListeners();

    try {
      await _authService.resendVerificationCode(email: email.trim());

      return true;
    } catch (error) {
      _errorMessage = _cleanErrorMessage(error);

      return false;
    } finally {
      _isLoading = false;

      notifyListeners();
    }
  }

  // ==========================================
  // LOGOUT
  // ==========================================

  Future<void> logout() async {
    await _authService.logout();

    _user = null;
    _errorMessage = null;

    notifyListeners();
  }

  // ==========================================
  // CLEAR ERROR
  // ==========================================

  void clearError() {
    _errorMessage = null;

    notifyListeners();
  }

  // ==========================================
  // CLEAN ERROR MESSAGE
  // ==========================================

  String _cleanErrorMessage(Object error) {
    return error.toString().replaceFirst('Exception: ', '');
  }
}

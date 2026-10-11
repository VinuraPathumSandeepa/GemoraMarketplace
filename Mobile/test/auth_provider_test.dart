import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';

void main() {
  group('AuthProvider State Tests', () {
    test('initial state has no user and is not authenticated', () {
      final provider = AuthProvider();

      expect(provider.user, isNull);
      expect(provider.isAuthenticated, isFalse);
      expect(provider.errorMessage, isNull);
    });

    test('clearError resets error message state', () {
      final provider = AuthProvider();
      provider.clearError();

      expect(provider.errorMessage, isNull);
    });
  });
}

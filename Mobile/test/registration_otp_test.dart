import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/auth/verify_email_screen.dart';
import 'package:gemora_mobile/services/auth_service.dart';
import 'package:provider/provider.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

class _FakeAuthService extends AuthService {
  int resendCalls = 0;

  @override
  Future<bool> hasToken() async => false;

  @override
  Future<bool> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String countryCode,
    required String region,
    required String password,
    required String role,
  }) async => false;

  @override
  Future<void> resendVerificationCode({required String email}) async {
    resendCalls++;
  }
}

void main() {
  test('registration reads the server email delivery flag', () async {
    final client = MockClient((request) async {
      expect(request.url.path, '/api/Auth/register');
      return http.Response('{"verificationEmailSent":false}', 201);
    });
    final service = AuthService(client: client);
    final sent = await service.register(
      fullName: 'Buyer',
      email: 'buyer@example.com',
      phoneNumber: '+94771234567',
      countryCode: 'LK',
      region: 'Western',
      password: 'TestPassword123!',
      role: 'Buyer',
    );
    expect(sent, isFalse);
    client.close();
  });

  test('registration preserves the email delivery result', () async {
    final provider = AuthProvider(authService: _FakeAuthService());
    final registered = await provider.register(
      fullName: 'Buyer',
      email: 'buyer@example.com',
      phoneNumber: '+94771234567',
      countryCode: 'LK',
      region: 'Western',
      password: 'TestPassword123!',
      role: 'Buyer',
    );
    expect(registered, isTrue);
    expect(provider.registrationEmailSent, isFalse);
  });

  testWidgets('failed delivery offers immediate resend and clears warning', (
    tester,
  ) async {
    final service = _FakeAuthService();
    final provider = AuthProvider(authService: service);
    await provider.initialize();
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: provider,
        child: const MaterialApp(
          home: VerifyEmailScreen(
            email: 'buyer@example.com',
            initialEmailSent: false,
          ),
        ),
      ),
    );

    expect(find.textContaining('We could not send a code'), findsOneWidget);
    await tester.ensureVisible(find.text('Resend Code'));
    await tester.tap(find.text('Resend Code'));
    await tester.pumpAndSettle();
    expect(service.resendCalls, 1);
    expect(find.text('We sent a 6-digit verification code to'), findsOneWidget);
    expect(find.textContaining('We could not send a code'), findsNothing);
  });
}

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/auth/login_screen.dart';
import 'package:provider/provider.dart';

class TestAuthProvider extends AuthProvider {
  @override
  bool get isLoading => false;
}

void main() {
  testWidgets('LoginScreen renders header, inputs, and submit button',
      (WidgetTester tester) async {
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>(
        create: (_) => TestAuthProvider(),
        child: const MaterialApp(
          home: LoginScreen(),
        ),
      ),
    );

    expect(find.text('Gemora Marketplace'), findsOneWidget);
    expect(find.text('Sign in to your account'), findsOneWidget);

    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(find.text('Sign In'), findsOneWidget);

    await tester.enterText(
        find.byType(TextFormField).at(0), 'buyer@gemora.com');
    await tester.enterText(
        find.byType(TextFormField).at(1), 'SecurePass123!');

    expect(find.text('buyer@gemora.com'), findsOneWidget);
    expect(find.text('SecurePass123!'), findsOneWidget);
  });
}

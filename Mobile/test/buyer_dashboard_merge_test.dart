import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/main.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/dashboards/buyer_dashboard.dart';
import 'package:gemora_mobile/widgets/notification_bell.dart';
import 'package:provider/provider.dart';

class _LoadingAuthProvider extends AuthProvider {
  @override
  bool get isLoading => true;
}

void main() {
  testWidgets('buyer keeps all services in the simplified profile menu', (
    tester,
  ) async {
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: _LoadingAuthProvider(),
        child: const GemoraApp(),
      ),
    );

    final context = tester.element(find.byType(AuthGate));
    Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => const BuyerDashboard()));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 350));

    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Explore'), findsOneWidget);
    expect(find.text('Profile'), findsOneWidget);
    expect(find.text('Ask Gemora AI'), findsOneWidget);
    expect(find.byType(NotificationBell), findsOneWidget);

    await tester.tap(find.text('Profile'));
    await tester.pump();

    expect(find.text('Orders'), findsOneWidget);
    expect(find.text('Services'), findsOneWidget);
    expect(find.byType(NotificationBell), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

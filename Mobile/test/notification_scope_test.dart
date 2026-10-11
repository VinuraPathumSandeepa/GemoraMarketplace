import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/main.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/dashboards/seller_dashboard.dart';
import 'package:gemora_mobile/screens/notifications/notifications_screen.dart';
import 'package:gemora_mobile/widgets/notification_bell.dart';
import 'package:provider/provider.dart';

class _LoadingAuthProvider extends AuthProvider {
  @override
  bool get isLoading => true;
}

void main() {
  testWidgets('notification bell can read provider on a pushed dashboard', (
    tester,
  ) async {
    final auth = _LoadingAuthProvider();
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: auth,
        child: const GemoraApp(),
      ),
    );

    final context = tester.element(find.byType(AuthGate));
    Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => const SellerDashboard()));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 350));

    expect(find.byType(NotificationBell), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.tap(find.byType(NotificationBell));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 350));

    expect(find.byType(NotificationsScreen), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

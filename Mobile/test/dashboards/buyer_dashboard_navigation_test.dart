import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/dashboards/buyer_dashboard.dart';
import 'package:gemora_mobile/screens/dashboards/buyer_profile_screen.dart';
import 'package:gemora_mobile/screens/marketplace/marketplace_home_screen.dart';
import 'package:gemora_mobile/screens/marketplace/marketplace_screen.dart';
import 'package:provider/provider.dart';

class TestAuthProvider extends AuthProvider {
  @override
  bool get isLoading => false;
}

void main() {
  group('BuyerDashboard navigation', () {
    testWidgets('buyer marketplace removes the workspace destination', (
      tester,
    ) async {
      var profileOpenCount = 0;

      await tester.pumpWidget(
        MaterialApp(
          home: MarketplaceScreen(
            isAuthenticated: true,
            role: 'Buyer',
            onProfile: () => profileOpenCount++,
          ),
        ),
      );
      await tester.pump();

      expect(find.byType(NavigationDestination), findsNWidgets(3));
      expect(find.text('Home'), findsOneWidget);
      expect(find.text('Explore'), findsOneWidget);
      expect(find.text('Profile'), findsOneWidget);
      expect(find.text('Workspace'), findsNothing);

      await tester.tap(find.text('Profile'));
      await tester.pump();

      expect(profileOpenCount, 1);
    });

    testWidgets('uses a three-item buyer dock and dashboard AI pill', (
      tester,
    ) async {
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthProvider>(
          create: (_) => TestAuthProvider(),
          child: const MaterialApp(home: BuyerDashboard()),
        ),
      );
      await tester.pump(const Duration(milliseconds: 900));

      expect(find.byType(NavigationBar), findsOneWidget);
      expect(find.byType(NavigationDestination), findsNWidgets(3));
      expect(find.text('Home'), findsOneWidget);
      expect(find.text('Explore'), findsOneWidget);
      expect(find.text('Profile'), findsOneWidget);
      expect(find.text('Market'), findsNothing);
      expect(find.text('Assistant'), findsNothing);
      expect(find.text('Ask Gemora AI'), findsOneWidget);
    });

    testWidgets('Explore and Profile replace the former duplicate tabs', (
      tester,
    ) async {
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthProvider>(
          create: (_) => TestAuthProvider(),
          child: const MaterialApp(home: BuyerDashboard()),
        ),
      );
      await tester.pump(const Duration(milliseconds: 300));

      await tester.tap(find.text('Explore'));
      await tester.pump();
      expect(find.byType(MarketplaceHomeScreen), findsOneWidget);
      expect(find.text('Explore Gems'), findsOneWidget);

      await tester.tap(find.text('Profile'));
      await tester.pump();
      expect(find.byType(BuyerProfileScreen), findsOneWidget);
      expect(find.text('Your Gemora profile'), findsOneWidget);
      expect(find.text('Orders'), findsOneWidget);
      expect(find.text('Services'), findsOneWidget);
    });
  });
}

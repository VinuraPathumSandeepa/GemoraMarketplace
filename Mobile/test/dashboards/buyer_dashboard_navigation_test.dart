import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/buyer_export_tracking_model.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/dashboards/buyer_dashboard.dart';
import 'package:gemora_mobile/screens/marketplace/buyer_export_tracking_screen.dart';
import 'package:gemora_mobile/services/export_compliance_service.dart';
import 'package:provider/provider.dart';

class TestAuthProvider extends AuthProvider {
  @override
  bool get isLoading => false;
}

class FakeTrackingService extends ExportComplianceService {
  @override
  Future<List<BuyerExportTrackingModel>> getMyExportTracking() async {
    return [
      BuyerExportTrackingModel(
        orderId: 'order-test-1',
        orderNumber: 'GEM-EXPORT-777',
        gemTitle: 'Royal Ceylon Sapphire',
        status: 'Approved',
        stageDescription: 'Export clearance completed',
        originCountry: 'Sri Lanka',
        destinationCountry: 'Switzerland',
        isApproved: true,
        requiresExport: true,
      ),
    ];
  }
}

void main() {
  group('BuyerDashboard Bottom Navigation Tests', () {
    testWidgets(
      'renders 4 bottom navigation items and removes top airplane icon',
      (tester) async {
        final fakeService = FakeTrackingService();

        await tester.pumpWidget(
          ChangeNotifierProvider<AuthProvider>(
            create: (_) => TestAuthProvider(),
            child: MaterialApp(
              home: BuyerDashboard(exportComplianceService: fakeService),
            ),
          ),
        );
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 300));

        // Verify bottom navigation exists with exactly 4 items
        final navBarFinder = find.byType(NavigationBar);
        expect(navBarFinder, findsOneWidget);

        final destinationsFinder = find.byType(NavigationDestination);
        expect(destinationsFinder, findsNWidgets(4));

        expect(find.text('Market'), findsOneWidget);
        expect(find.text('Assistant'), findsOneWidget);
        expect(find.text('Orders'), findsOneWidget);
        expect(find.text('Exports'), findsOneWidget);

        // Verify top bar actions: Logout is present, airplane icon is NOT in AppBar
        final appBarFinder = find.byType(AppBar);
        expect(appBarFinder, findsOneWidget);
        expect(
          find.descendant(of: appBarFinder, matching: find.byIcon(Icons.logout)),
          findsOneWidget,
        );
        expect(
          find.descendant(
            of: appBarFinder,
            matching: find.byIcon(Icons.flight_takeoff),
          ),
          findsNothing,
        );

        // Verify airplane icon is used for Exports destination in bottom nav
        final exportsNavFinder = find.widgetWithText(
          NavigationDestination,
          'Exports',
        );
        expect(
          find.descendant(
            of: exportsNavFinder,
            matching: find.byIcon(Icons.flight_takeoff),
          ),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'tapping Exports navigates to existing Track My Exports view in-place',
      (tester) async {
        final fakeService = FakeTrackingService();

        await tester.pumpWidget(
          ChangeNotifierProvider<AuthProvider>(
            create: (_) => TestAuthProvider(),
            child: MaterialApp(
              home: BuyerDashboard(exportComplianceService: fakeService),
            ),
          ),
        );
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 300));

        // Initial state is Marketplace
        expect(find.text('Marketplace'), findsOneWidget);

        // Tap the Exports bottom navigation destination
        await tester.tap(find.text('Exports'));
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 300));

        // Header updates to Track My Exports
        expect(find.text('Track My Exports'), findsOneWidget);

        // Existing BuyerExportTrackingScreen is rendered without nested AppBar
        expect(find.byType(BuyerExportTrackingScreen), findsOneWidget);
        expect(find.byType(AppBar), findsOneWidget); // only outer AppBar, no duplicate

        // Reused tracking content is displayed
        expect(find.text('GEM-EXPORT-777'), findsOneWidget);
        expect(find.text('Royal Ceylon Sapphire'), findsOneWidget);
        expect(find.text('Sri Lanka → Switzerland'), findsOneWidget);

        // Bottom navigation remains visible with all 4 items
        expect(find.byType(NavigationDestination), findsNWidgets(4));

        // Switch back to Market
        await tester.tap(find.text('Market'));
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 300));
        expect(find.text('Marketplace'), findsOneWidget);
      },
    );
  });
}

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/compliance_document_model.dart';
import 'package:gemora_mobile/models/export_request_model.dart';
import 'package:gemora_mobile/models/marketplace/order_model.dart';
import 'package:gemora_mobile/models/user_model.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/export_compliance/create_export_request_screen.dart';
import 'package:gemora_mobile/screens/seller/seller_orders_screen.dart';
import 'package:gemora_mobile/services/export_compliance_service.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';
import 'package:provider/provider.dart';

class MockAuthProvider extends AuthProvider {
  final UserModel? _mockUser;
  MockAuthProvider(this._mockUser);

  @override
  UserModel? get user => _mockUser;
}

class FakeMarketplaceService extends MarketplaceService {
  List<OrderModel> ordersToReturn = [];

  @override
  Future<List<OrderModel>> getSellerOrders() async {
    return ordersToReturn;
  }
}

class FakeExportService extends ExportComplianceService {
  String? createdFromOrderId;

  @override
  Future<ExportRequestModel> createExportRequestFromOrder(
    dynamic orderId,
  ) async {
    createdFromOrderId = orderId.toString();
    return ExportRequestModel(
      id: 'req-from-order-1',
      originCountry: 'Sri Lanka',
      destinationCountry: 'United States',
      declaredValue: 3500.0,
      currency: 'USD',
      status: 'Draft',
      orderId: orderId.toString(),
      orderNumber: 'GEM-ORD-101',
      gemListingTitle: 'Star Ruby 5ct',
      createdAt: DateTime.now(),
      updatedAt: DateTime.now(),
    );
  }

  @override
  Future<ExportRequestModel> getExportRequestById(String id) async {
    return ExportRequestModel(
      id: id,
      originCountry: 'Sri Lanka',
      destinationCountry: 'United States',
      declaredValue: 3500.0,
      currency: 'USD',
      status: 'Draft',
      orderId: 'order-target-99',
      orderNumber: 'GEM-INT-099',
      gemListingTitle: 'Star Ruby 5ct',
      createdAt: DateTime.now(),
      updatedAt: DateTime.now(),
    );
  }

  @override
  Future<List<ComplianceDocumentModel>> getComplianceDocuments(
    String id,
  ) async {
    return [];
  }
}

void main() {
  final sellerUser = UserModel(
    id: 'seller-id-1',
    fullName: 'Test Seller',
    email: 'seller@gemora.com',
    role: 'Seller',
  );

  Widget createTestWidget(Widget child) {
    return ChangeNotifierProvider<AuthProvider>(
      create: (_) => MockAuthProvider(sellerUser),
      child: MaterialApp(home: child),
    );
  }

  group('Seller Order Export Clearance Flow Tests', () {
    testWidgets(
      '1. Domestic order shows "Export clearance not required" and no Start button',
      (tester) async {
        final fakeMarketplace = FakeMarketplaceService()
          ..ordersToReturn = [
            OrderModel(
              id: 'order-domestic-1',
              orderNumber: 'GEM-DOM-001',
              gemListingId: 1,
              gemTitle: 'Ceylon Yellow Sapphire 2ct',
              gemImageUrl: null,
              buyerName: 'Local Buyer',
              sellerName: 'Gem Seller',
              agreedPrice: 150000.0,
              currency: 'LKR',
              status: 'Confirmed',
              shippingCountryCode: 'LK',
              exportStatus: 'NotRequired',
              exportRequestId: null,
              createdAt: DateTime.now(),
              statusHistory: [],
            ),
          ];

        final fakeExport = FakeExportService();

        await tester.pumpWidget(
          createTestWidget(
            SellerOrdersScreen(
              marketplaceService: fakeMarketplace,
              exportService: fakeExport,
            ),
          ),
        );
        await tester.pumpAndSettle();

        expect(find.text('Export clearance not required'), findsOneWidget);
        expect(find.text('Start Export Clearance'), findsNothing);
        expect(find.text('Start Export Request'), findsNothing);
      },
    );

    testWidgets(
      '2. International order without request shows "Export clearance required" and Start button',
      (tester) async {
        final fakeMarketplace = FakeMarketplaceService()
          ..ordersToReturn = [
            OrderModel(
              id: 'order-intl-1',
              orderNumber: 'GEM-INT-001',
              gemListingId: 2,
              gemTitle: 'Royal Blue Sapphire 4ct',
              gemImageUrl: null,
              buyerName: 'US Buyer',
              sellerName: 'Gem Seller',
              agreedPrice: 5000.0,
              currency: 'USD',
              status: 'Confirmed',
              shippingCountryCode: 'US',
              exportStatus: 'NotStarted',
              exportRequestId: null,
              createdAt: DateTime.now(),
              statusHistory: [],
            ),
          ];

        final fakeExport = FakeExportService();

        await tester.pumpWidget(
          createTestWidget(
            SellerOrdersScreen(
              marketplaceService: fakeMarketplace,
              exportService: fakeExport,
            ),
          ),
        );
        await tester.pumpAndSettle();

        expect(find.text('Export clearance required'), findsOneWidget);
        expect(find.text('Start Export Clearance'), findsOneWidget);
        expect(find.text('View Export Clearance'), findsNothing);
      },
    );

    testWidgets(
      '3. International order with existing request shows "View Export Clearance" and no Start button',
      (tester) async {
        final fakeMarketplace = FakeMarketplaceService()
          ..ordersToReturn = [
            OrderModel(
              id: 'order-intl-2',
              orderNumber: 'GEM-INT-002',
              gemListingId: 3,
              gemTitle: 'Padparadscha Sapphire 3ct',
              gemImageUrl: null,
              buyerName: 'Swiss Buyer',
              sellerName: 'Gem Seller',
              agreedPrice: 12000.0,
              currency: 'USD',
              status: 'Confirmed',
              shippingCountryCode: 'CH',
              exportStatus: 'UnderComplianceReview',
              exportRequestId: 'req-existing-1',
              createdAt: DateTime.now(),
              statusHistory: [],
            ),
          ];

        final fakeExport = FakeExportService();

        await tester.pumpWidget(
          createTestWidget(
            SellerOrdersScreen(
              marketplaceService: fakeMarketplace,
              exportService: fakeExport,
            ),
          ),
        );
        await tester.pumpAndSettle();

        expect(find.text('View Export Clearance'), findsOneWidget);
        expect(find.text('Start Export Clearance'), findsNothing);
      },
    );

    testWidgets(
      '4. Pressing "Start Export Clearance" invokes from-order API with orderId',
      (tester) async {
        final fakeMarketplace = FakeMarketplaceService()
          ..ordersToReturn = [
            OrderModel(
              id: 'order-target-99',
              orderNumber: 'GEM-INT-099',
              gemListingId: 4,
              gemTitle: 'Star Ruby 5ct',
              gemImageUrl: null,
              buyerName: 'German Buyer',
              sellerName: 'Gem Seller',
              agreedPrice: 8500.0,
              currency: 'USD',
              status: 'Confirmed',
              shippingCountryCode: 'DE',
              exportStatus: 'NotStarted',
              exportRequestId: null,
              createdAt: DateTime.now(),
              statusHistory: [],
            ),
          ];

        final fakeExport = FakeExportService();

        await tester.pumpWidget(
          createTestWidget(
            SellerOrdersScreen(
              marketplaceService: fakeMarketplace,
              exportService: fakeExport,
            ),
          ),
        );
        await tester.pumpAndSettle();

        final startButton = find.text('Start Export Clearance');
        expect(startButton, findsOneWidget);

        await tester.tap(startButton);
        await tester.pump();
        expect(fakeExport.createdFromOrderId, 'order-target-99');
        await tester.pump(const Duration(milliseconds: 500));
      },
    );

    testWidgets(
      '5. Standalone CreateExportRequestScreen displays order-required notice',
      (tester) async {
        await tester.pumpWidget(
          createTestWidget(const CreateExportRequestScreen()),
        );
        await tester.pumpAndSettle();

        expect(
          find.text('Order Required for Export Clearance'),
          findsOneWidget,
        );
        expect(find.text('View Seller Orders'), findsOneWidget);
        expect(find.byType(DropdownButtonFormField<String>), findsNothing);
      },
    );
  });
}

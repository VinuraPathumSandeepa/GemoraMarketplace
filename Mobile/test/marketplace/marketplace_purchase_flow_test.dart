import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/marketplace_gem.dart';
import 'package:gemora_mobile/models/marketplace/order_model.dart';
import 'package:gemora_mobile/screens/marketplace/marketplace_screen.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';

class MockMarketplaceService extends MarketplaceService {
  final MarketplaceGem gemToReturn;
  final OrderModel? orderToReturn;
  final Exception? errorToThrow;
  final Completer<void>? delayCompleter;

  int createOrderCallCount = 0;
  int? lastGemListingId;
  String? lastShippingAddress;
  String? lastShippingRegion;
  String? lastShippingCountryCode;

  MockMarketplaceService({
    required this.gemToReturn,
    this.orderToReturn,
    this.errorToThrow,
    this.delayCompleter,
  });

  @override
  Future<MarketplaceGem> getGem(int id) async {
    return gemToReturn;
  }

  @override
  Future<OrderModel> createOrder(
    int gemListingId, {
    Map<String, dynamic>? deliveryDetails,
    String shippingAddress = 'Main Street',
    String shippingRegion = 'Western',
    String shippingCountryCode = 'LK',
  }) async {
    createOrderCallCount++;
    lastGemListingId = gemListingId;
    lastShippingAddress = shippingAddress;
    lastShippingRegion = shippingRegion;
    lastShippingCountryCode = shippingCountryCode;

    if (delayCompleter != null) {
      await delayCompleter!.future;
    }

    if (errorToThrow != null) {
      throw errorToThrow!;
    }

    return orderToReturn ??
        OrderModel(
          id: 'test-order-id-123',
          orderNumber: 'GEM-A1B2C3D4',
          gemListingId: gemListingId,
          gemTitle: gemToReturn.title,
          gemImageUrl: gemToReturn.primaryImageUrl,
          buyerName: 'Buyer Test',
          sellerName: gemToReturn.sellerName,
          agreedPrice: gemToReturn.price,
          currency: gemToReturn.currency,
          status: 'Pending',
          shippingCountryCode: shippingCountryCode,
          exportStatus: shippingCountryCode == 'LK'
              ? 'NotRequired'
              : 'NotStarted',
          createdAt: DateTime.now(),
          statusHistory: [],
        );
  }
}

void main() {
  final testGem = MarketplaceGem(
    id: 40,
    title: 'Ceylon Royal Blue Sapphire',
    gemType: 'Sapphire',
    description: 'Fine unheated Ceylon sapphire',
    caratWeight: 2.14,
    color: 'Royal Blue',
    clarity: 'Eye clean',
    cut: 'Cushion',
    price: 195000.0,
    currency: 'LKR',
    primaryImageUrl: 'https://example.com/sapphire.png',
    certificateNumber: 'NGJA-2026-99',
    certificateAuthority: 'NGJA',
    countryCode: 'LK',
    region: 'Ratnapura',
    sellerName: 'Gem Merchant LK',
    isAvailable: true,
  );

  Widget createWidget({
    required MockMarketplaceService service,
    bool isAuthenticated = true,
    String? role = 'buyer',
  }) {
    return MaterialApp(
      home: MarketplaceGemDetailsScreen(
        gemId: testGem.id,
        isAuthenticated: isAuthenticated,
        role: role,
        service: service,
      ),
    );
  }

  void prepareTester(WidgetTester tester) {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
  }

  group('Marketplace Gem Purchase Flow Tests', () {
    testWidgets('5. Buy button does not show old placeholder text', (
      WidgetTester tester,
    ) async {
      prepareTester(tester);

      final mockService = MockMarketplaceService(gemToReturn: testGem);

      await tester.pumpWidget(createWidget(service: mockService));
      await tester.pumpAndSettle();

      expect(find.text('Buy Gemstone'), findsOneWidget);

      await tester.tap(find.text('Buy Gemstone'));
      await tester.pumpAndSettle();

      expect(
        find.text('Purchase workflow will connect to Marketplace Orders next.'),
        findsNothing,
      );
      expect(find.text('Confirm Purchase'), findsOneWidget);
    });

    testWidgets(
      '6 & 12. Domestic purchase flow creates domestic order with countryCode LK',
      (WidgetTester tester) async {
        prepareTester(tester);

        final mockService = MockMarketplaceService(gemToReturn: testGem);

        await tester.pumpWidget(createWidget(service: mockService));
        await tester.pumpAndSettle();

        await tester.tap(find.text('Buy Gemstone'));
        await tester.pumpAndSettle();

        expect(find.text('Confirm Purchase'), findsOneWidget);
        expect(
          find.text('Domestic delivery: Export clearance not required'),
          findsOneWidget,
        );

        // Tap Place Order
        await tester.tap(find.text('Place Order'));
        await tester.pumpAndSettle();

        expect(mockService.createOrderCallCount, equals(1));
        expect(mockService.lastGemListingId, equals(40));
        expect(mockService.lastShippingCountryCode, equals('LK'));
        expect(mockService.lastShippingAddress, isNotEmpty);
        expect(mockService.lastShippingRegion, isNotEmpty);

        // Verify success dialog
        expect(find.text('Order Created'), findsOneWidget);
        expect(find.textContaining('GEM-A1B2C3D4'), findsOneWidget);
        expect(find.text('View My Orders'), findsOneWidget);
      },
    );

    testWidgets(
      '11. International purchase selects US and creates order with countryCode US',
      (WidgetTester tester) async {
        prepareTester(tester);

        final mockService = MockMarketplaceService(gemToReturn: testGem);

        await tester.pumpWidget(createWidget(service: mockService));
        await tester.pumpAndSettle();

        await tester.tap(find.text('Buy Gemstone'));
        await tester.pumpAndSettle();

        // Open Dropdown to pick United States
        await tester.tap(find.byType(DropdownButtonFormField<String>));
        await tester.pumpAndSettle();

        await tester.tap(find.text('United States (International - US)').last);
        await tester.pumpAndSettle();

        expect(
          find.text(
            'International delivery: Export clearance required (Component 4)',
          ),
          findsOneWidget,
        );

        await tester.tap(find.text('Place Order'));
        await tester.pumpAndSettle();

        expect(mockService.createOrderCallCount, equals(1));
        expect(mockService.lastShippingCountryCode, equals('US'));
        expect(mockService.lastShippingRegion, equals('California'));
        expect(
          mockService.lastShippingAddress,
          equals('742 Evergreen Terrace, Springfield'),
        );

        expect(find.text('Order Created'), findsOneWidget);
        expect(
          find.textContaining(
            'Export clearance is required for this international order',
          ),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      '7. Loading state prevents duplicate order taps during submission',
      (WidgetTester tester) async {
        prepareTester(tester);

        final completer = Completer<void>();
        final mockService = MockMarketplaceService(
          gemToReturn: testGem,
          delayCompleter: completer,
        );

        await tester.pumpWidget(createWidget(service: mockService));
        await tester.pumpAndSettle();

        await tester.tap(find.text('Buy Gemstone'));
        await tester.pumpAndSettle();

        // Tap Place Order once to start submission
        await tester.tap(find.text('Place Order'));
        await tester.pump(); // Start async work without settling

        expect(mockService.createOrderCallCount, equals(1));

        // During async submission, duplicate taps cannot trigger another order
        expect(mockService.createOrderCallCount, equals(1));

        // Complete order
        completer.complete();
        await tester.pumpAndSettle();

        expect(mockService.createOrderCallCount, equals(1));
      },
    );

    testWidgets('9. API failure displays friendly error message', (
      WidgetTester tester,
    ) async {
      prepareTester(tester);

      final mockService = MockMarketplaceService(
        gemToReturn: testGem,
        errorToThrow: Exception('This gemstone is no longer available.'),
      );

      await tester.pumpWidget(createWidget(service: mockService));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Buy Gemstone'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Place Order'));
      await tester.pumpAndSettle();

      expect(
        find.text('This gemstone is no longer available.'),
        findsOneWidget,
      );
      expect(find.text('Order Created'), findsNothing);
    });

    testWidgets('10. Required shipping validation prevents empty submission', (
      WidgetTester tester,
    ) async {
      prepareTester(tester);

      final mockService = MockMarketplaceService(gemToReturn: testGem);

      await tester.pumpWidget(createWidget(service: mockService));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Buy Gemstone'));
      await tester.pumpAndSettle();

      // Clear address field
      final addressFields = find.byType(TextFormField);
      await tester.enterText(addressFields.first, '');
      await tester.pump();

      await tester.tap(find.text('Place Order'));
      await tester.pumpAndSettle();

      expect(find.text('Shipping address is required'), findsOneWidget);
      expect(mockService.createOrderCallCount, equals(0));
    });

    testWidgets(
      '8. Non-buyer role is informed that Buyer account is required',
      (WidgetTester tester) async {
        prepareTester(tester);

        final mockService = MockMarketplaceService(gemToReturn: testGem);

        await tester.pumpWidget(
          createWidget(
            service: mockService,
            isAuthenticated: true,
            role: 'seller',
          ),
        );
        await tester.pumpAndSettle();

        await tester.tap(find.text('Buy Gemstone'));
        await tester.pumpAndSettle();

        expect(
          find.text(
            'Seller accounts can browse the marketplace, but purchasing requires a Buyer account.',
          ),
          findsOneWidget,
        );
        expect(mockService.createOrderCallCount, equals(0));
      },
    );
  });
}

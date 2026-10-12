import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/marketplace_gem.dart';
import 'package:gemora_mobile/models/marketplace/order_model.dart';
import 'package:gemora_mobile/screens/marketplace/gem_detail_screen.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';

class _OrderService extends MarketplaceService {
  int calls = 0;
  String? submittedAddress;

  @override
  Future<OrderModel> createOrder(
    int gemListingId, {
    Map<String, dynamic>? deliveryDetails,
    String shippingAddress = 'Main Street',
    String shippingRegion = 'Western',
    String shippingCountryCode = 'LK',
  }) async {
    calls++;
    submittedAddress = shippingAddress;
    return OrderModel(
      id: 'order-1',
      orderNumber: 'GEM-TEST123',
      gemListingId: gemListingId,
      gemTitle: 'Natural Blue Ruby',
      gemImageUrl: null,
      buyerName: 'Buyer',
      sellerName: 'Seller',
      agreedPrice: 500,
      currency: 'LKR',
      status: 'Pending',
      shippingCountryCode: shippingCountryCode,
      createdAt: DateTime(2026),
      statusHistory: const [],
    );
  }
}

MarketplaceGem _gem({required bool isAvailable}) => MarketplaceGem(
  id: 7,
  sellerName: 'Seller',
  title: 'Natural Blue Ruby',
  gemType: 'Ruby',
  description: 'Blue gemstone',
  caratWeight: 1,
  color: 'Blue',
  clarity: 'VS',
  cut: 'Oval',
  price: 500,
  currency: 'LKR',
  primaryImageUrl: null,
  certificateNumber: null,
  certificateAuthority: null,
  countryCode: 'LK',
  region: 'Ratnapura',
  isAvailable: isAvailable,
);

void main() {
  testWidgets('sold gemstone cannot be purchased again', (tester) async {
    final service = _OrderService();
    await tester.pumpWidget(
      MaterialApp(home: GemDetailScreen(gem: _gem(isAvailable: false), service: service)),
    );
    expect(find.text('Sold'), findsWidgets);
    expect(tester.widget<FilledButton>(find.byType(FilledButton)).onPressed, isNull);
    expect(service.calls, 0);
  });

  testWidgets('order success opens after delivery sheet closes', (tester) async {
    final service = _OrderService();
    await tester.pumpWidget(
      MaterialApp(home: GemDetailScreen(gem: _gem(isAvailable: true), service: service)),
    );
    await tester.tap(find.text('Purchase gemstone'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('Place Order'), findsOneWidget);
    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Buyer Test');
    await tester.enterText(fields.at(1), '0771234567');
    await tester.enterText(fields.at(2), '1 Main Street');
    await tester.enterText(fields.at(3), 'Colombo');
    await tester.enterText(fields.at(4), 'Colombo');
    await tester.enterText(fields.at(5), 'Western');
    await tester.enterText(fields.at(6), '00100');
    await tester.tap(find.text('Place Order'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(service.calls, 1);
    expect(service.submittedAddress, isNotEmpty);
    expect(find.text('Order created'), findsOneWidget);
    expect(find.textContaining('GEM-TEST123'), findsOneWidget);
  });
}

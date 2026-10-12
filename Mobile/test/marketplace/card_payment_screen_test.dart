import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/order_model.dart';
import 'package:gemora_mobile/screens/marketplace/card_payment_screen.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';

class _PaymentService extends MarketplaceService {
  String? confirmedToken;
  String? confirmedIntent;

  @override
  Future<Map<String, dynamic>> createPaymentIntent(String orderId) async => {
    'paymentIntentId': 'gemora_pi_test',
    'amount': 120000,
    'currency': 'LKR',
  };

  @override
  Future<OrderModel> confirmCardPayment({
    required String orderId,
    required String paymentIntentId,
    required String paymentMethodToken,
  }) async {
    confirmedIntent = paymentIntentId;
    confirmedToken = paymentMethodToken;
    return _order(status: 'Paid');
  }
}

OrderModel _order({String status = 'AwaitingPayment'}) => OrderModel(
  id: 'order-1',
  orderNumber: 'GEM-ABCD1234',
  gemListingId: 12,
  gemTitle: 'Royal Blue Sapphire',
  gemImageUrl: null,
  buyerName: 'Buyer',
  sellerName: 'Seller',
  agreedPrice: 200000,
  currency: 'LKR',
  status: status,
  createdAt: DateTime(2026, 10, 12),
  statusHistory: const [],
);

void main() {
  testWidgets('card form confirms payment without exposing sandbox language', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(500, 1300);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    final service = _PaymentService();

    await tester.pumpWidget(
      MaterialApp(
        home: CardPaymentScreen(order: _order(), service: service),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Credit card'), findsOneWidget);
    expect(find.text('Debit card'), findsOneWidget);
    expect(find.textContaining('sandbox', findRichText: true), findsNothing);

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Malindu Buyer');
    await tester.enterText(fields.at(1), '4242424242424242');
    await tester.enterText(fields.at(2), '1299');
    await tester.enterText(fields.at(3), '123');
    await tester.tap(find.textContaining('Pay LKR'));
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));

    expect(service.confirmedIntent, 'gemora_pi_test');
    expect(service.confirmedToken, 'demo_card_mobile_4242');
    expect(find.text('Payment approved'), findsOneWidget);
  });
}

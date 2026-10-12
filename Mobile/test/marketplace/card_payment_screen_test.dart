import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/order_model.dart';
import 'package:gemora_mobile/screens/marketplace/card_payment_screen.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';

class _PaymentService extends MarketplaceService {
  String? confirmedToken;
  String? confirmedIntent;
  Map<String, dynamic>? savedDelivery;
  bool failDeliverySave = false;
  int intentCalls = 0;

  @override
  Future<OrderModel> updateOrderDeliveryDetails(
    String orderId,
    Map<String, dynamic> deliveryDetails,
  ) async {
    if (failDeliverySave) throw Exception('Delivery could not be saved.');
    savedDelivery = deliveryDetails;
    return _order();
  }

  @override
  Future<Map<String, dynamic>> createPaymentIntent(String orderId) async {
    intentCalls++;
    return {
      'paymentIntentId': 'gemora_pi_test',
      'amount': 120000,
      'currency': 'LKR',
    };
  }

  @override
  Future<OrderModel> confirmCardPayment({
    required String orderId,
    required String paymentIntentId,
    required String paymentMethodToken,
  }) async {
    confirmedIntent = paymentIntentId;
    confirmedToken = paymentMethodToken;
    if (paymentMethodToken.endsWith('_decline')) {
      throw Exception('The card issuer declined this payment.');
    }
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
  testWidgets('delivery is saved before the card payment starts', (
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

    expect(find.text('Credit card'), findsNothing);

    for (final entry in <int, String>{
      1: '0771234567',
      3: '12 Main Street',
      5: 'Colombo',
      6: 'Colombo',
      7: 'Western',
      8: '00100',
    }.entries) {
      await tester.enterText(
        find.byType(TextFormField).at(entry.key),
        entry.value,
      );
    }
    await tester.ensureVisible(find.text('Save and continue to card payment'));
    await tester.tap(find.text('Save and continue to card payment'));
    await tester.pumpAndSettle();

    expect(service.savedDelivery?['recipientPhone'], '0771234567');
    expect(service.savedDelivery?['addressLine1'], '12 Main Street');
    expect(find.text('Credit card'), findsOneWidget);
    expect(find.text('Debit card'), findsOneWidget);
    expect(find.textContaining('12 Main Street'), findsOneWidget);
    expect(service.intentCalls, 0);

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Malindu Buyer');
    await tester.enterText(fields.at(1), '4242424242424242');
    await tester.enterText(fields.at(2), '1299');
    await tester.enterText(fields.at(3), '123');
    await tester.ensureVisible(find.textContaining('Pay LKR'));
    await tester.tap(find.textContaining('Pay LKR'));
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));

    expect(service.confirmedIntent, 'gemora_pi_test');
    expect(service.confirmedToken, 'demo_card_mobile_4242');
    expect(find.text('Demo payment approved'), findsOneWidget);
  });

  testWidgets('declined demo payment stays on the card step for retry', (
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

    for (final entry in <int, String>{
      1: '0771234567',
      3: '12 Main Street',
      5: 'Colombo',
      6: 'Colombo',
      7: 'Western',
      8: '00100',
    }.entries) {
      await tester.enterText(
        find.byType(TextFormField).at(entry.key),
        entry.value,
      );
    }
    await tester.ensureVisible(find.text('Save and continue to card payment'));
    await tester.tap(find.text('Save and continue to card payment'));
    await tester.pumpAndSettle();

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Malindu Buyer');
    await tester.enterText(fields.at(1), '4000000000000002');
    await tester.enterText(fields.at(2), '1299');
    await tester.enterText(fields.at(3), '123');
    await tester.ensureVisible(find.textContaining('Pay LKR'));
    await tester.tap(find.textContaining('Pay LKR'));
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));

    expect(service.confirmedToken, 'demo_card_mobile_decline');
    expect(find.textContaining('Payment failed:'), findsOneWidget);
    expect(find.text('Demo payment approved'), findsNothing);
    expect(find.text('Edit delivery details'), findsOneWidget);

    await tester.enterText(fields.at(1), '4242424242424242');
    await tester.ensureVisible(find.textContaining('Pay LKR'));
    await tester.tap(find.textContaining('Pay LKR'));
    await tester.pumpAndSettle();
    expect(service.intentCalls, 2);
    expect(find.text('Demo payment approved'), findsOneWidget);
    expect(find.textContaining('Delivery is pending'), findsOneWidget);
  });

  testWidgets('missing delivery details cannot open card payment', (tester) async {
    final service = _PaymentService();
    await tester.pumpWidget(MaterialApp(
      home: CardPaymentScreen(order: _order(), service: service),
    ));
    await tester.ensureVisible(find.text('Save and continue to card payment'));
    await tester.tap(find.text('Save and continue to card payment'));
    await tester.pumpAndSettle();
    expect(find.text('Street address is required'), findsOneWidget);
    expect(find.text('Credit card'), findsNothing);
    expect(service.savedDelivery, isNull);
    expect(service.intentCalls, 0);
  });

  testWidgets('save failure keeps delivery details for retry and editing', (tester) async {
    tester.view.physicalSize = const Size(500, 1300);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    final service = _PaymentService()..failDeliverySave = true;
    await tester.pumpWidget(MaterialApp(
      home: CardPaymentScreen(order: _order(), service: service),
    ));
    for (final entry in <int, String>{
      1: '0771234567', 3: '12 Main Street', 5: 'Colombo',
      6: 'Colombo', 7: 'Western', 8: '00100', 9: 'lk',
    }.entries) {
      await tester.enterText(find.byType(TextFormField).at(entry.key), entry.value);
    }
    await tester.ensureVisible(find.text('Save and continue to card payment'));
    await tester.tap(find.text('Save and continue to card payment'));
    await tester.pumpAndSettle();
    expect(find.text('Delivery could not be saved.'), findsOneWidget);
    expect(find.text('Credit card'), findsNothing);
    expect(service.intentCalls, 0);

    service.failDeliverySave = false;
    await tester.tap(find.text('Save and continue to card payment'));
    await tester.pumpAndSettle();
    expect(service.savedDelivery?['countryCode'], 'LK');
    await tester.ensureVisible(find.text('Edit delivery details'));
    await tester.tap(find.text('Edit delivery details'));
    await tester.pumpAndSettle();
    expect(find.text('12 Main Street'), findsOneWidget);
    expect(find.text('Credit card'), findsNothing);
  });
}

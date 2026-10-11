import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/order_model.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

Map<String, dynamic> orderJson({
  String status = 'Pending',
  String fulfillmentStatus = 'Pending',
}) => {
  'id': '42b1bb58-4f47-4db8-9648-ea663ea64fea',
  'orderNumber': 'GEM-42B1BB58',
  'gemListingId': 14,
  'gemTitle': 'Royal Blue Sapphire',
  'buyerName': 'Buyer',
  'sellerName': 'Seller',
  'agreedPrice': 850000,
  'currency': 'LKR',
  'status': status,
  'fulfillmentStatus': fulfillmentStatus,
  'shippingCountryCode': 'LK',
  'createdAt': '2026-10-12T04:00:00Z',
  'statusHistory': <dynamic>[],
  'fulfillmentHistory': <dynamic>[],
};

void main() {
  test(
    'order model parses payment, fulfillment and shipment lifecycle data',
    () {
      final json = orderJson(status: 'Paid', fulfillmentStatus: 'InTransit')
        ..addAll({
          'paymentDueAt': '2026-10-12T07:00:00Z',
          'paidAt': '2026-10-12T05:00:00Z',
          'shipment': {
            'id': 'shipment-1',
            'courierName': 'Gemora Rider',
            'trackingNumber': 'GM-100',
            'status': 'InTransit',
          },
          'fulfillmentHistory': [
            {
              'newStatus': 'InTransit',
              'changedByName': 'Seller',
              'note': 'Rider collected the package.',
              'createdAt': '2026-10-12T06:00:00Z',
            },
          ],
        });

      final order = OrderModel.fromJson(json);

      expect(order.status, 'Paid');
      expect(order.fulfillmentStatus, 'InTransit');
      expect(order.paidAt, isNotNull);
      expect(order.shipment?.trackingNumber, 'GM-100');
      expect(
        order.fulfillmentHistory.single.note,
        'Rider collected the package.',
      );
    },
  );

  test('seller approval uses the existing seller order endpoint', () async {
    late http.Request captured;
    final service = MarketplaceService(
      tokenLoader: () async => 'test-token',
      client: MockClient((request) async {
        captured = request;
        return http.Response(
          jsonEncode(orderJson(status: 'Confirmed')),
          200,
          headers: {'content-type': 'application/json'},
        );
      }),
    );

    final result = await service.approveOrder(
      '42b1bb58-4f47-4db8-9648-ea663ea64fea',
    );

    expect(captured.method, 'POST');
    expect(
      captured.url.path,
      endsWith(
        '/api/seller/orders/42b1bb58-4f47-4db8-9648-ea663ea64fea/approve',
      ),
    );
    expect(result.status, 'Confirmed');
  });

  test(
    'order creation sends the verified structured delivery snapshot',
    () async {
      late Map<String, dynamic> payload;
      final service = MarketplaceService(
        tokenLoader: () async => 'test-token',
        client: MockClient((request) async {
          payload = jsonDecode(request.body) as Map<String, dynamic>;
          return http.Response(jsonEncode(orderJson()), 201);
        }),
      );

      await service.createOrder(
        14,
        shippingAddress: '120 Galle Road',
        shippingRegion: 'Western Province',
        shippingCountryCode: 'LK',
        deliveryDetails: {
          'recipientName': 'Buyer Test',
          'recipientPhone': '+94771234567',
          'addressLine1': '120 Galle Road',
          'city': 'Colombo',
          'district': 'Colombo',
          'region': 'Western Province',
          'postalCode': '00300',
          'countryCode': 'LK',
        },
      );

      final delivery = payload['deliveryDetails'] as Map<String, dynamic>;
      expect(delivery['recipientName'], 'Buyer Test');
      expect(delivery['recipientPhone'], '+94771234567');
      expect(delivery['postalCode'], '00300');
    },
  );

  test('buyer payment creates an intent then confirms it', () async {
    final paths = <String>[];
    final service = MarketplaceService(
      tokenLoader: () async => 'test-token',
      client: MockClient((request) async {
        paths.add(request.url.path);
        if (request.url.path.endsWith('/payment/intent')) {
          return http.Response(
            jsonEncode({
              'paymentIntentId': 'gemora_pi_mobile',
              'provider': 'GemoraSandbox',
              'orderTotalAmount': 850000,
              'advancePercentage': 10,
              'amount': 85000,
              'remainingAmount': 765000,
              'currency': 'LKR',
              'expiresAt': '2026-10-12T07:00:00Z',
            }),
            200,
          );
        }
        return http.Response(
          jsonEncode({'order': orderJson(status: 'Paid')}),
          200,
        );
      }),
    );

    final intent = await service.createPaymentIntent(
      '42b1bb58-4f47-4db8-9648-ea663ea64fea',
    );
    final paid = await service.confirmPayment(
      '42b1bb58-4f47-4db8-9648-ea663ea64fea',
      paymentIntentId: intent.paymentIntentId,
      paymentMethodToken: 'demo_card_mobile_test',
    );

    expect(intent.amount, 85000);
    expect(paid.status, 'Paid');
    expect(paths.last, endsWith('/payment/confirm'));
  });
}

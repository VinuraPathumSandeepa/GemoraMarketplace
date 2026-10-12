import 'dart:async';

import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/dashboards/seller_dashboard.dart';

import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:gemora_mobile/models/shipment_model.dart';
import 'package:gemora_mobile/services/shipment_service.dart';
import 'package:gemora_mobile/screens/shipments/seller_shipment_list.dart';
import 'package:gemora_mobile/screens/shipments/create_shipment_screen.dart';
import 'package:gemora_mobile/screens/shipments/seller_order_inbox.dart';
import 'package:gemora_mobile/screens/shipments/shipment_detail_screen.dart';

ShipmentService service(Future<http.Response> Function(http.Request) handler) =>
    ShipmentService(
      client: MockClient(handler),
      tokenLoader: () async => 'test-token',
    );
void main() {
  test(
    'Shipment parses numeric strings, nullable fields and unknown statuses',
    () {
      final s = ShipmentModel.fromJson({
        'id': 'a',
        'declaredValue': '145000.50',
        'status': 'CustomsHold',
        'originAddress': 'Colombo',
        'originCountryCode': 'LK',
        'createdAt': '2026-10-10T11:00:00',
      });
      expect(s.value, 145000.50);
      expect(s.status, 'CustomsHold');
      expect(s.route('origin'), 'Colombo, LK');
      expect(s.createdAt, isNotNull);
      expect(s.currency, '');
      expect(ShipmentModel.parseDate('bad date'), isNull);
    },
  );
  test('Paid order amount accepts eligible and seller API contracts', () {
    expect(
      ShipmentOrder.fromJson({'totalAmount': 100, 'currency': 'LKR'}).amount,
      100,
    );
    expect(ShipmentOrder.fromJson({'agreedPrice': '200.5'}).amount, 200.5);
    expect(
      ShipmentOrder.fromJson({'status': 'Pending', 'sellerReadAt': null})
          .unread,
      isTrue,
    );
  });
  test('Country, required text and optional package weight validation', () {
    expect(ShipmentValidation.country(' lk '), isNull);
    expect(ShipmentValidation.country('LKA'), isNotNull);
    expect(ShipmentValidation.requiredText('  '), isNotNull);
    expect(ShipmentValidation.requiredText('abc', max: 2), isNotNull);
    for (final weight in ['0', '-2', 'abc', 'NaN', 'Infinity']) {
      expect(ShipmentValidation.weight(weight), isNotNull);
    }
    expect(ShipmentValidation.weight(''), isNull);
    expect(ShipmentValidation.weight('0.5'), isNull);
  });
  test('Create uses paid-order contract, package data and auth; financial fields omitted', () async {
    final api = service((r) async {
      expect(r.method, 'POST');
      expect(r.url.path, endsWith('/shipments'));
      expect(r.headers['Authorization'], 'Bearer test-token');
      final body = jsonDecode(r.body) as Map;
      expect(body['originCountryCode'], 'LK');
      expect(body['packageWeight'], 1.5);
      expect(body['packageDimensions'], '10 x 10 x 5');
      expect(body.containsKey('declaredValue'), isFalse);
      expect(body.containsKey('currency'), isFalse);
      return http.Response('{"id":"shipment-1"}', 201);
    });
    final created = await api.createShipment(
      orderId: 'paid-order',
      originAddress: 'Colombo',
      originRegion: 'Western',
      originCountryCode: 'lk',
      destinationAddress: 'Galle',
      destinationRegion: 'Southern',
      destinationCountryCode: 'LK',
      packageDescription: 'Gemstone',
      packageWeight: 1.5,
      packageDimensions: '10 x 10 x 5',
    );
    expect(created['id'], 'shipment-1');
    api.dispose();
  });
  test('Missing optional records only treat 404 as absent', () async {
    final absent = service((r) async => http.Response('{}', 404));
    expect(await absent.getShippingPlan('id'), isNull);
    absent.dispose();
    final forbidden = service((r) async => http.Response('{}', 403));
    await expectLater(
      forbidden.getInsuranceRecord('id'),
      throwsA(predicate((e) => e.toString().contains('permission'))),
    );
    forbidden.dispose();
  });
  test(
    'Unauthorized and server validation messages remain actionable',
    () async {
      final unauthorized = service((r) async => http.Response('{}', 401));
      await expectLater(
        unauthorized.getMyShipments(),
        throwsA(predicate((e) => e.toString().contains('sign in'))),
      );
      unauthorized.dispose();
      final invalid = service(
        (r) async => http.Response(
          '{"errors":{"OrderId":["Order already has a shipment."]}}',
          400,
        ),
      );
      await expectLater(
        invalid.getMyShipments(),
        throwsA(
          predicate(
            (e) => e.toString().contains('Order already has a shipment'),
          ),
        ),
      );
      invalid.dispose();
    },
  );
  test(
    'Order actions use existing seller endpoints and rejection body',
    () async {
      final api = service((r) async {
        expect(r.url.path, endsWith('/seller/orders/order-1/reject'));
        expect(jsonDecode(r.body), {
          'reason': 'Unavailable',
          'alreadySold': true,
        });
        return http.Response('{}', 200);
      });
      await api.orderAction('order-1', 'reject', {
        'reason': 'Unavailable',
        'alreadySold': true,
      });
      api.dispose();
    },
  );
  testWidgets('Shipment list loading and empty state fit a narrow screen', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(320, 700);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final pending = Completer<http.Response>();
    final api = service((r) => pending.future);
    await tester.pumpWidget(
      MaterialApp(home: SellerShipmentList(service: api)),
    );
    await tester.pump();
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    pending.complete(http.Response('[]', 200));
    await tester.pumpAndSettle();
    expect(find.textContaining('No shipments yet'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    api.dispose();
  });
  testWidgets('Shipment error offers retry and recovers', (tester) async {
    var count = 0;
    final api = service(
      (r) async =>
          ++count == 1 ? http.Response('{}', 401) : http.Response('[]', 200),
    );
    await tester.pumpWidget(
      MaterialApp(home: SellerShipmentList(service: api)),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('session has expired'), findsOneWidget);
    await tester.ensureVisible(find.text('Retry'));
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.textContaining('No shipments yet'), findsOneWidget);
    expect(count, 2);
    await tester.pumpWidget(const SizedBox());
    api.dispose();
  });
  testWidgets('Paid-order selection autofills destination and order value', (
    tester,
  ) async {
    final api = service(
      (r) async => http.Response(
        jsonEncode([
          {
            'id': 'order-1',
            'gemTitle': 'Blue Sapphire',
            'shippingAddress': 'Buyer address',
            'shippingRegion': 'Western',
            'shippingCountryCode': 'LK',
            'totalAmount': 145000,
            'currency': 'LKR',
          },
        ]),
        200,
      ),
    );
    await tester.pumpWidget(
      MaterialApp(home: CreateShipmentScreen(service: api)),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.byType(DropdownButtonFormField<String>).first);
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Blue Sapphire').last);
    await tester.pumpAndSettle();
    expect(find.text('LKR 145000.00'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('Destination Address'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.pumpAndSettle();
    final fields = tester
        .widgetList<TextFormField>(find.byType(TextFormField))
        .toList();
    expect(fields.any((f) => f.controller?.text == 'Buyer address'), isTrue);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    api.dispose();
  });
  testWidgets('Inbox shows unread pending requests and can show all orders', (
    tester,
  ) async {
    final api = service(
      (r) async => http.Response(
        jsonEncode([
          {
            'id': 'a',
            'gemTitle': 'Pending gem',
            'status': 'Pending',
            'sellerReadAt': null,
          },
          {'id': 'b', 'gemTitle': 'Paid gem', 'status': 'Paid'},
        ]),
        200,
      ),
    );
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: SellerOrderInbox(service: api)),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('1 new orders'), findsOneWidget);
    expect(find.text('Pending gem'), findsOneWidget);
    expect(find.text('Paid gem'), findsNothing);
    await tester.tap(find.text('Show all orders'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Paid gem'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Paid gem'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    api.dispose();
  });
  testWidgets('Tracking failure does not hide saved shipment details', (
    tester,
  ) async {
    final api = service((r) async {
      if (r.url.path.endsWith('/tracking')) return http.Response('{}', 500);
      if (r.url.path.endsWith('/plan') || r.url.path.endsWith('/insurance')) {
        return http.Response('{}', 404);
      }
      return http.Response(
        '{"id":"shipment-1","orderId":"o","status":"InTransit","currency":"LKR","declaredValue":100}',
        200,
      );
    });
    await tester.pumpWidget(
      MaterialApp(
        home: ShipmentDetailScreen(shipmentId: 'shipment-1', service: api),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Shipment Information'), findsOneWidget);
    expect(find.text('shipment-1'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    api.dispose();
  });
  testWidgets('Seller dashboard opens My Shipments without repeated buttons', (
    tester,
  ) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthProvider(),
        child: const MaterialApp(home: SellerDashboard()),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Manage Shipments'), findsNothing);
    await tester.scrollUntilVisible(
      find.text('My Shipments'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('My Shipments'));
    await tester.pumpAndSettle();
    expect(find.byType(SellerShipmentList), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
  });
  testWidgets('View Details navigates with the selected shipment ID', (
    tester,
  ) async {
    SharedPreferences.setMockInitialValues({});
    final api = service(
      (r) async => http.Response(
        '[{"id":"shipment-123","orderId":"o","status":"PlanGenerated","declaredValue":100,"currency":"LKR"}]',
        200,
      ),
    );
    await tester.pumpWidget(
      MaterialApp(home: SellerShipmentList(service: api)),
    );
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('View Details →'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.drag(find.byType(ListView).first, const Offset(0, -350));
    await tester.pumpAndSettle();
    await tester.tap(find.text('View Details →'));
    await tester.pumpAndSettle();
    expect(
      tester
          .widget<ShipmentDetailScreen>(find.byType(ShipmentDetailScreen))
          .shipmentId,
      'shipment-123',
    );
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    api.dispose();
  });
}

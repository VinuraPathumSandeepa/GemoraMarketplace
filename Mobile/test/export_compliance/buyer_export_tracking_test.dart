import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/buyer_export_tracking_model.dart';
import 'package:gemora_mobile/screens/marketplace/buyer_export_tracking_screen.dart';
import 'package:gemora_mobile/services/export_compliance_service.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

class FakeBuyerTrackingService extends ExportComplianceService {
  List<BuyerExportTrackingModel>? trackingListToReturn;
  BuyerExportTrackingModel? singleTrackingToReturn;
  Object? errorToThrow;

  @override
  Future<List<BuyerExportTrackingModel>> getMyExportTracking() async {
    if (errorToThrow != null) throw errorToThrow!;
    return trackingListToReturn ?? [];
  }

  @override
  Future<BuyerExportTrackingModel> getOrderExportTracking(
    dynamic orderId,
  ) async {
    if (errorToThrow != null) throw errorToThrow!;
    return singleTrackingToReturn ??
        BuyerExportTrackingModel(
          orderId: orderId.toString(),
          orderNumber: 'GEM-TEST-001',
          gemTitle: 'Royal Blue Sapphire',
          status: 'UnderReview',
          stageDescription: 'Under compliance verification',
          originCountry: 'Sri Lanka',
          destinationCountry: 'United States',
          isApproved: false,
          requiresExport: true,
        );
  }
}

void main() {
  group('Buyer export tracking API compatibility', () {
    setUp(
      () =>
          SharedPreferences.setMockInitialValues({'auth_token': 'test-token'}),
    );

    test('uses the tracking endpoint when it returns an empty list', () async {
      final requests = <String>[];
      final service = ExportComplianceService(
        client: MockClient((request) async {
          requests.add(request.url.path);
          return http.Response('[]', 200);
        }),
      );

      expect(await service.getMyExportTracking(), isEmpty);
      expect(requests, ['/api/export-tracking/my']);
    });

    test(
      'falls back to order export statuses when tracking route is missing',
      () async {
        final requests = <String>[];
        final service = ExportComplianceService(
          client: MockClient((request) async {
            requests.add(request.url.path);
            expect(request.headers['authorization'], 'Bearer test-token');
            if (request.url.path == '/api/export-tracking/my') {
              return http.Response('', 404);
            }
            return http.Response(
              jsonEncode([
                {
                  'id': 'order-1',
                  'orderNumber': 'GEM-123',
                  'gemTitle': 'Blue Sapphire',
                  'shippingCountryCode': 'US',
                  'exportStatus': 'Submitted',
                  'exportRequestId': 'export-1',
                  'createdAt': '2026-10-11T00:00:00Z',
                },
              ]),
              200,
            );
          }),
        );

        final tracking = await service.getMyExportTracking();
        expect(requests, ['/api/export-tracking/my', '/api/orders/my']);
        expect(tracking.single.orderId, 'order-1');
        expect(tracking.single.status, 'Submitted');
        expect(tracking.single.exportRequestId, 'export-1');
        expect(tracking.single.requiresExport, isTrue);
      },
    );

    test(
      'explains when both tracking and order routes are unavailable',
      () async {
        final service = ExportComplianceService(
          client: MockClient((_) async => http.Response('', 404)),
        );

        expect(
          service.getMyExportTracking(),
          throwsA(
            isA<Exception>().having(
              (error) => error.toString(),
              'message',
              contains('Restart the updated Gemora API'),
            ),
          ),
        );
      },
    );
  });

  group('BuyerExportTrackingModel Unit Tests', () {
    test('fromJson deserializes all fields correctly', () {
      final json = {
        'orderId': '11111111-2222-3333-4444-555555555555',
        'orderNumber': 'GEM-11112222',
        'gemListingId': 42,
        'gemTitle': 'Ceylon Star Ruby 5ct',
        'gemImageUrl': 'https://example.com/gem.jpg',
        'exportRequestId': 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
        'status': 'Approved',
        'stageDescription': 'Official export permit granted',
        'originCountry': 'Sri Lanka',
        'destinationCountry': 'Japan',
        'submittedAt': '2026-10-01T10:00:00Z',
        'reviewedAt': '2026-10-02T12:00:00Z',
        'updatedAt': '2026-10-02T12:00:00Z',
        'isApproved': true,
        'requiresExport': true,
      };

      final model = BuyerExportTrackingModel.fromJson(json);

      expect(model.orderId, '11111111-2222-3333-4444-555555555555');
      expect(model.orderNumber, 'GEM-11112222');
      expect(model.gemListingId, 42);
      expect(model.gemTitle, 'Ceylon Star Ruby 5ct');
      expect(model.gemImageUrl, 'https://example.com/gem.jpg');
      expect(model.exportRequestId, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee');
      expect(model.status, 'Approved');
      expect(model.stageDescription, 'Official export permit granted');
      expect(model.originCountry, 'Sri Lanka');
      expect(model.destinationCountry, 'Japan');
      expect(model.isApproved, true);
      expect(model.requiresExport, true);
    });

    test('fromJson handles null values safely', () {
      final json = {
        'orderId': 'order-123',
        'orderNumber': 'GEM-ORD',
        'gemTitle': 'Cat\'s Eye Chrysoberyl',
        'status': 'Draft',
        'stageDescription': 'Preparing export documentation',
        'originCountry': 'Sri Lanka',
        'destinationCountry': 'United Kingdom',
        'isApproved': false,
        'requiresExport': true,
      };

      final model = BuyerExportTrackingModel.fromJson(json);

      expect(model.orderId, 'order-123');
      expect(model.gemImageUrl, isNull);
      expect(model.submittedAt, isNull);
      expect(model.reviewedAt, isNull);
      expect(model.isApproved, false);
    });
  });

  group('BuyerExportTrackingScreen Widget Tests', () {
    testWidgets('Displays tracking list when items are returned', (
      tester,
    ) async {
      final fakeService = FakeBuyerTrackingService();
      fakeService.trackingListToReturn = [
        BuyerExportTrackingModel(
          orderId: 'order-1',
          orderNumber: 'GEM-001',
          gemTitle: 'Ceylon Sapphire 2ct',
          status: 'UnderReview',
          stageDescription: 'Under official NGJA review',
          originCountry: 'Sri Lanka',
          destinationCountry: 'United States',
          isApproved: false,
          requiresExport: true,
        ),
      ];

      await tester.pumpWidget(
        MaterialApp(home: BuyerExportTrackingScreen(service: fakeService)),
      );

      await tester.pumpAndSettle();

      expect(find.text('Track My Exports'), findsOneWidget);
      expect(find.text('GEM-001'), findsOneWidget);
      expect(find.text('Ceylon Sapphire 2ct'), findsOneWidget);
      expect(find.text('UnderReview'), findsOneWidget);
      expect(find.text('Sri Lanka → United States'), findsOneWidget);
    });

    testWidgets('Displays empty state when no export tracking records exist', (
      tester,
    ) async {
      final fakeService = FakeBuyerTrackingService();
      fakeService.trackingListToReturn = [];

      await tester.pumpWidget(
        MaterialApp(home: BuyerExportTrackingScreen(service: fakeService)),
      );

      await tester.pumpAndSettle();

      expect(find.text('No Export Clearances Found'), findsOneWidget);
    });

    testWidgets('Displays single order tracking when orderId is provided', (
      tester,
    ) async {
      final fakeService = FakeBuyerTrackingService();
      fakeService.singleTrackingToReturn = BuyerExportTrackingModel(
        orderId: 'single-order-uuid',
        orderNumber: 'GEM-999',
        gemTitle: 'Padparadscha Sapphire',
        status: 'Approved',
        stageDescription: 'Clearance Granted',
        originCountry: 'Sri Lanka',
        destinationCountry: 'France',
        isApproved: true,
        requiresExport: true,
      );

      await tester.pumpWidget(
        MaterialApp(
          home: BuyerExportTrackingScreen(
            orderId: 'single-order-uuid',
            service: fakeService,
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('GEM-999'), findsOneWidget);
      expect(find.text('Padparadscha Sapphire'), findsOneWidget);
      expect(find.text('Approved'), findsOneWidget);
    });
  });
}

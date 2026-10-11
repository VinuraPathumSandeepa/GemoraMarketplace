import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/compliance_document_model.dart';
import 'package:gemora_mobile/models/export_request_model.dart';
import 'package:gemora_mobile/models/user_model.dart';
import 'package:gemora_mobile/providers/auth_provider.dart';
import 'package:gemora_mobile/screens/export_compliance/export_request_detail_screen.dart';
import 'package:gemora_mobile/screens/export_compliance/my_export_requests_screen.dart';
import 'package:gemora_mobile/services/export_compliance_service.dart';
import 'package:provider/provider.dart';

class MockAuthProvider extends AuthProvider {
  final UserModel? _mockUser;
  MockAuthProvider(this._mockUser);

  @override
  UserModel? get user => _mockUser;
}

class FakeExportComplianceService extends ExportComplianceService {
  bool getMyExportRequestsCalled = false;
  List<ExportRequestModel>? requestsToReturn;
  Object? errorToThrow;
  Duration? delay;

  ExportRequestModel? detailRequestToReturn;
  List<ComplianceDocumentModel>? documentsToReturn;

  @override
  Future<List<ExportRequestModel>> getMyExportRequests() async {
    getMyExportRequestsCalled = true;
    if (delay != null) {
      await Future.delayed(delay!);
    }
    if (errorToThrow != null) {
      throw errorToThrow!;
    }
    return requestsToReturn ?? [];
  }

  @override
  Future<ExportRequestModel> getExportRequestById(String id) async {
    if (errorToThrow != null) {
      throw errorToThrow!;
    }
    return detailRequestToReturn ??
        ExportRequestModel(
          id: id,
          originCountry: 'Sri Lanka',
          destinationCountry: 'United States',
          declaredValue: 5000.0,
          currency: 'USD',
          status: 'UnderOfficerReview',
          createdAt: DateTime.now(),
          updatedAt: DateTime.now(),
        );
  }

  @override
  Future<List<ComplianceDocumentModel>> getComplianceDocuments(
    String id,
  ) async {
    return documentsToReturn ?? [];
  }
}

void main() {
  final buyerUser = UserModel(
    id: 'user-buyer-1',
    fullName: 'Test Buyer',
    email: 'buyer@gemora.com',
    role: 'Buyer',
  );

  final sellerUser = UserModel(
    id: 'user-seller-1',
    fullName: 'Test Seller',
    email: 'seller@gemora.com',
    role: 'Seller',
  );

  Widget createTestableWidget({
    required UserModel? user,
    required Widget child,
  }) {
    return ChangeNotifierProvider<AuthProvider>(
      create: (_) => MockAuthProvider(user),
      child: MaterialApp(home: child),
    );
  }

  group('Component 4 — Flutter Export Compliance Tests', () {
    // ----------------------------------------------------
    // BUYER TESTS
    // ----------------------------------------------------
    testWidgets(
      '1. Buyer opens Track My Exports & getMyExportRequests is NOT invoked',
      (WidgetTester tester) async {
        final fakeService = FakeExportComplianceService();

        await tester.pumpWidget(
          createTestableWidget(
            user: buyerUser,
            child: MyExportRequestsScreen(exportService: fakeService),
          ),
        );
        await tester.pumpAndSettle();

        expect(fakeService.getMyExportRequestsCalled, isFalse);
      },
    );

    testWidgets('2. Buyer mode displays placeholder text', (
      WidgetTester tester,
    ) async {
      final fakeService = FakeExportComplianceService();

      await tester.pumpWidget(
        createTestableWidget(
          user: buyerUser,
          child: MyExportRequestsScreen(exportService: fakeService),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Track My Exports'), findsNWidgets(2));
      expect(
        find.text(
          'Export tracking will appear here for purchases linked to your account.',
        ),
        findsOneWidget,
      );
    });

    testWidgets(
      '3. Buyer mode has NO Create Request button or seller controls',
      (WidgetTester tester) async {
        final fakeService = FakeExportComplianceService();

        await tester.pumpWidget(
          createTestableWidget(
            user: buyerUser,
            child: MyExportRequestsScreen(exportService: fakeService),
          ),
        );
        await tester.pumpAndSettle();

        expect(find.text('Create Request'), findsNothing);
        expect(find.text('Create Export Request'), findsNothing);
        expect(find.byIcon(Icons.add), findsNothing);
      },
    );

    // ----------------------------------------------------
    // SELLER TESTS
    // ----------------------------------------------------
    testWidgets('4. Seller sees My Export Requests and triggers API call', (
      WidgetTester tester,
    ) async {
      final fakeService = FakeExportComplianceService();

      await tester.pumpWidget(
        createTestableWidget(
          user: sellerUser,
          child: MyExportRequestsScreen(exportService: fakeService),
        ),
      );
      await tester.pumpAndSettle();

      expect(fakeService.getMyExportRequestsCalled, isTrue);
      expect(find.text('My Export Requests'), findsOneWidget);
    });

    testWidgets('5. Seller request loading state displays spinner', (
      WidgetTester tester,
    ) async {
      final fakeService = FakeExportComplianceService()
        ..delay = const Duration(seconds: 2);

      await tester.pumpWidget(
        createTestableWidget(
          user: sellerUser,
          child: MyExportRequestsScreen(exportService: fakeService),
        ),
      );
      await tester.pump(); // initial frame

      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pump(const Duration(seconds: 3)); // finish async
    });

    testWidgets(
      '6. Seller request empty state displays friendly prompt and View Orders button',
      (WidgetTester tester) async {
        final fakeService = FakeExportComplianceService()
          ..requestsToReturn = [];

        await tester.pumpWidget(
          createTestableWidget(
            user: sellerUser,
            child: MyExportRequestsScreen(exportService: fakeService),
          ),
        );
        await tester.pumpAndSettle();

        expect(
          find.text('Export clearance starts from an international order.'),
          findsOneWidget,
        );
        expect(find.text('View Orders'), findsOneWidget);
      },
    );

    testWidgets(
      '7. Seller request error state displays error message and Retry button',
      (WidgetTester tester) async {
        final fakeService = FakeExportComplianceService()
          ..errorToThrow = Exception(
            "We couldn't load your export requests. Please try again.",
          );

        await tester.pumpWidget(
          createTestableWidget(
            user: sellerUser,
            child: MyExportRequestsScreen(exportService: fakeService),
          ),
        );
        await tester.pumpAndSettle();

        expect(
          find.text("We couldn't load your export requests. Please try again."),
          findsOneWidget,
        );
        expect(find.text('Retry'), findsOneWidget);
      },
    );

    testWidgets(
      '8. Seller mode no longer displays generic Create Request button',
      (WidgetTester tester) async {
        final fakeService = FakeExportComplianceService()
          ..requestsToReturn = [];

        await tester.pumpWidget(
          createTestableWidget(
            user: sellerUser,
            child: MyExportRequestsScreen(exportService: fakeService),
          ),
        );
        await tester.pumpAndSettle();

        expect(find.text('Create Request'), findsNothing);
        expect(find.text('Create Export Request'), findsNothing);
        expect(find.byType(FloatingActionButton), findsNothing);
        expect(find.text('View Orders'), findsOneWidget);
      },
    );

    testWidgets('9. Export request list renders status tags correctly', (
      WidgetTester tester,
    ) async {
      final fakeService = FakeExportComplianceService()
        ..requestsToReturn = [
          ExportRequestModel(
            id: 'req-1',
            originCountry: 'Sri Lanka',
            destinationCountry: 'United States',
            declaredValue: 12500.0,
            currency: 'USD',
            status: 'UnderOfficerReview',
            createdAt: DateTime.now(),
            updatedAt: DateTime.now(),
          ),
          ExportRequestModel(
            id: 'req-2',
            originCountry: 'Sri Lanka',
            destinationCountry: 'Switzerland',
            declaredValue: 45000.0,
            currency: 'USD',
            status: 'Approved',
            createdAt: DateTime.now(),
            updatedAt: DateTime.now(),
          ),
        ];

      await tester.pumpWidget(
        createTestableWidget(
          user: sellerUser,
          child: MyExportRequestsScreen(exportService: fakeService),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Sri Lanka → United States'), findsOneWidget);
      expect(find.text('Officer Review'), findsOneWidget);
      expect(find.text('Sri Lanka → Switzerland'), findsOneWidget);
      expect(find.text('Approved'), findsOneWidget);
    });

    testWidgets(
      '10. Export request details screen renders Officer Review status',
      (WidgetTester tester) async {
        final fakeService = FakeExportComplianceService()
          ..detailRequestToReturn = ExportRequestModel(
            id: 'req-100',
            originCountry: 'Sri Lanka',
            destinationCountry: 'Germany',
            declaredValue: 8000.0,
            currency: 'USD',
            status: 'UnderOfficerReview',
            createdAt: DateTime.now(),
            updatedAt: DateTime.now(),
          );

        await tester.pumpWidget(
          createTestableWidget(
            user: sellerUser,
            child: ExportRequestDetailScreen(
              requestId: 'req-100',
              exportService: fakeService,
            ),
          ),
        );
        await tester.pumpAndSettle();

        expect(find.text('Export Request Details'), findsOneWidget);
        expect(find.text('Officer Review'), findsAtLeastNWidgets(1));
      },
    );
  });
}

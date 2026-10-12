import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/gem_verification_model.dart';
import 'package:gemora_mobile/screens/gemologist/verification_details_screen.dart';
import 'package:gemora_mobile/services/gem_verification_service.dart';

GemVerificationModel _verification(String decision, {String? notes}) {
  return GemVerificationModel(
    verificationId: 7,
    decision: decision,
    reviewNotes: notes,
    createdAt: DateTime.utc(2026, 10, 11),
    reviewedAt: decision == 'Pending' ? null : DateTime.utc(2026, 10, 11),
    gemListingId: 12,
    sellerId: 'seller',
    sellerName: 'Seller',
    title: 'Purple Quartz',
    gemType: 'Quartz',
    description: 'A gemstone listing',
    caratWeight: 2,
    color: 'Purple',
    clarity: 'Loup',
    cut: 'Oval',
    price: 100,
    currency: 'USD',
    primaryImageUrl: null,
    certificateNumber: null,
    certificateAuthority: null,
    certificateUrl: null,
    listingStatus: decision == 'Pending' ? 'PendingVerification' : decision,
    aiStatus: 'Completed',
    aiSuggestedGemType: null,
    aiConfidenceScore: null,
    aiFindings: null,
    aiRiskFlags: null,
    aiProcessedAt: null,
    gemologistId: decision == 'Pending' ? null : 'gemologist',
    gemologistName: decision == 'Pending' ? null : 'Gemologist',
  );
}

class _FakeVerificationService extends GemVerificationService {
  String? submittedNotes;

  @override
  Future<GemVerificationModel> getVerification(int verificationId) async =>
      _verification('Pending');

  @override
  Future<GemVerificationModel> reviewVerification({
    required int verificationId,
    required String decision,
    String? reviewNotes,
  }) async {
    submittedNotes = reviewNotes;
    return _verification(decision, notes: reviewNotes);
  }
}

void main() {
  testWidgets('request changes closes notes dialog without a frame error', (
    tester,
  ) async {
    final service = _FakeVerificationService();
    await tester.pumpWidget(
      MaterialApp(
        home: VerificationDetailsScreen(verificationId: 7, service: service),
      ),
    );
    await tester.pumpAndSettle();

    final requestButton = find.widgetWithText(
      OutlinedButton,
      'Request Changes',
    );
    await tester.scrollUntilVisible(requestButton, 250);
    await tester.ensureVisible(requestButton);
    await tester.pumpAndSettle();
    await tester.tap(requestButton);
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(FilledButton, 'Request Changes'));
    await tester.pump();
    expect(find.text('Please enter review notes.'), findsOneWidget);
    expect(service.submittedNotes, isNull);

    await tester.enterText(
      find.byType(TextField),
      'Please upload a clearer image.',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Request Changes'));
    await tester.pump();
    expect(tester.takeException(), isNull);
    await tester.pump(const Duration(milliseconds: 350));

    expect(service.submittedNotes, 'Please upload a clearer image.');
    expect(
      find.text(
        'The verification was successfully updated to Changes Requested.',
      ),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });
}

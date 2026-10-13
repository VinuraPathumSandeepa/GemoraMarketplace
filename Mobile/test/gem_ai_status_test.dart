import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/gem_ai_analysis_model.dart';
import 'package:gemora_mobile/models/gem_verification_model.dart';
import 'package:gemora_mobile/screens/gemologist/verification_details_screen.dart';
import 'package:gemora_mobile/services/gem_verification_service.dart';

class _FailedVerificationService extends GemVerificationService {
  int analysisCalls = 0;

  @override
  Future<GemVerificationModel> getVerification(int verificationId) async =>
      GemVerificationModel(
        verificationId: verificationId,
        decision: 'Pending',
        reviewNotes: null,
        createdAt: DateTime.utc(2026, 10, 13),
        reviewedAt: null,
        gemListingId: 12,
        sellerId: 'seller',
        sellerName: 'Seller',
        title: 'Blue Sapphire',
        gemType: 'Sapphire',
        description: 'Submitted listing',
        caratWeight: 2,
        color: 'Blue',
        clarity: 'VVS',
        cut: 'Oval',
        price: 100,
        currency: 'USD',
        primaryImageUrl: null,
        certificateNumber: null,
        certificateAuthority: null,
        certificateUrl: null,
        listingStatus: 'PendingVerification',
        aiStatus: 'Failed',
        aiSuggestedGemType: null,
        aiConfidenceScore: 0.85,
        aiFindings: 'The AI provider is rate limiting requests.',
        aiRiskFlags: jsonEncode({
          'imageAnalyzed': false,
          'evidenceWarnings': ['Certificate number is missing.'],
          'riskFlags': ['Certificate number is missing.'],
          'validationIssues': <String>[],
          'workflowSteps': [
            {'id': 'validation', 'status': 'Completed'},
            {'id': 'ai', 'status': 'Failed'},
            {'id': 'findings', 'status': 'Skipped'},
          ],
        }),
        aiProcessedAt: DateTime.utc(2026, 10, 13),
        gemologistId: null,
        gemologistName: null,
      );

  @override
  Future<GemAiAnalysisModel> runAiAnalysis(int verificationId) async {
    analysisCalls++;
    return GemAiAnalysisModel.fromJson({
      'status': 'Failed',
      'findings': 'The AI provider is rate limiting requests.',
      'imageAnalyzed': false,
      'confidenceScore': 0.85,
      'evidenceWarnings': ['Certificate number is missing.'],
      'riskFlags': ['Certificate number is missing.'],
      'workflowSteps': [
        {'id': 'validation', 'status': 'Completed'},
        {'id': 'ai', 'status': 'Failed'},
        {'id': 'findings', 'status': 'Skipped'},
      ],
    });
  }
}

void main() {
  testWidgets('failed analysis keeps warning status and manual review available',
      (tester) async {
    final service = _FailedVerificationService();
    await tester.pumpWidget(MaterialApp(
      home: VerificationDetailsScreen(verificationId: 7, service: service),
    ));
    await tester.pumpAndSettle();

    final result = find.byKey(const ValueKey('ai-result'));
    await tester.scrollUntilVisible(result, 250);
    expect(find.descendant(of: result,
        matching: find.byIcon(Icons.error_outline_rounded)), findsWidgets);
    expect(find.descendant(of: result,
        matching: find.text('Not available')), findsOneWidget);
    final unavailable = tester.widget<Text>(find.descendant(
        of: result, matching: find.text('Not available')));
    expect(unavailable.style?.color, const Color(0xFF697771));
    expect(find.text('Model-reported confidence'), findsOneWidget);
    expect(find.descendant(of: result,
        matching: find.text('ai: Failed')), findsOneWidget);
    expect(find.descendant(of: result,
        matching: find.text('findings: Skipped')), findsOneWidget);
    expect(find.text('Certificate number is missing.'), findsOneWidget);
    expect(find.text('Evidence Warnings'), findsOneWidget);

    final runButton = find.widgetWithText(FilledButton, 'Run AI Analysis');
    await tester.ensureVisible(runButton);
    await tester.pumpAndSettle();
    await tester.tap(runButton);
    await tester.pumpAndSettle();
    expect(service.analysisCalls, 1);
    expect(find.byType(VerificationDetailsScreen), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

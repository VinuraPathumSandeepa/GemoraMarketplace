import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/gem_listing_model.dart';
import 'package:gemora_mobile/screens/dashboards/admin_gem_listings_screen.dart';
import 'package:gemora_mobile/services/admin_gem_listing_service.dart';

class _FakeAdminGemService extends AdminGemListingService {
  int? deletedId;

  @override
  Future<List<GemListingModel>> getAll() async => const [
    GemListingModel(
      id: 11,
      title: 'Blue Sapphire',
      description: '',
      gemType: 'Sapphire',
      caratWeight: 1,
      color: 'Blue',
      clarity: 'VS',
      cut: 'Oval',
      price: 1000,
      currency: 'LKR',
      status: 'Sold',
    ),
  ];

  @override
  Future<void> delete(int id) async => deletedId = id;
}

void main() {
  testWidgets('admin can confirm deletion of a sold gem listing', (
    tester,
  ) async {
    final service = _FakeAdminGemService();
    await tester.pumpWidget(
      MaterialApp(home: AdminGemListingsScreen(service: service)),
    );
    await tester.pumpAndSettle();

    expect(find.text('Blue Sapphire'), findsOneWidget);
    expect(find.text('#11 · Sapphire · Sold'), findsOneWidget);
    await tester.tap(find.byTooltip('Delete Blue Sapphire'));
    await tester.pumpAndSettle();
    expect(find.textContaining('order history will remain'), findsOneWidget);
    await tester.tap(find.text('Delete').last);
    await tester.pumpAndSettle();

    expect(service.deletedId, 11);
    expect(find.text('Blue Sapphire'), findsNothing);
  });
}

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/gem_listing_model.dart';
import 'package:gemora_mobile/widgets/seller_listing_search.dart';

const listings = [
  GemListingModel(
    id: 1,
    title: 'Royal Blue Sapphire',
    description: '',
    gemType: 'Sapphire',
    caratWeight: 2.4,
    color: 'Royal Blue',
    clarity: 'VVS',
    cut: 'Oval',
    price: 1200,
    currency: 'USD',
    status: 'Approved',
    certificateNumber: 'GIA-123',
  ),
  GemListingModel(
    id: 2,
    title: 'Ceylon Ruby',
    description: '',
    gemType: 'Ruby',
    caratWeight: 1.2,
    color: 'Red',
    clarity: 'VS',
    cut: 'Cushion',
    price: 900,
    currency: 'USD',
    status: 'Draft',
    certificateNumber: 'GIA-456',
  ),
];

void main() {
  test(
    'suggests listing titles and certificate values from seller listings',
    () {
      expect(
        buildSellerListingSuggestions(
          listings,
          'roy',
        ).map((suggestion) => suggestion.label).toList(),
        ['Royal Blue Sapphire', 'Royal Blue'],
      );
      expect(
        buildSellerListingSuggestions(
          listings,
          'GIA',
        ).map((suggestion) => suggestion.label).toList(),
        ['GIA-123', 'GIA-456'],
      );
      expect(matchesSellerListing(listings[1], 'gia-456'), isTrue);
      expect(matchesSellerListing(listings[0], 'ruby'), isFalse);
    },
  );

  testWidgets('selecting a suggestion updates the query', (tester) async {
    String query = '';
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SellerListingSearch(
            listings: listings,
            onQueryChanged: (value) => query = value,
          ),
        ),
      ),
    );
    await tester.enterText(find.byType(TextField), 'roy');
    await tester.pumpAndSettle();
    expect(find.text('Royal Blue Sapphire'), findsOneWidget);
    await tester.tap(find.text('Royal Blue Sapphire'));
    await tester.pumpAndSettle();
    expect(query, 'Royal Blue Sapphire');
    expect(find.text('SUGGESTED SEARCHES'), findsNothing);
  });
}

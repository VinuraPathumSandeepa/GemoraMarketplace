import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/marketplace_gem.dart';
import 'package:gemora_mobile/widgets/gem_search_suggestions.dart';

final gems = [
  MarketplaceGem(
    id: 1,
    sellerName: 'Gem House',
    title: 'Royal Blue Sapphire',
    gemType: 'Sapphire',
    description: '',
    caratWeight: 2.4,
    color: 'Royal Blue',
    clarity: '',
    cut: 'Oval',
    price: 100000,
    currency: 'LKR',
    primaryImageUrl: null,
    certificateNumber: null,
    certificateAuthority: null,
    countryCode: 'LK',
    region: '',
    isAvailable: true,
  ),
  MarketplaceGem(
    id: 2,
    sellerName: 'Gem House',
    title: 'Ceylon Blue Sapphire',
    gemType: 'Sapphire',
    description: '',
    caratWeight: 1.8,
    color: 'Blue',
    clarity: '',
    cut: 'Oval',
    price: 80000,
    currency: 'LKR',
    primaryImageUrl: null,
    certificateNumber: null,
    certificateAuthority: null,
    countryCode: 'LK',
    region: '',
    isAvailable: true,
  ),
];

void main() {
  test('suggestions use marketplace listings and unique gemstone types', () {
    final suggestions = buildGemSearchSuggestions(gems, 'sap');
    expect(suggestions.map((item) => item.label), [
      'Ceylon Blue Sapphire',
      'Royal Blue Sapphire',
      'Sapphire',
    ]);
    expect(buildGemSearchSuggestions(gems, 'BLUE').length, 4);
  });

  testWidgets('tapping a suggestion passes its search term', (tester) async {
    String? selected;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: GemSearchSuggestions(
            gems: gems,
            query: 'sap',
            onSelected: (value) => selected = value,
          ),
        ),
      ),
    );

    await tester.tap(find.text('Sapphire').first);
    expect(selected, 'Sapphire');
  });
}

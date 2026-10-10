import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/screens/seller/create_gem_listing_screen.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  Future<void> pumpCreateListingScreen(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(1200, 4200));

    addTearDown(() async {
      await tester.binding.setSurfaceSize(null);
    });

    await tester.pumpWidget(const MaterialApp(home: CreateGemListingScreen()));

    await tester.pumpAndSettle();
  }

  Finder createButton() {
    return find.widgetWithText(FilledButton, 'Create Gem Listing');
  }

  Future<void> tapCreate(WidgetTester tester) async {
    final Finder button = createButton();

    await tester.ensureVisible(button);

    await tester.pumpAndSettle();

    await tester.tap(button);

    await tester.pumpAndSettle();
  }

  Future<void> enterValidRequiredFields(WidgetTester tester) async {
    final Finder fields = find.byType(TextFormField);

    expect(fields, findsNWidgets(10));

    await tester.enterText(fields.at(0), 'Premium Blue Sapphire');

    await tester.enterText(fields.at(1), 'Sapphire');

    await tester.enterText(
      fields.at(2),
      'Natural blue sapphire prepared for professional verification.',
    );

    await tester.enterText(fields.at(3), '2.50');

    await tester.enterText(fields.at(4), 'Royal Blue');

    await tester.enterText(fields.at(5), 'Eye Clean');

    await tester.enterText(fields.at(6), 'Oval Mixed Cut');

    await tester.enterText(fields.at(7), '250000');
  }

  group('Component 1 - Create Gem Listing Mobile UI', () {
    testWidgets('C1_MOB_01_CreateListingScreen_RendersRequiredSections', (
      WidgetTester tester,
    ) async {
      await pumpCreateListingScreen(tester);

      expect(find.text('Create Gem Listing'), findsWidgets);

      expect(find.text('Seller Workspace'), findsOneWidget);

      expect(find.text('Gemstone Information'), findsOneWidget);

      expect(find.text('Gem Characteristics'), findsOneWidget);

      expect(find.text('Pricing'), findsOneWidget);

      expect(find.text('Gemstone Image'), findsOneWidget);

      expect(find.text('Certificate'), findsOneWidget);

      expect(find.byType(TextFormField), findsNWidgets(10));
    });

    testWidgets('C1_MOB_02_EmptyForm_ShowsRequiredFieldValidation', (
      WidgetTester tester,
    ) async {
      await pumpCreateListingScreen(tester);

      await tapCreate(tester);

      expect(find.text('Title is required.'), findsOneWidget);

      expect(find.text('Gem type is required.'), findsOneWidget);

      expect(find.text('Description is required.'), findsOneWidget);

      expect(find.text('carat weight is required.'), findsOneWidget);

      expect(find.text('Color is required.'), findsOneWidget);

      expect(find.text('Clarity is required.'), findsOneWidget);

      expect(find.text('Cut is required.'), findsOneWidget);

      expect(find.text('price is required.'), findsOneWidget);
    });

    testWidgets('C1_MOB_03_InvalidBoundaryValues_AreRejected', (
      WidgetTester tester,
    ) async {
      await pumpCreateListingScreen(tester);

      final Finder fields = find.byType(TextFormField);

      expect(fields, findsNWidgets(10));

      await tester.enterText(fields.at(0), 'AB');

      await tester.enterText(fields.at(1), 'Sapphire');

      await tester.enterText(fields.at(2), 'short');

      await tester.enterText(fields.at(3), 'abc');

      await tester.enterText(fields.at(4), 'Blue');

      await tester.enterText(fields.at(5), 'Eye Clean');

      await tester.enterText(fields.at(6), 'Oval');

      await tester.enterText(fields.at(7), '0');

      await tapCreate(tester);

      expect(
        find.text('Title must contain at least 3 characters.'),
        findsOneWidget,
      );

      expect(
        find.text('Description must contain at least 10 characters.'),
        findsOneWidget,
      );

      expect(find.text('Enter a valid carat weight.'), findsOneWidget);

      expect(find.text('price must be greater than 0.'), findsOneWidget);
    });

    testWidgets('C1_MOB_04_ValidFormWithoutImage_IsRejectedBeforeApiCall', (
      WidgetTester tester,
    ) async {
      await pumpCreateListingScreen(tester);

      await enterValidRequiredFields(tester);

      await tapCreate(tester);

      expect(find.text('Title is required.'), findsNothing);

      expect(find.text('Description is required.'), findsNothing);

      expect(find.text('carat weight is required.'), findsNothing);

      expect(find.text('price is required.'), findsNothing);

      expect(find.text('Please select a gemstone image.'), findsOneWidget);
    });
  });
}

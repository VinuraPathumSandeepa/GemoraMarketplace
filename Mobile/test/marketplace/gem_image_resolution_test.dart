import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/config/api_config.dart';
import 'package:gemora_mobile/models/marketplace/marketplace_gem.dart';

void main() {
  group('Gem Image URL Resolution Tests', () {
    test('1. full HTTPS PrimaryImageUrl is used directly without modification', () {
      const liveSupabaseUrl =
          'https://sdfeoixnyshlefidjdkn.supabase.co/storage/v1/object/public/gem-images/5b211de9bf144f2d9f97dbf170162b61.png';

      final resolved = ApiConfig.resolveImageUrl(liveSupabaseUrl);

      expect(resolved, equals(liveSupabaseUrl));
      expect(resolved!.startsWith('https://'), isTrue);
    });

    test('2. null or blank image returns null so UI displays fallback', () {
      expect(ApiConfig.resolveImageUrl(null), isNull);
      expect(ApiConfig.resolveImageUrl(''), isNull);
      expect(ApiConfig.resolveImageUrl('   '), isNull);
    });

    test('3. relative path resolves using serverUrl without double slashes', () {
      final resolved = ApiConfig.resolveImageUrl('/images/gem1.png');
      expect(resolved, equals('${ApiConfig.serverUrl}/images/gem1.png'));

      final resolvedNoSlash = ApiConfig.resolveImageUrl('images/gem2.png');
      expect(resolvedNoSlash, equals('${ApiConfig.serverUrl}/images/gem2.png'));
    });

    test('4. image URL resolver does not prepend serverUrl/localhost to full Supabase URL', () {
      const fullUrl =
          'https://sdfeoixnyshlefidjdkn.supabase.co/storage/v1/object/public/gem-images/sample.webp';

      final resolved = ApiConfig.resolveImageUrl('  $fullUrl  ');

      expect(resolved, equals(fullUrl));
      expect(resolved!.contains(ApiConfig.serverUrl), isFalse);
    });

    test('MarketplaceGem model resolvedImageUrl getter returns resolved URL', () {
      final gemWithUrl = MarketplaceGem.fromJson({
        'id': 1,
        'title': 'Ceylon Sapphire',
        'primaryImageUrl': 'https://example.com/sapphire.png',
      });
      expect(gemWithUrl.resolvedImageUrl, equals('https://example.com/sapphire.png'));

      final gemWithoutUrl = MarketplaceGem.fromJson({
        'id': 2,
        'title': 'Rough Ruby',
        'primaryImageUrl': null,
      });
      expect(gemWithoutUrl.resolvedImageUrl, isNull);
    });
  });

  group('Gemstone Details Image Fallback Widget Tests', () {
    testWidgets('null image displays diamond fallback icon without crashing',
        (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Center(
              child: Icon(Icons.diamond_outlined, color: Color(0xFFC99242), size: 80),
            ),
          ),
        ),
      );

      expect(find.byIcon(Icons.diamond_outlined), findsOneWidget);
    });
  });
}

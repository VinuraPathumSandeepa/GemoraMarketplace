import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/marketplace/marketplace_gem.dart';
import 'package:gemora_mobile/services/marketplace_service.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  group('MarketplaceService.getGems', () {
    test('parses paginated response with "items" key successfully', () async {
      final mockClient = MockClient((request) async {
        final payload = {
          'items': [
            {
              'id': 40,
              'title': 'Green Zircon',
              'gemType': 'Zircon',
              'description': 'Natural Green Zircon',
              'caratWeight': 3.00,
              'color': 'Green',
              'clarity': 'Eye clean',
              'cut': 'Cushion',
              'price': 123344.00,
              'currency': 'LKR',
              'primaryImageUrl': 'https://example.com/zircon.png',
              'certificateNumber': 'GZ-2026-001',
              'certificateAuthority': 'Gem Laboratory',
              'countryCode': 'LK',
              'region': 'Sabaragamuwa',
              'sellerName': 'Vinura',
              'isAvailable': true,
            },
          ],
          'page': 1,
          'pageSize': 12,
          'totalItems': 1,
          'totalPages': 1,
        };

        return http.Response(
          jsonEncode(payload),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = MarketplaceService(client: mockClient);
      final gems = await service.getGems();

      expect(gems.length, 1);
      final gem = gems.first;
      expect(gem.id, 40);
      expect(gem.title, 'Green Zircon');
      expect(gem.gemType, 'Zircon');
      expect(gem.caratWeight, 3.0);
      expect(gem.price, 123344.0);
      expect(gem.sellerName, 'Vinura');
      expect(gem.isCertified, isTrue);
    });

    test('handles empty items array in paginated response', () async {
      final mockClient = MockClient((request) async {
        final payload = {
          'items': <dynamic>[],
          'page': 1,
          'pageSize': 12,
          'totalItems': 0,
          'totalPages': 0,
        };

        return http.Response(
          jsonEncode(payload),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = MarketplaceService(client: mockClient);
      final gems = await service.getGems();

      expect(gems, isEmpty);
    });

    test('supports raw list response for backward compatibility', () async {
      final mockClient = MockClient((request) async {
        final payload = [
          {
            'id': 21,
            'title': 'Ceylon Royal Blue Sapphire',
            'gemType': 'Sapphire',
            'description': 'Royal Blue',
            'caratWeight': 2.14,
            'color': 'Royal Blue',
            'clarity': 'Eye Clean',
            'cut': 'Oval',
            'price': 195000.0,
            'currency': 'LKR',
            'sellerName': 'Vinura',
            'isAvailable': true,
          },
        ];

        return http.Response(
          jsonEncode(payload),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = MarketplaceService(client: mockClient);
      final gems = await service.getGems();

      expect(gems.length, 1);
      expect(gems.first.title, 'Ceylon Royal Blue Sapphire');
    });

    test('throws friendly error on invalid response shape', () async {
      final mockClient = MockClient((request) async {
        return http.Response(
          jsonEncode({'unexpected': 'shape'}),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = MarketplaceService(client: mockClient);

      expect(
        () => service.getGems(),
        throwsA(
          predicate(
            (e) =>
                e is Exception &&
                e.toString().contains(
                  'The marketplace returned an unexpected response.',
                ),
          ),
        ),
      );
    });
  });

  group('MarketplaceGem.fromJson parsing tolerance', () {
    test('handles nullable fields, string numbers, and fallback defaults', () {
      final json = <String, dynamic>{
        'id': '105',
        'title': 'Rough Tourmaline',
        'gemType': 'Tourmaline',
        'description': null,
        'caratWeight': '5.75',
        'color': null,
        'clarity': null,
        'cut': null,
        'price': '45000',
        'currency': null,
        'primaryImageUrl': null,
        'certificateNumber': null,
        'certificateAuthority': null,
        'countryCode': null,
        'region': null,
        'sellerName': null,
        'isAvailable': 'true',
      };

      final gem = MarketplaceGem.fromJson(json);

      expect(gem.id, 105);
      expect(gem.title, 'Rough Tourmaline');
      expect(gem.gemType, 'Tourmaline');
      expect(gem.description, '');
      expect(gem.caratWeight, 5.75);
      expect(gem.color, '');
      expect(gem.clarity, '');
      expect(gem.cut, '');
      expect(gem.price, 45000.0);
      expect(gem.currency, 'LKR');
      expect(gem.primaryImageUrl, isNull);
      expect(gem.certificateNumber, isNull);
      expect(gem.certificateAuthority, isNull);
      expect(gem.countryCode, '');
      expect(gem.region, '');
      expect(gem.sellerName, '');
      expect(gem.isAvailable, isTrue);
      expect(gem.isCertified, isFalse);
    });

    test('correctly evaluates isCertified property', () {
      final nonCertified = MarketplaceGem.fromJson({
        'id': 1,
        'title': 'Gem A',
        'certificateNumber': '',
        'certificateAuthority': '   ',
      });
      expect(nonCertified.isCertified, isFalse);

      final certifiedWithNumber = MarketplaceGem.fromJson({
        'id': 2,
        'title': 'Gem B',
        'certificateNumber': 'CERT-123',
        'certificateAuthority': null,
      });
      expect(certifiedWithNumber.isCertified, isTrue);

      final certifiedWithAuthority = MarketplaceGem.fromJson({
        'id': 3,
        'title': 'Gem C',
        'certificateNumber': null,
        'certificateAuthority': 'GIA',
      });
      expect(certifiedWithAuthority.isCertified, isTrue);
    });
  });
}

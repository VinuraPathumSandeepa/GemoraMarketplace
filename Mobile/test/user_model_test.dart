import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/user_model.dart';

void main() {
  group('UserModel Tests', () {
    test('fromJson parses user attributes correctly', () {
      final json = {
        'id': 'usr_101',
        'fullName': 'Madhuka Sandeepa',
        'email': 'madhuka@gemora.com',
        'role': 'Seller',
      };

      final user = UserModel.fromJson(json);

      expect(user.id, equals('usr_101'));
      expect(user.fullName, equals('Madhuka Sandeepa'));
      expect(user.email, equals('madhuka@gemora.com'));
      expect(user.role, equals('Seller'));
    });

    test('fromJson handles null optional fields gracefully', () {
      final json = {
        'fullName': 'John Doe',
        'email': 'john@gemora.com',
      };

      final user = UserModel.fromJson(json);

      expect(user.id, isNull);
      expect(user.fullName, equals('John Doe'));
      expect(user.email, equals('john@gemora.com'));
      expect(user.role, equals(''));
    });
  });
}

import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/notification_model.dart';
import 'token_storage_service.dart';

class NotificationService {
  final TokenStorageService _tokenStorage = TokenStorageService();

  // ============================================================
  // GET NOTIFICATIONS
  // ============================================================

  Future<List<NotificationModel>> getNotifications() async {
    final token = await _requiredToken();

    final response = await http
        .get(Uri.parse(ApiConfig.notifications), headers: _headers(token))
        .timeout(const Duration(seconds: 30));

    if (response.statusCode != 200) {
      throw Exception(_errorMessage(response, 'Unable to load notifications.'));
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! List) {
      throw Exception('The notification response was invalid.');
    }

    return decoded.map((item) {
      if (item is! Map<String, dynamic>) {
        throw Exception('A notification record was invalid.');
      }

      return NotificationModel.fromJson(item);
    }).toList();
  }

  // ============================================================
  // GET UNREAD COUNT
  // ============================================================

  Future<int> getUnreadCount() async {
    final token = await _requiredToken();

    final response = await http
        .get(
          Uri.parse(ApiConfig.notificationUnreadCount),
          headers: _headers(token),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode != 200) {
      throw Exception(
        _errorMessage(response, 'Unable to load unread notification count.'),
      );
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The unread notification response was invalid.');
    }

    final value = decoded['unreadCount'];

    if (value is int) {
      return value;
    }

    return int.tryParse(value?.toString() ?? '') ?? 0;
  }

  // ============================================================
  // MARK ONE AS READ
  // ============================================================

  Future<void> markAsRead(String notificationId) async {
    final cleanId = notificationId.trim();

    if (cleanId.isEmpty) {
      throw Exception('The notification ID was invalid.');
    }

    final token = await _requiredToken();

    final response = await http
        .put(
          Uri.parse(ApiConfig.markNotificationRead(cleanId)),
          headers: _headers(token),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        _errorMessage(response, 'Unable to mark the notification as read.'),
      );
    }
  }

  // ============================================================
  // MARK ALL AS READ
  // ============================================================

  Future<void> markAllAsRead() async {
    final token = await _requiredToken();

    final response = await http
        .put(
          Uri.parse(ApiConfig.markAllNotificationsRead),
          headers: _headers(token),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        _errorMessage(response, 'Unable to mark all notifications as read.'),
      );
    }
  }

  // ============================================================
  // AUTH
  // ============================================================

  Future<String> _requiredToken() async {
    final token = await _tokenStorage.getToken();

    if (token == null || token.trim().isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }

    return token.trim();
  }

  Map<String, String> _headers(String token) {
    return {'Accept': 'application/json', 'Authorization': 'Bearer $token'};
  }

  // ============================================================
  // ERROR HANDLING
  // ============================================================

  String _errorMessage(http.Response response, String fallback) {
    if (response.statusCode == 401) {
      return 'Your session has expired. Please sign in again.';
    }
    if (response.statusCode >= 500) {
      return fallback;
    }
    try {
      if (response.body.trim().isEmpty) {
        return fallback;
      }

      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        final message = decoded['message'];

        if (message != null && message.toString().trim().isNotEmpty) {
          return message.toString().trim();
        }

        final title = decoded['title'];

        if (title != null && title.toString().trim().isNotEmpty) {
          return title.toString().trim();
        }

        final errors = decoded['errors'];

        if (errors is Map) {
          final messages = <String>[];

          for (final value in errors.values) {
            if (value is List) {
              messages.addAll(value.map((item) => item.toString()));
            }
          }

          if (messages.isNotEmpty) {
            return messages.join('\n');
          }
        }
      }
    } catch (_) {
      // Fall back to the supplied message.
    }

    return fallback;
  }
}

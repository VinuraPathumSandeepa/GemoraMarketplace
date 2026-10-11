import 'package:flutter/foundation.dart';

import '../models/notification_model.dart';
import '../services/notification_service.dart';

class NotificationProvider extends ChangeNotifier {
  NotificationProvider({NotificationService? service})
    : _service = service ?? NotificationService();

  final NotificationService _service;
  List<NotificationModel> notifications = [];
  int unreadCount = 0;
  bool loading = false;
  bool working = false;
  String? error;
  String? actionError;

  Future<void> refreshCount() async {
    try {
      unreadCount = await _service.getUnreadCount();
      notifyListeners();
    } catch (_) {
      // A failed badge request must not block the dashboard.
    }
  }

  Future<void> load() async {
    loading = true;
    error = null;
    actionError = null;
    notifyListeners();
    try {
      notifications = await _service.getNotifications();
      unreadCount = notifications.where((item) => !item.isRead).length;
    } catch (failure) {
      error = _message(
        failure,
        'Unable to load notifications. Please try again.',
      );
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<bool> markRead(NotificationModel notification) async {
    if (notification.isRead) return true;
    actionError = null;
    try {
      await _service.markAsRead(notification.id);
      notifications = notifications
          .map(
            (item) =>
                item.id == notification.id ? item.copyWith(isRead: true) : item,
          )
          .toList();
      unreadCount = notifications.where((item) => !item.isRead).length;
      notifyListeners();
      return true;
    } catch (failure) {
      actionError = _message(
        failure,
        'Unable to mark this notification as read. Please try again.',
      );
      notifyListeners();
      return false;
    }
  }

  Future<bool> markAllRead() async {
    if (working || unreadCount == 0) return true;
    actionError = null;
    working = true;
    notifyListeners();
    try {
      await _service.markAllAsRead();
      notifications = notifications
          .map((item) => item.copyWith(isRead: true))
          .toList();
      unreadCount = 0;
      return true;
    } catch (failure) {
      actionError = _message(
        failure,
        'Unable to mark all notifications as read. Please try again.',
      );
      return false;
    } finally {
      working = false;
      notifyListeners();
    }
  }

  static String _message(Object failure, String fallback) {
    if (failure.toString().contains('Your session has expired')) {
      return 'Your session has expired. Please sign in again.';
    }
    return fallback;
  }
}

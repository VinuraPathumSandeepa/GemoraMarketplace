import 'package:flutter_test/flutter_test.dart';
import 'package:gemora_mobile/models/notification_model.dart';
import 'package:gemora_mobile/providers/notification_provider.dart';
import 'package:gemora_mobile/services/notification_service.dart';

class FakeNotificationService extends NotificationService {
  final records = <NotificationModel>[
    NotificationModel(
      id: 'one',
      type: 'GemApproved',
      title: 'Approved',
      message: 'Your listing was approved.',
      relatedEntityType: 'GemListing',
      relatedEntityId: '12',
      isRead: false,
      createdAt: DateTime.utc(2026, 10, 11),
    ),
    NotificationModel(
      id: 'two',
      type: 'GemRejected',
      title: 'Rejected',
      message: 'Your listing was rejected.',
      relatedEntityType: 'GemListing',
      relatedEntityId: '13',
      isRead: false,
      createdAt: DateTime.utc(2026, 10, 10),
    ),
  ];

  bool failRead = false;

  @override
  Future<List<NotificationModel>> getNotifications() async => records;

  @override
  Future<int> getUnreadCount() async =>
      records.where((item) => !item.isRead).length;

  @override
  Future<void> markAsRead(String notificationId) async {
    if (failRead) throw Exception('Unable to mark notification as read.');
    final index = records.indexWhere((item) => item.id == notificationId);
    records[index] = records[index].copyWith(isRead: true);
  }

  @override
  Future<void> markAllAsRead() async {
    for (var index = 0; index < records.length; index++) {
      records[index] = records[index].copyWith(isRead: true);
    }
  }
}

void main() {
  test('read actions update the badge and stay read after reload', () async {
    final provider = NotificationProvider(service: FakeNotificationService());
    await provider.load();
    expect(provider.unreadCount, 2);

    expect(await provider.markRead(provider.notifications.first), isTrue);
    expect(provider.unreadCount, 1);
    expect(await provider.markAllRead(), isTrue);
    expect(provider.unreadCount, 0);

    await provider.load();
    expect(provider.notifications.every((item) => item.isRead), isTrue);
  });

  test('failed read keeps the notification unread', () async {
    final service = FakeNotificationService()..failRead = true;
    final provider = NotificationProvider(service: service);
    await provider.load();

    expect(await provider.markRead(provider.notifications.first), isFalse);
    expect(provider.unreadCount, 2);
    expect(provider.notifications.first.isRead, isFalse);
    expect(provider.actionError, isNotNull);
  });
}

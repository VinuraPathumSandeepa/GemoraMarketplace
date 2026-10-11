import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/notification_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/notification_provider.dart';
import '../seller/gem_listing_details_screen.dart';
import '../gemologist/verification_queue_screen.dart';
import '../gemologist/verification_details_screen.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  static const Color _darkGreen = Color(0xFF08251E);
  static const Color _green = Color(0xFF16483B);
  static const Color _gold = Color(0xFFC99242);
  static const Color _lightGold = Color(0xFFE4BC74);
  static const Color _cream = Color(0xFFFAF7F0);
  static const Color _text = Color(0xFF10241F);
  static const Color _muted = Color(0xFF697771);
  static const Color _border = Color(0xFFE7DFD2);

  bool _showUnreadOnly = false;

  @override
  void initState() {
    super.initState();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) context.read<NotificationProvider>().load();
    });
  }

  // ============================================================
  // LOAD NOTIFICATIONS
  // ============================================================

  Future<void> _loadNotifications() async {
    await context.read<NotificationProvider>().load();
  }

  // ============================================================
  // MARK ONE AS READ
  // ============================================================

  Future<void> _markRead(NotificationModel notification) async {
    final provider = context.read<NotificationProvider>();
    if (!await provider.markRead(notification)) {
      if (mounted) _showMessage(provider.actionError ?? 'Please try again.');
      return;
    }
    if (!mounted) return;

    final id = int.tryParse(notification.relatedEntityId ?? '');
    final role = context.read<AuthProvider>().user?.role;
    if (role == 'Seller' &&
        notification.relatedEntityType == 'GemListing' &&
        id != null) {
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GemListingDetailsScreen(listingId: id),
        ),
      );
    } else if (role == 'Gemologist' &&
        notification.relatedEntityType == 'GemVerification' &&
        id != null) {
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => VerificationDetailsScreen(verificationId: id),
        ),
      );
    } else if (role == 'Gemologist' &&
        notification.type == 'GemVerificationRequested') {
      await Navigator.of(context).push(
        MaterialPageRoute(builder: (_) => const VerificationQueueScreen()),
      );
    }
  }

  // ============================================================
  // MARK ALL AS READ
  // ============================================================

  Future<void> _markAllRead() async {
    final provider = context.read<NotificationProvider>();
    final success = await provider.markAllRead();
    if (mounted) {
      _showMessage(
        success
            ? 'All notifications marked as read.'
            : provider.actionError ?? 'Please try again.',
      );
    }
  }

  // ============================================================
  // FILTERED DATA
  // ============================================================

  List<NotificationModel> get _visibleNotifications {
    final notifications = context.read<NotificationProvider>().notifications;
    if (!_showUnreadOnly) {
      return notifications;
    }

    return notifications.where((notification) => !notification.isRead).toList();
  }

  int get _unreadCount {
    return context.read<NotificationProvider>().unreadCount;
  }

  // ============================================================
  // BUILD
  // ============================================================

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<NotificationProvider>();
    return Scaffold(
      backgroundColor: _cream,
      appBar: AppBar(
        backgroundColor: _darkGreen,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Notifications',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
            ),
            Text(
              'Gemora Notification Center',
              style: TextStyle(color: Colors.white60, fontSize: 10),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: _unreadCount == 0 || provider.working
                ? null
                : _markAllRead,
            child: Text(
              provider.working ? 'Please wait' : 'Read all',
              style: TextStyle(
                color: _unreadCount == 0 ? Colors.white38 : _lightGold,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadNotifications,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    final provider = context.watch<NotificationProvider>();
    if (provider.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (provider.error != null) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(22),
        children: [
          const SizedBox(height: 90),
          _ErrorState(message: provider.error!, onRetry: _loadNotifications),
        ],
      );
    }

    final visible = _visibleNotifications;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
      children: [
        _buildSummaryCard(),
        const SizedBox(height: 20),
        _buildFilters(),
        const SizedBox(height: 20),

        if (visible.isEmpty)
          _buildEmptyState()
        else
          ...visible.map(
            (notification) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: _NotificationCard(
                notification: notification,
                onTap: () => _markRead(notification),
              ),
            ),
          ),
      ],
    );
  }

  // ============================================================
  // SUMMARY
  // ============================================================

  Widget _buildSummaryCard() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [_darkGreen, _green],
        ),
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.07),
            blurRadius: 18,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 50,
            height: 50,
            decoration: BoxDecoration(
              color: _lightGold.withValues(alpha: 0.14),
              borderRadius: BorderRadius.circular(14),
            ),
            child: const Icon(
              Icons.notifications_active_outlined,
              color: _lightGold,
              size: 26,
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Stay up to date',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  _unreadCount == 0
                      ? 'You are all caught up.'
                      : '$_unreadCount unread ${_unreadCount == 1 ? 'notification' : 'notifications'} waiting for you.',
                  style: const TextStyle(
                    color: Colors.white70,
                    fontSize: 12,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ============================================================
  // FILTERS
  // ============================================================

  Widget _buildFilters() {
    return Row(
      children: [
        _FilterButton(
          label: 'All',
          selected: !_showUnreadOnly,
          onTap: () {
            setState(() {
              _showUnreadOnly = false;
            });
          },
        ),
        const SizedBox(width: 10),
        _FilterButton(
          label: 'Unread ($_unreadCount)',
          selected: _showUnreadOnly,
          onTap: () {
            setState(() {
              _showUnreadOnly = true;
            });
          },
        ),
      ],
    );
  }

  // ============================================================
  // EMPTY STATE
  // ============================================================

  Widget _buildEmptyState() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 50),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: _border),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        children: [
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              color: const Color(0xFFF4F0E7),
              borderRadius: BorderRadius.circular(20),
            ),
            child: const Icon(
              Icons.notifications_none_rounded,
              size: 30,
              color: _gold,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            _showUnreadOnly
                ? 'No unread notifications'
                : 'No notifications yet',
            style: const TextStyle(
              color: _text,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            _showUnreadOnly
                ? 'You have already reviewed everything.'
                : 'Important Gemora activity will appear here.',
            textAlign: TextAlign.center,
            style: const TextStyle(color: _muted, fontSize: 12, height: 1.5),
          ),
        ],
      ),
    );
  }

  // ============================================================
  // HELPERS
  // ============================================================

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }
}

// ============================================================
// NOTIFICATION CARD
// ============================================================

class _NotificationCard extends StatelessWidget {
  final NotificationModel notification;

  final VoidCallback onTap;

  const _NotificationCard({required this.notification, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final presentation = _presentationFor(notification.type);

    return Material(
      color: notification.isRead ? Colors.white : const Color(0xFFFFFBF3),
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(18),
        child: Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            border: Border.all(
              color: notification.isRead
                  ? const Color(0xFFE7DFD2)
                  : const Color(0xFFE4BC74),
            ),
            borderRadius: BorderRadius.circular(18),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 46,
                height: 46,
                decoration: BoxDecoration(
                  color: presentation.background,
                  borderRadius: BorderRadius.circular(13),
                ),
                child: Icon(
                  presentation.icon,
                  color: presentation.color,
                  size: 23,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notification.title,
                            style: TextStyle(
                              color: Color(0xFF10241F),
                              fontSize: 15,
                              fontWeight: notification.isRead
                                  ? FontWeight.w600
                                  : FontWeight.w800,
                            ),
                          ),
                        ),
                        if (!notification.isRead)
                          Container(
                            width: 8,
                            height: 8,
                            decoration: const BoxDecoration(
                              color: Color(0xFFC99242),
                              shape: BoxShape.circle,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 7),
                    Text(
                      notification.message,
                      style: const TextStyle(
                        color: Color(0xFF697771),
                        fontSize: 12,
                        height: 1.5,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        const Icon(
                          Icons.schedule_rounded,
                          size: 13,
                          color: Color(0xFF8A918E),
                        ),
                        const SizedBox(width: 5),
                        Text(
                          _timeAgo(notification.createdAt),
                          style: const TextStyle(
                            color: Color(0xFF7B8581),
                            fontSize: 10,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static _NotificationPresentation _presentationFor(String type) {
    switch (type) {
      case 'GemSubmitted':
        return const _NotificationPresentation(
          icon: Icons.outbox_rounded,
          color: Color(0xFF16483B),
          background: Color(0xFFE8F2ED),
        );
      case 'GemVerificationRequested':
        return const _NotificationPresentation(
          icon: Icons.fact_check_outlined,
          color: Color(0xFF16483B),
          background: Color(0xFFE8F2ED),
        );
      case 'GemApproved':
        return const _NotificationPresentation(
          icon: Icons.verified_rounded,
          color: Color(0xFF247A50),
          background: Color(0xFFE8F5ED),
        );

      case 'GemChangesRequested':
        return const _NotificationPresentation(
          icon: Icons.edit_note_rounded,
          color: Color(0xFFB3761B),
          background: Color(0xFFFFF2DA),
        );

      case 'GemRejected':
        return const _NotificationPresentation(
          icon: Icons.cancel_outlined,
          color: Color(0xFFB94747),
          background: Color(0xFFFCEAEA),
        );

      default:
        return const _NotificationPresentation(
          icon: Icons.notifications_outlined,
          color: Color(0xFFC99242),
          background: Color(0xFFF4F0E7),
        );
    }
  }

  static String _timeAgo(DateTime value) {
    final local = value.toLocal();

    final difference = DateTime.now().difference(local);

    if (difference.isNegative) {
      return 'Just now';
    }

    if (difference.inMinutes < 1) {
      return 'Just now';
    }

    if (difference.inMinutes < 60) {
      final minutes = difference.inMinutes;

      return '$minutes min ago';
    }

    if (difference.inHours < 24) {
      final hours = difference.inHours;

      return '$hours ${hours == 1 ? 'hour' : 'hours'} ago';
    }

    if (difference.inDays < 7) {
      final days = difference.inDays;

      return '$days ${days == 1 ? 'day' : 'days'} ago';
    }

    final day = local.day.toString().padLeft(2, '0');

    final month = local.month.toString().padLeft(2, '0');

    return '$day/$month/${local.year}';
  }
}

// ============================================================
// NOTIFICATION PRESENTATION
// ============================================================

class _NotificationPresentation {
  final IconData icon;

  final Color color;

  final Color background;

  const _NotificationPresentation({
    required this.icon,
    required this.color,
    required this.background,
  });
}

// ============================================================
// FILTER BUTTON
// ============================================================

class _FilterButton extends StatelessWidget {
  final String label;

  final bool selected;

  final VoidCallback onTap;

  const _FilterButton({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? const Color(0xFF102E27) : Colors.white,
      borderRadius: BorderRadius.circular(99),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(99),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 9),
          decoration: BoxDecoration(
            border: Border.all(
              color: selected
                  ? const Color(0xFF102E27)
                  : const Color(0xFFE7DFD2),
            ),
            borderRadius: BorderRadius.circular(99),
          ),
          child: Text(
            label,
            style: TextStyle(
              color: selected
                  ? const Color(0xFFE4BC74)
                  : const Color(0xFF697771),
              fontSize: 11,
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
      ),
    );
  }
}

// ============================================================
// ERROR STATE
// ============================================================

class _ErrorState extends StatelessWidget {
  final String message;

  final VoidCallback onRetry;

  const _ErrorState({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: const Color(0xFFE7DFD2)),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        children: [
          const Icon(
            Icons.notifications_off_outlined,
            size: 42,
            color: Color(0xFFC99242),
          ),
          const SizedBox(height: 14),
          const Text(
            'Unable to load notifications',
            style: TextStyle(
              color: Color(0xFF10241F),
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 7),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Color(0xFF697771),
              fontSize: 12,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: onRetry,
            icon: const Icon(Icons.refresh_rounded),
            label: const Text('Try Again'),
          ),
        ],
      ),
    );
  }
}

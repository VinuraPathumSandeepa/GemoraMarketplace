import 'package:flutter/material.dart';

import '../../config/api_config.dart';
import '../../constants/gemora_colors.dart';
import '../../models/marketplace/order_model.dart';
import '../../services/marketplace_service.dart';
import '../../widgets/buyer_glass.dart';
import 'buyer_export_tracking_screen.dart';
import 'card_payment_screen.dart';

enum _OrderFilter { all, action, active, delivered, cancelled }

class MyOrdersScreen extends StatefulWidget {
  final MarketplaceService? service;

  const MyOrdersScreen({super.key, this.service});

  @override
  State<MyOrdersScreen> createState() => _MyOrdersScreenState();
}

class _MyOrdersScreenState extends State<MyOrdersScreen> {
  late final MarketplaceService _service;
  final _search = TextEditingController();
  bool _loading = true;
  List<OrderModel> _orders = [];
  String? _error;
  _OrderFilter _filter = _OrderFilter.all;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? MarketplaceService();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final orders = await _service.getMyOrders();
      if (mounted) setState(() => _orders = orders);
    } catch (error) {
      if (mounted) {
        setState(
          () => _error = error.toString().replaceFirst('Exception: ', ''),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _cancel(OrderModel order) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this order?'),
        content: Text(
          '${order.orderNumber} will be cancelled and the gemstone will return to the marketplace.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Keep order'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFF9E3631),
            ),
            child: const Text('Cancel order'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await _service.cancelOrder(order.id);
      await _load();
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(error.toString().replaceFirst('Exception: ', '')),
          ),
        );
      }
    }
  }

  Future<void> _pay(OrderModel order) async {
    final changed = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => CardPaymentScreen(order: order, service: _service),
      ),
    );
    if (changed == true) await _load();
  }

  bool _matchesFilter(OrderModel order, _OrderFilter filter) {
    final status = order.status.toLowerCase();
    final fulfillment = order.fulfillmentStatus.toLowerCase();
    return switch (filter) {
      _OrderFilter.all => true,
      _OrderFilter.action =>
        status == 'confirmed' || status == 'awaitingpayment',
      _OrderFilter.active => !{
        'delivered',
        'completed',
        'cancelled',
        'rejected',
        'refunded',
        'failed',
      }.contains(status),
      _OrderFilter.delivered =>
        status == 'delivered' || fulfillment == 'delivered',
      _OrderFilter.cancelled => {
        'cancelled',
        'rejected',
        'refunded',
        'failed',
      }.contains(status),
    };
  }

  List<OrderModel> get _visibleOrders {
    final query = _search.text.trim().toLowerCase();
    return _orders.where((order) {
      final matchesSearch =
          query.isEmpty ||
          order.gemTitle.toLowerCase().contains(query) ||
          order.orderNumber.toLowerCase().contains(query) ||
          order.sellerName.toLowerCase().contains(query);
      return matchesSearch && _matchesFilter(order, _filter);
    }).toList()..sort((a, b) => b.createdAt.compareTo(a.createdAt));
  }

  int _count(_OrderFilter filter) =>
      _orders.where((order) => _matchesFilter(order, filter)).length;

  @override
  Widget build(BuildContext context) {
    final visibleOrders = _visibleOrders;
    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: AppBar(
        backgroundColor: GemoraColors.darkGreen,
        foregroundColor: Colors.white,
        title: const Text('My Orders'),
        actions: [
          IconButton(
            tooltip: 'Refresh orders',
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: BuyerGradientBackground(
        child: RefreshIndicator(
          color: GemoraColors.green,
          onRefresh: _load,
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverToBoxAdapter(child: _buildHeader()),
              SliverToBoxAdapter(child: _buildFilters()),
              if (_loading)
                const SliverFillRemaining(
                  hasScrollBody: false,
                  child: Center(
                    child: CircularProgressIndicator(color: GemoraColors.green),
                  ),
                )
              else if (_error != null)
                SliverFillRemaining(hasScrollBody: false, child: _buildError())
              else if (visibleOrders.isEmpty)
                SliverFillRemaining(hasScrollBody: false, child: _buildEmpty())
              else
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 4, 16, 36),
                  sliver: SliverList.separated(
                    itemCount: visibleOrders.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 14),
                    itemBuilder: (context, index) {
                      final order = visibleOrders[index];
                      return TweenAnimationBuilder<double>(
                        key: ValueKey('${order.id}-${_filter.name}'),
                        tween: Tween(begin: 0, end: 1),
                        duration: Duration(milliseconds: 260 + index * 55),
                        curve: Curves.easeOutCubic,
                        builder: (context, value, child) => Opacity(
                          opacity: value,
                          child: Transform.translate(
                            offset: Offset(0, 18 * (1 - value)),
                            child: child,
                          ),
                        ),
                        child: _OrderGlassCard(
                          order: order,
                          onPay: () => _pay(order),
                          onCancel: () => _cancel(order),
                        ),
                      );
                    },
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    final active = _count(_OrderFilter.active);
    final delivered = _count(_OrderFilter.delivered);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 20, 16, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const BuyerSectionHeading(
            eyebrow: 'Purchase journey',
            title: 'Track every gemstone',
            description: 'From seller approval and payment to courier handover and delivery.',
          ),
          const SizedBox(height: 18),
          Row(
            children: [
              Expanded(
                child: _SummaryTile(
                  icon: Icons.receipt_long_rounded,
                  label: 'Total',
                  value: '${_orders.length}',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _SummaryTile(
                  icon: Icons.local_shipping_outlined,
                  label: 'Active',
                  value: '$active',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _SummaryTile(
                  icon: Icons.check_circle_outline_rounded,
                  label: 'Delivered',
                  value: '$delivered',
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          TextField(
            controller: _search,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              hintText: 'Search order, gemstone or seller',
              prefixIcon: const Icon(Icons.search_rounded),
              suffixIcon: _search.text.isEmpty
                  ? null
                  : IconButton(
                      onPressed: () {
                        _search.clear();
                        setState(() {});
                      },
                      icon: const Icon(Icons.close_rounded),
                    ),
              filled: true,
              fillColor: Colors.white.withValues(alpha: .76),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(18),
                borderSide: BorderSide.none,
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(18),
                borderSide: BorderSide(
                  color: GemoraColors.darkGreen.withValues(alpha: .09),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilters() {
    const labels = {
      _OrderFilter.all: 'All',
      _OrderFilter.action: 'Action needed',
      _OrderFilter.active: 'Active',
      _OrderFilter.delivered: 'Delivered',
      _OrderFilter.cancelled: 'Closed',
    };
    return SizedBox(
      height: 54,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.fromLTRB(16, 4, 16, 10),
        children: labels.entries.map((entry) {
          final selected = _filter == entry.key;
          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ChoiceChip(
              selected: selected,
              showCheckmark: false,
              label: Text('${entry.value}  ${_count(entry.key)}'),
              onSelected: (_) => setState(() => _filter = entry.key),
              selectedColor: GemoraColors.darkGreen,
              backgroundColor: Colors.white.withValues(alpha: .68),
              side: BorderSide(
                color: selected
                    ? GemoraColors.darkGreen
                    : Colors.white.withValues(alpha: .86),
              ),
              labelStyle: TextStyle(
                color: selected ? Colors.white : GemoraColors.darkGreen,
                fontSize: 11,
                fontWeight: FontWeight.w700,
              ),
            ),
          );
        }).toList(),
      ),
    );
  }

  Widget _buildError() => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_rounded,
            size: 48,
            color: GemoraColors.darkGold,
          ),
          const SizedBox(height: 12),
          Text(_error!, textAlign: TextAlign.center),
          const SizedBox(height: 16),
          FilledButton(onPressed: _load, child: const Text('Try again')),
        ],
      ),
    ),
  );

  Widget _buildEmpty() => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.inventory_2_outlined,
            size: 54,
            color: GemoraColors.darkGold,
          ),
          const SizedBox(height: 14),
          Text(
            _orders.isEmpty
                ? 'Your gemstone orders will appear here.'
                : 'No orders match this filter.',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: GemoraColors.mutedText,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    ),
  );
}

class _SummaryTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _SummaryTile({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return BuyerGlassPanel(
      padding: const EdgeInsets.all(13),
      borderRadius: BorderRadius.circular(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 19, color: GemoraColors.green),
          const SizedBox(height: 9),
          Text(
            value,
            style: const TextStyle(
              color: GemoraColors.darkGreen,
              fontSize: 22,
              fontWeight: FontWeight.w900,
            ),
          ),
          Text(
            label,
            style: const TextStyle(color: GemoraColors.mutedText, fontSize: 10),
          ),
        ],
      ),
    );
  }
}

class _OrderGlassCard extends StatefulWidget {
  final OrderModel order;
  final VoidCallback onPay;
  final VoidCallback onCancel;

  const _OrderGlassCard({
    required this.order,
    required this.onPay,
    required this.onCancel,
  });

  @override
  State<_OrderGlassCard> createState() => _OrderGlassCardState();
}

class _OrderGlassCardState extends State<_OrderGlassCard> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final order = widget.order;
    final status = order.status.toLowerCase();
    final canPay = status == 'confirmed' || status == 'awaitingpayment';
    final canCancel = status == 'pending' || status == 'confirmed';
    final color = _statusColor(status);
    return BuyerGlassPanel(
      padding: EdgeInsets.zero,
      child: Column(
        children: [
          InkWell(
            onTap: () => setState(() => _expanded = !_expanded),
            borderRadius: BorderRadius.circular(24),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _OrderImage(
                        url: ApiConfig.resolveImageUrl(order.gemImageUrl),
                      ),
                      const SizedBox(width: 13),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              order.gemTitle,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                color: GemoraColors.darkGreen,
                                fontSize: 16,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            const SizedBox(height: 5),
                            Text(
                              order.orderNumber,
                              style: const TextStyle(
                                color: GemoraColors.mutedText,
                                fontSize: 11,
                                letterSpacing: .5,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Wrap(
                              spacing: 7,
                              runSpacing: 7,
                              children: [
                                _Pill(
                                  label: _readableStatus(order.status),
                                  color: color,
                                ),
                                _Pill(
                                  label: _deliveryLabel(order),
                                  color: GemoraColors.green,
                                  icon: Icons.local_shipping_outlined,
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                      AnimatedRotation(
                        turns: _expanded ? .5 : 0,
                        duration: const Duration(milliseconds: 220),
                        child: const Icon(
                          Icons.keyboard_arrow_down_rounded,
                          color: GemoraColors.mutedText,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Text(
                        '${order.currency} ${order.agreedPrice.toStringAsFixed(2)}',
                        style: const TextStyle(
                          color: GemoraColors.darkGreen,
                          fontSize: 16,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const Spacer(),
                      Text(
                        _date(order.createdAt),
                        style: const TextStyle(
                          color: GemoraColors.mutedText,
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ),
                  if (canPay) ...[
                    const SizedBox(height: 14),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton.icon(
                        onPressed: widget.onPay,
                        style: FilledButton.styleFrom(
                          backgroundColor: GemoraColors.darkGreen,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14),
                          ),
                        ),
                        icon: const Icon(Icons.credit_card_rounded, size: 18),
                        label: const Text(
                          'Pay with credit or debit card',
                          style: TextStyle(fontWeight: FontWeight.w800),
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
          AnimatedCrossFade(
            firstChild: const SizedBox(width: double.infinity),
            secondChild: _OrderDetails(
              order: order,
              canCancel: canCancel,
              onCancel: widget.onCancel,
            ),
            crossFadeState: _expanded
                ? CrossFadeState.showSecond
                : CrossFadeState.showFirst,
            duration: const Duration(milliseconds: 260),
            sizeCurve: Curves.easeOutCubic,
          ),
        ],
      ),
    );
  }
}

class _OrderDetails extends StatelessWidget {
  final OrderModel order;
  final bool canCancel;
  final VoidCallback onCancel;

  const _OrderDetails({
    required this.order,
    required this.canCancel,
    required this.onCancel,
  });

  @override
  Widget build(BuildContext context) {
    final hasExport =
        order.exportStatus != null &&
        order.exportStatus!.toLowerCase() != 'notrequired';
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .32),
        border: Border(
          top: BorderSide(color: GemoraColors.darkGreen.withValues(alpha: .07)),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SizedBox(height: 14),
          const Text(
            'ORDER JOURNEY',
            style: TextStyle(
              color: GemoraColors.darkGold,
              fontSize: 9,
              fontWeight: FontWeight.w900,
              letterSpacing: 1.2,
            ),
          ),
          const SizedBox(height: 12),
          _JourneyBar(order: order),
          if (order.trackingNumber != null) ...[
            const SizedBox(height: 14),
            _InfoRow(
              icon: Icons.pin_drop_outlined,
              title: 'Tracking number',
              value: order.trackingNumber!,
            ),
          ],
          const SizedBox(height: 10),
          _InfoRow(
            icon: Icons.storefront_outlined,
            title: 'Seller',
            value: order.sellerName,
          ),
          const SizedBox(height: 10),
          _InfoRow(
            icon: Icons.public_rounded,
            title: 'Export clearance',
            value: _exportLabel(order.exportStatus),
          ),
          if (order.statusHistory.isNotEmpty) ...[
            const SizedBox(height: 18),
            ...order.statusHistory.reversed
                .take(3)
                .map(
                  (history) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          margin: const EdgeInsets.only(top: 5),
                          decoration: const BoxDecoration(
                            color: GemoraColors.darkGold,
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            '${_readableStatus(history.newStatus)} · ${history.changedByName}'
                            '${history.reason == null ? '' : '\n${history.reason}'}',
                            style: const TextStyle(
                              color: GemoraColors.mutedText,
                              fontSize: 11,
                              height: 1.4,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
          ],
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              if (hasExport)
                OutlinedButton.icon(
                  onPressed: () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) =>
                          BuyerExportTrackingScreen(orderId: order.id),
                    ),
                  ),
                  icon: const Icon(Icons.flight_takeoff_rounded, size: 17),
                  label: const Text('Track export'),
                ),
              if (canCancel)
                TextButton.icon(
                  onPressed: onCancel,
                  icon: const Icon(Icons.close_rounded, size: 17),
                  label: const Text('Cancel order'),
                  style: TextButton.styleFrom(
                    foregroundColor: const Color(0xFF9E3631),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _JourneyBar extends StatelessWidget {
  final OrderModel order;

  const _JourneyBar({required this.order});

  @override
  Widget build(BuildContext context) {
    final current = _journeyStep(order);
    const labels = ['Placed', 'Approved', 'Paid', 'Handover', 'Delivered'];
    return Row(
      children: List.generate(labels.length, (index) {
        final done = index <= current;
        return Expanded(
          child: Column(
            children: [
              Row(
                children: [
                  if (index > 0)
                    Expanded(
                      child: Container(
                        height: 2,
                        color: index <= current
                            ? GemoraColors.green
                            : GemoraColors.border,
                      ),
                    ),
                  AnimatedContainer(
                    duration: const Duration(milliseconds: 300),
                    width: 22,
                    height: 22,
                    decoration: BoxDecoration(
                      color: done ? GemoraColors.green : Colors.white,
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: done ? GemoraColors.green : GemoraColors.border,
                      ),
                    ),
                    child: done
                        ? const Icon(
                            Icons.check_rounded,
                            color: Colors.white,
                            size: 14,
                          )
                        : null,
                  ),
                  if (index < labels.length - 1)
                    Expanded(
                      child: Container(
                        height: 2,
                        color: index < current
                            ? GemoraColors.green
                            : GemoraColors.border,
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                labels[index],
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: done ? GemoraColors.darkGreen : GemoraColors.mutedText,
                  fontSize: 8,
                  fontWeight: done ? FontWeight.w800 : FontWeight.w500,
                ),
              ),
            ],
          ),
        );
      }),
    );
  }
}

class _OrderImage extends StatelessWidget {
  final String? url;

  const _OrderImage({required this.url});

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(17),
      child: Container(
        width: 72,
        height: 72,
        color: const Color(0xFFE9E4D9),
        child: url == null || url!.isEmpty
            ? const Icon(
                Icons.diamond_outlined,
                color: GemoraColors.darkGold,
                size: 32,
              )
            : Image.network(
                url!,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const Icon(
                  Icons.diamond_outlined,
                  color: GemoraColors.darkGold,
                  size: 32,
                ),
              ),
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  final String label;
  final Color color;
  final IconData? icon;

  const _Pill({required this.label, required this.color, this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .11),
        borderRadius: BorderRadius.circular(99),
        border: Border.all(color: color.withValues(alpha: .18)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 12, color: color),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: TextStyle(
              color: color,
              fontSize: 9,
              fontWeight: FontWeight.w800,
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String title;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.title,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 17, color: GemoraColors.green),
        const SizedBox(width: 9),
        Text('$title: ', style: const TextStyle(fontSize: 11)),
        Expanded(
          child: Text(
            value,
            textAlign: TextAlign.right,
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w800),
          ),
        ),
      ],
    );
  }
}

int _journeyStep(OrderModel order) {
  final status = order.status.toLowerCase();
  final fulfillment = order.fulfillmentStatus.toLowerCase();
  if (status == 'delivered' || fulfillment == 'delivered') {
    return 4;
  }
  if ({
    'handedovertocourier',
    'intransit',
    'outfordelivery',
  }.contains(fulfillment)) {
    return 3;
  }
  if ({
    'paid',
    'preparingforshipment',
    'shipmentcreated',
    'intransit',
  }.contains(status)) {
    return 2;
  }
  if ({'confirmed', 'awaitingpayment'}.contains(status)) {
    return 1;
  }
  return 0;
}

Color _statusColor(String status) {
  if ({'cancelled', 'rejected', 'failed'}.contains(status)) {
    return const Color(0xFF9E3631);
  }
  if ({'delivered', 'completed', 'paid'}.contains(status)) {
    return const Color(0xFF2E7D55);
  }
  if ({'confirmed', 'awaitingpayment'}.contains(status)) {
    return GemoraColors.darkGold;
  }
  return GemoraColors.green;
}

String _readableStatus(String value) => value
    .replaceAllMapped(RegExp(r'([a-z])([A-Z])'), (m) => '${m[1]} ${m[2]}')
    .replaceAll('_', ' ')
    .trim();

String _deliveryLabel(OrderModel order) {
  final status = order.fulfillmentStatus;
  if (status.isNotEmpty && status.toLowerCase() != 'pending') {
    return _readableStatus(status);
  }
  return order.status.toLowerCase() == 'delivered'
      ? 'Delivered'
      : 'Delivery pending';
}

String _exportLabel(String? value) {
  if (value == null || value.isEmpty || value.toLowerCase() == 'notrequired') {
    return 'Not required';
  }
  return _readableStatus(value);
}

String _date(DateTime date) {
  const months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
  return '${date.day} ${months[date.month - 1]} ${date.year}';
}

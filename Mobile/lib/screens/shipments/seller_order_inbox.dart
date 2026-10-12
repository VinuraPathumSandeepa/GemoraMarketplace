import 'package:flutter/material.dart';

import '../../models/shipment_model.dart';
import '../../services/shipment_service.dart';
import 'shipment_widgets.dart';
import 'shipment_detail_screen.dart';

Future<void> showSellerOrderInbox(BuildContext context) =>
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: shippingCream,
      builder: (_) => const FractionallySizedBox(
        heightFactor: .9,
        child: SellerOrderInbox(),
      ),
    );

class SellerOrderInbox extends StatefulWidget {
  final ShipmentService? service;
  const SellerOrderInbox({super.key, this.service});
  @override
  State<SellerOrderInbox> createState() => _SellerOrderInboxState();
}

class _SellerOrderInboxState extends State<SellerOrderInbox> {
  late final ShipmentService _service = widget.service ?? ShipmentService();
  List<ShipmentOrder> _orders = [];
  bool _loading = true, _all = false;
  String? _error, _busy;
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    if (widget.service == null) _service.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final rows = await _service.getSellerOrders();
      final orders =
          rows
              .map((r) => ShipmentOrder.fromJson(Map<String, dynamic>.from(r)))
              .toList()
            ..sort(
              (a, b) =>
                  (ShipmentModel.parseDate(b.data['createdAt']) ??
                          DateTime(1970))
                      .compareTo(
                        ShipmentModel.parseDate(a.data['createdAt']) ??
                            DateTime(1970),
                      ),
            );
      if (mounted) {
        setState(() {
          _orders = orders;
          _error = null;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = shippingError(e);
          _loading = false;
        });
      }
    }
  }

  Future<void> _act(
    ShipmentOrder order,
    String action, {
    String reason = '',
    bool sold = false,
  }) async {
    if (_busy != null) return;
    setState(() => _busy = order.id);
    try {
      await _service.orderAction(
        order.id,
        action,
        action == 'message/read'
            ? {'messageAt': order.data['createdAt']}
            : {'reason': reason, 'alreadySold': sold},
      );
      if (mounted) await _load();
    } catch (e) {
      if (mounted) setState(() => _error = shippingError(e));
    } finally {
      if (mounted) setState(() => _busy = null);
    }
  }

  Future<void> _decision(
    ShipmentOrder order,
    String action, {
    bool sold = false,
  }) async {
    final reason = TextEditingController();
    final form = GlobalKey<FormState>();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        scrollable: true,
        title: Text(
          action == 'approve'
              ? 'Approve this buyer?'
              : sold
              ? 'Mark gemstone as sold?'
              : 'Reject order request?',
        ),
        content: Form(
          key: form,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                action == 'approve'
                    ? 'The gemstone will be reserved for 3 hours and other pending requests will be rejected.'
                    : sold
                    ? 'All pending requests will be rejected and the listing will leave the marketplace.'
                    : 'Explain your decision to the buyer. The gemstone will remain available.',
              ),
              if (action == 'reject' && !sold)
                TextFormField(
                  controller: reason,
                  maxLength: 1000,
                  maxLines: 3,
                  decoration: const InputDecoration(
                    labelText: 'Rejection reason',
                  ),
                  validator: (v) =>
                      ShipmentValidation.requiredText(v, max: 1000),
                ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Back'),
          ),
          FilledButton(
            onPressed: () {
              if (form.currentState!.validate()) Navigator.pop(ctx, true);
            },
            child: const Text('Confirm decision'),
          ),
        ],
      ),
    );
    final text = reason.text.trim();
    // The dialog's closing animation may still be using its controller.
    await Future<void>.delayed(const Duration(milliseconds: 300));
    reason.dispose();
    if (confirmed == true && mounted) {
      await _act(order, action, reason: text, sold: sold);
    }
  }

  Future<void> _details(ShipmentOrder order) async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => SellerOrderDetails(orderId: order.id)),
    );
    if (mounted) await _load();
  }

  @override
  Widget build(BuildContext context) {
    final visible = _orders
        .where((o) => _all || o.text('status') == 'Pending')
        .toList();
    return Column(
      children: [
        Container(
          color: shippingGreen,
          padding: const EdgeInsets.all(20),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'GEMORA INBOX',
                      style: TextStyle(
                        color: shippingGold,
                        fontSize: 10,
                        letterSpacing: 2,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Order requests',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    Text(
                      '${_orders.where((o) => o.unread).length} new orders',
                      style: const TextStyle(color: Colors.white),
                    ),
                  ],
                ),
              ),
              IconButton(
                tooltip: 'Close inbox',
                onPressed: () => Navigator.pop(context),
                icon: const Icon(Icons.close, color: Colors.white),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.all(12),
          child: Wrap(
            spacing: 12,
            children: [
              OutlinedButton(
                onPressed: _busy != null
                    ? null
                    : () => setState(() => _all = !_all),
                child: Text(_all ? 'Show pending requests' : 'Show all orders'),
              ),
              OutlinedButton.icon(
                onPressed: _busy != null ? null : _load,
                icon: const Icon(Icons.refresh),
                label: const Text('Refresh'),
              ),
            ],
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _load,
            child: shippingBody([
              if (_error != null) ShippingFeedback(_error!, retry: _load),
              if (_loading)
                const Center(child: CircularProgressIndicator())
              else if (visible.isEmpty && _error == null)
                ShippingFeedback(
                  _all ? 'No orders yet.' : 'No pending requests. New buyer orders will appear here.',
                )
              else
                ...visible.map(
                  (o) => ShippingSection(
                    title: o.text('gemTitle'),
                    children: [
                      Align(
                        alignment: Alignment.centerLeft,
                        child: ShipmentStatusBadge(o.text('status')),
                      ),
                      const SizedBox(height: 12),
                      ShippingValue('Order', o.text('orderNumber')),
                      ShippingValue('Buyer', o.text('buyerName')),
                      ShippingValue(
                        'Placed',
                        shippingDate(o.data['createdAt']),
                      ),
                      ShippingValue(
                        'Value',
                        shippingMoney(o.amount, o.text('currency')),
                      ),
                      if (o.data['paymentDueAt'] != null)
                        ShippingValue(
                          'Payment deadline',
                          ShipmentModel.parseDate(o.data['paymentDueAt'])
                              ?.toString(),
                        ),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          TextButton(
                            onPressed: _busy != null ? null : () => _details(o),
                            child: const Text('View Order'),
                          ),
                          if (o.text('status') == 'Pending') ...[
                            FilledButton(
                              onPressed: _busy != null
                                  ? null
                                  : () => _decision(o, 'approve'),
                              child: const Text('Approve'),
                            ),
                            OutlinedButton(
                              onPressed: _busy != null
                                  ? null
                                  : () => _decision(o, 'reject'),
                              child: const Text('Reject'),
                            ),
                            TextButton(
                              onPressed: _busy != null
                                  ? null
                                  : () => _decision(o, 'reject', sold: true),
                              child: const Text('Already sold'),
                            ),
                          ],
                          if (o.unread)
                            TextButton(
                              onPressed: _busy != null
                                  ? null
                                  : () => _act(o, 'message/read'),
                              child: const Text('Mark read'),
                            ),
                        ],
                      ),
                      if (_busy == o.id) const LinearProgressIndicator(),
                    ],
                  ),
                ),
            ]),
          ),
        ),
      ],
    );
  }
}

class SellerOrderDetails extends StatefulWidget {
  final String orderId;
  const SellerOrderDetails({super.key, required this.orderId});
  @override
  State<SellerOrderDetails> createState() => _SellerOrderDetailsState();
}

class _SellerOrderDetailsState extends State<SellerOrderDetails> {
  final _service = ShipmentService();
  Map<String, dynamic>? _data;
  String? _error;
  bool _loading = true;
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _service.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final data = await _service.getSellerOrder(widget.orderId);
      if (mounted) {
        setState(() {
          _data = data;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = shippingError(e);
          _loading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: shippingCream,
    appBar: AppBar(
      title: const Text('Order Details'),
      backgroundColor: shippingGreen,
      foregroundColor: Colors.white,
    ),
    body: SafeArea(
      child: RefreshIndicator(
        onRefresh: _load,
        child: shippingBody([
          if (_loading)
            const Center(child: CircularProgressIndicator())
          else if (_error != null)
            ShippingFeedback(_error!, retry: _load)
          else if (_data != null)
            ShippingSection(
              title: _data!['gemTitle']?.toString() ?? 'Order',
              children: [
                ShippingValue('Order ID', _data!['id']),
                ShippingValue('Order Number', _data!['orderNumber']),
                ShippingValue('Buyer', _data!['buyerName']),
                ShippingValue('Status', _data!['status']),
                ShippingValue(
                  'Value',
                  shippingMoney(
                    _data!['agreedPrice'],
                    _data!['currency']?.toString() ?? '',
                  ),
                ),
                ShippingValue('Shipping Address', _data!['shippingAddress']),
                ShippingValue('Shipping Region', _data!['shippingRegion']),
                ShippingValue(
                  'Shipping Country',
                  _data!['shippingCountryCode'],
                ),
                ShippingValue(
                  'Fulfillment Status',
                  _data!['fulfillmentStatus'],
                ),
                ShippingValue('Created', shippingDate(_data!['createdAt'])),
                if (_data!['paymentDueAt'] != null)
                  ShippingValue(
                    'Payment deadline',
                    ShipmentModel.parseDate(_data!['paymentDueAt'])?.toString(),
                  ),
                if (_data!['shipment'] is Map &&
                    (_data!['shipment'] as Map)['id'] != null)
                  TextButton(
                    onPressed: () => Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => ShipmentDetailScreen(
                          shipmentId: (_data!['shipment'] as Map)['id']
                              .toString(),
                        ),
                      ),
                    ),
                    child: const Text('View Shipment'),
                  ),
                if (_data!['statusHistory'] is List)
                  ...(_data!['statusHistory'] as List).map(
                    (e) => ShippingValue(
                      'Status History',
                      e['newStatus'] ?? e['status'],
                    ),
                  ),
              ],
            ),
        ]),
      ),
    ),
  );
}

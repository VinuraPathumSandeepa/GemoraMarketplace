import 'package:flutter/material.dart';

import '../../models/marketplace/order_model.dart';
import '../../services/marketplace_service.dart';
import 'buyer_export_tracking_screen.dart';
import 'buyer_payment_sheet.dart';

class MyOrdersScreen extends StatefulWidget {
  final MarketplaceService? service;

  const MyOrdersScreen({super.key, this.service});

  @override
  State<MyOrdersScreen> createState() => _MyOrdersScreenState();
}

class _MyOrdersScreenState extends State<MyOrdersScreen> {
  late final MarketplaceService _service;
  bool _loading = true;
  List<OrderModel> _orders = [];
  String? _error;
  String? _busyOrderId;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? MarketplaceService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      _orders = await _service.getMyOrders();
    } catch (e) {
      _error = e.toString().replaceFirst('Exception: ', '');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _cancel(OrderModel order) async {
    if (_busyOrderId != null) return;
    setState(() => _busyOrderId = order.id);
    try {
      await _service.cancelOrder(order.id);
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))),
        );
      }
    } finally {
      if (mounted) setState(() => _busyOrderId = null);
    }
  }

  Future<void> _pay(OrderModel order) async {
    final paid = await showBuyerPaymentSheet(
      context,
      order: order,
      service: _service,
    );
    if (paid && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Payment approved successfully.')),
      );
      await _load();
    }
  }

  Future<void> _confirmReceipt(OrderModel order) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Confirm delivery'),
        content: const Text(
          'Confirm only after you have received and checked your gemstone package.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Not yet'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Confirm received'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    setState(() => _busyOrderId = order.id);
    try {
      await _service.completeOrder(order.id);
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))),
        );
      }
    } finally {
      if (mounted) setState(() => _busyOrderId = null);
    }
  }

  String _statusLabel(String status) {
    const labels = {
      'AwaitingPayment': 'Awaiting payment',
      'ReadyForDispatch': 'Ready for dispatch',
      'HandedOverToCourier': 'Handed to rider',
      'InTransit': 'In transit',
      'OutForDelivery': 'Out for delivery',
      'DeliveryFailed': 'Delivery issue',
    };
    return labels[status] ?? status;
  }

  int _journeyStep(OrderModel order) {
    if (order.status == 'Completed') return 5;
    if (order.fulfillmentStatus == 'Delivered' || order.status == 'Delivered') {
      return 4;
    }
    if ({
      'HandedOverToCourier',
      'InTransit',
      'OutForDelivery',
    }.contains(order.fulfillmentStatus)) {
      return 3;
    }
    if (order.status == 'Paid' ||
        {'Preparing', 'ReadyForDispatch'}.contains(order.fulfillmentStatus)) {
      return 2;
    }
    if ({'Confirmed', 'AwaitingPayment'}.contains(order.status)) return 1;
    return 0;
  }

  String _getExportStatusDisplay(String? status) {
    if (status == null || status.isEmpty) return 'Not Required';
    switch (status.toLowerCase()) {
      case 'notrequired':
        return 'Not Required';
      case 'notstarted':
        return 'Not Started';
      case 'draft':
        return 'In Preparation';
      case 'submitted':
        return 'Submitted';
      case 'undercompliancereview':
        return 'Compliance Review';
      case 'underofficerreview':
        return 'Officer Review';
      case 'revisionrequired':
        return 'Revision Required';
      case 'approved':
        return 'Approved';
      case 'rejected':
        return 'Rejected';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return Center(child: Text(_error!));
    if (_orders.isEmpty) {
      return const Center(child: Text('You have no orders yet.'));
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 104),
        itemCount: _orders.length,
        itemBuilder: (_, index) {
          final o = _orders[index];
          final canCancel = {
            'Pending',
            'Confirmed',
            'AwaitingPayment',
          }.contains(o.status);
          final payable =
              {'Confirmed', 'AwaitingPayment'}.contains(o.status) &&
              (o.paymentDueAt == null ||
                  o.paymentDueAt!.isAfter(DateTime.now()));
          final canConfirm =
              o.status != 'Completed' &&
              (o.status == 'Delivered' || o.fulfillmentStatus == 'Delivered');
          final busy = _busyOrderId == o.id;
          final exportStatus = o.exportStatus;
          final hasTracking =
              exportStatus != null &&
              exportStatus.toLowerCase() != 'notrequired';

          return Card(
            margin: const EdgeInsets.only(bottom: 14),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
              side: BorderSide(color: Colors.grey.shade300),
            ),
            child: ExpansionTile(
              title: Text(
                o.gemTitle,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              subtitle: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SizedBox(height: 3),
                  Text(
                    '${o.orderNumber}\n${o.currency} ${o.agreedPrice.toStringAsFixed(2)} • ${_statusLabel(o.status == 'Paid' ? o.fulfillmentStatus : o.status)}',
                  ),
                  const SizedBox(height: 4),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 3,
                    ),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF0F4F2),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      'Export Clearance: ${_getExportStatusDisplay(exportStatus)}',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF10241F),
                      ),
                    ),
                  ),
                ],
              ),
              childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              children: [
                _BuyerOrderJourney(currentStep: _journeyStep(o)),
                const SizedBox(height: 12),
                if (o.shipment != null)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.local_shipping_outlined),
                    title: Text(o.shipment!.courierName),
                    subtitle: Text('Tracking: ${o.shipment!.trackingNumber}'),
                  ),
                ...o.statusHistory.map(
                  (h) => ListTile(
                    dense: true,
                    leading: const Icon(Icons.circle, size: 10),
                    title: Text(h.newStatus),
                    subtitle: Text(
                      '${h.changedByName}${h.reason == null ? '' : ' • ${h.reason}'}',
                    ),
                  ),
                ),
                ...o.fulfillmentHistory.map(
                  (h) => ListTile(
                    dense: true,
                    leading: const Icon(
                      Icons.local_shipping_outlined,
                      size: 18,
                    ),
                    title: Text(_statusLabel(h.newStatus)),
                    subtitle: Text(
                      '${h.changedByName}${h.note == null ? '' : ' • ${h.note}'}',
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    if (payable)
                      FilledButton.icon(
                        onPressed: busy ? null : () => _pay(o),
                        icon: const Icon(Icons.credit_card, size: 16),
                        label: const Text('Pay now'),
                      ),
                    if (canConfirm)
                      FilledButton.icon(
                        onPressed: busy ? null : () => _confirmReceipt(o),
                        icon: const Icon(Icons.verified_outlined, size: 16),
                        label: const Text('Confirm delivery'),
                      ),
                    if (hasTracking)
                      FilledButton.tonalIcon(
                        icon: const Icon(Icons.flight_takeoff, size: 16),
                        label: const Text('Track Export'),
                        onPressed: busy
                            ? null
                            : () {
                                Navigator.of(context).push(
                                  MaterialPageRoute(
                                    builder: (_) => BuyerExportTrackingScreen(
                                      orderId: o.id,
                                    ),
                                  ),
                                );
                              },
                      )
                    else
                      const SizedBox.shrink(),
                    if (canCancel)
                      OutlinedButton(
                        onPressed: busy ? null : () => _cancel(o),
                        child: const Text('Cancel order'),
                      ),
                    if (busy)
                      const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                  ],
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _BuyerOrderJourney extends StatelessWidget {
  final int currentStep;

  const _BuyerOrderJourney({required this.currentStep});

  @override
  Widget build(BuildContext context) {
    const labels = [
      'Placed',
      'Approved',
      'Paid',
      'Rider',
      'Delivered',
      'Confirmed',
    ];
    return Row(
      children: List.generate(labels.length, (index) {
        final active = index <= currentStep;
        return Expanded(
          child: Column(
            children: [
              Row(
                children: [
                  if (index > 0)
                    Expanded(
                      child: Container(
                        height: 2,
                        color: active
                            ? const Color(0xFFC99242)
                            : const Color(0xFFE2DED5),
                      ),
                    ),
                  Container(
                    width: 22,
                    height: 22,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: active
                          ? const Color(0xFF0B473A)
                          : const Color(0xFFE2DED5),
                    ),
                    child: active
                        ? const Icon(Icons.check, size: 14, color: Colors.white)
                        : null,
                  ),
                  if (index < labels.length - 1)
                    Expanded(
                      child: Container(
                        height: 2,
                        color: index < currentStep
                            ? const Color(0xFFC99242)
                            : const Color(0xFFE2DED5),
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 5),
              Text(
                labels[index],
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 9,
                  fontWeight: active ? FontWeight.w700 : FontWeight.w400,
                ),
              ),
            ],
          ),
        );
      }),
    );
  }
}

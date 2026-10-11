import 'package:flutter/material.dart';

import '../../models/marketplace/order_model.dart';
import '../../services/marketplace_service.dart';
import 'buyer_export_tracking_screen.dart';

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
    try {
      await _service.cancelOrder(order.id);
      await _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))),
        );
      }
    }
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
        padding: const EdgeInsets.all(16),
        itemCount: _orders.length,
        itemBuilder: (_, index) {
          final o = _orders[index];
          final canCancel = o.status == 'Pending' || o.status == 'Confirmed';
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
                    '${o.orderNumber}\n${o.currency} ${o.agreedPrice.toStringAsFixed(2)} â€¢ ${o.status}',
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
                ...o.statusHistory.map(
                  (h) => ListTile(
                    dense: true,
                    leading: const Icon(Icons.circle, size: 10),
                    title: Text(h.newStatus),
                    subtitle: Text(
                      '${h.changedByName}${h.reason == null ? '' : ' â€¢ ${h.reason}'}',
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    if (hasTracking)
                      FilledButton.tonalIcon(
                        icon: const Icon(Icons.flight_takeoff, size: 16),
                        label: const Text('Track Export'),
                        onPressed: () {
                          Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (_) =>
                                  BuyerExportTrackingScreen(orderId: o.id),
                            ),
                          );
                        },
                      )
                    else
                      const SizedBox.shrink(),
                    if (canCancel)
                      OutlinedButton(
                        onPressed: () => _cancel(o),
                        child: const Text('Cancel order'),
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

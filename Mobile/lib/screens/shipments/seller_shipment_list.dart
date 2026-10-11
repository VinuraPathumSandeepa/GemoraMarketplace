import 'package:flutter/material.dart';

import '../../models/shipment_model.dart';
import '../../services/shipment_service.dart';
import 'create_shipment_screen.dart';
import 'shipment_detail_screen.dart';
import 'shipment_widgets.dart';
import 'seller_order_inbox.dart';

class SellerShipmentList extends StatefulWidget {
  final ShipmentService? service;
  final bool buyerView;
  final bool embedded;
  const SellerShipmentList({
    super.key,
    this.service,
    this.buyerView = false,
    this.embedded = false,
  });
  @override
  State<SellerShipmentList> createState() => _SellerShipmentListState();
}

class _SellerShipmentListState extends State<SellerShipmentList> {
  late final ShipmentService _service = widget.service ?? ShipmentService();
  List<ShipmentModel> _shipments = [];
  bool _loading = true;
  String? _error;
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
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final rows = await _service.getMyShipments();
      if (mounted) {
        setState(() {
          _shipments = rows
              .map((r) => ShipmentModel.fromJson(Map<String, dynamic>.from(r)))
              .toList();
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

  Future<void> _open(Widget screen) async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
    if (mounted) await _load();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: shippingCream,
    appBar: widget.embedded
        ? null
        : AppBar(
            title: const Text(
              'My Shipments',
              style: TextStyle(
                color: shippingGold,
                fontWeight: FontWeight.bold,
              ),
            ),
            backgroundColor: shippingGreen,
            foregroundColor: Colors.white,
            actions: [
              if (!widget.buyerView)
                IconButton(
                  tooltip: 'Orders',
                  onPressed: () => showSellerOrderInbox(context),
                  icon: const Icon(Icons.notifications_outlined),
                ),
            ],
          ),
    body: SafeArea(
      child: RefreshIndicator(
        onRefresh: _load,
        child: shippingBody([
          if (!widget.embedded)
            TextButton.icon(
              onPressed: () => Navigator.maybePop(context),
              icon: const Icon(Icons.arrow_back, size: 16),
              label: Text(
                widget.buyerView ? 'Buyer Dashboard' : 'Seller Dashboard',
              ),
            ),
          const ShippingHeader(
            label: 'SECURE SHIPPING & INSURANCE',
            title: 'My Shipments',
            description: 'Manage your gemstone deliveries, follow their journey, and keep every shipment protected.',
          ),
          if (!widget.buyerView)
            Center(
              child: FilledButton.icon(
                style: FilledButton.styleFrom(backgroundColor: shippingGreen),
                onPressed: () => _open(const CreateShipmentScreen()),
                icon: const Icon(Icons.add),
                label: const Text('Create New Shipment'),
              ),
            ),
          const SizedBox(height: 24),
          if (_loading)
            const Center(child: CircularProgressIndicator())
          else if (_error != null)
            ShippingFeedback(_error!, retry: _load)
          else if (_shipments.isEmpty)
            ShippingFeedback(
              widget.buyerView
                  ? 'No shipments yet. Shipments will appear when your seller arranges delivery.'
                  : 'No shipments yet. Create a shipment for a paid order to get started.',
            )
          else
            ..._shipments.map(
              (s) => ShippingSection(
                title: 'Order: ${shortId(s.orderId)}',
                children: [
                  Align(
                    alignment: Alignment.centerLeft,
                    child: ShipmentStatusBadge(s.status),
                  ),
                  const SizedBox(height: 12),
                  ShippingValue('From', s.route('origin')),
                  ShippingValue('To', s.route('destination')),
                  ShippingValue('Value', shippingMoney(s.value, s.currency)),
                  ShippingValue('Created', shippingDate(s.data['createdAt'])),
                  Align(
                    alignment: Alignment.centerLeft,
                    child: TextButton(
                      onPressed: () =>
                          _open(ShipmentDetailScreen(shipmentId: s.id)),
                      child: const Text(
                        'View Details →',
                        style: TextStyle(color: shippingGold),
                      ),
                    ),
                  ),
                ],
              ),
            ),
        ]),
      ),
    ),
  );
}

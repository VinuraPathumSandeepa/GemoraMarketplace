import 'package:flutter/material.dart';

import '../../constants/gemora_colors.dart';
import '../../models/marketplace/order_model.dart';
import '../../services/export_compliance_service.dart';
import '../../services/marketplace_service.dart';
import '../export_compliance/export_request_detail_screen.dart';

class SellerOrdersScreen extends StatefulWidget {
  final MarketplaceService? marketplaceService;
  final ExportComplianceService? exportService;

  const SellerOrdersScreen({
    super.key,
    this.marketplaceService,
    this.exportService,
  });

  @override
  State<SellerOrdersScreen> createState() => _SellerOrdersScreenState();
}

class _SellerOrdersScreenState extends State<SellerOrdersScreen> {
  late final MarketplaceService _marketplaceService;
  late final ExportComplianceService _exportService;

  bool _loading = true;
  String? _error;
  List<OrderModel> _orders = [];
  final Set<dynamic> _startingExport = {};
  final Set<String> _busyOrders = {};

  static const Color _darkEmerald = Color(0xFF08251E);
  static const Color _gold = Color(0xFFC99242);
  static const Color _ivory = Color(0xFFFAF7F0);
  static const Color _border = Color(0xFFE7DFD2);

  @override
  void initState() {
    super.initState();
    _marketplaceService = widget.marketplaceService ?? MarketplaceService();
    _exportService = widget.exportService ?? ExportComplianceService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      _orders = await _marketplaceService.getSellerOrders();
    } catch (e) {
      _error = e.toString().replaceFirst('Exception: ', '');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _startExport(OrderModel order) async {
    setState(() => _startingExport.add(order.id));

    try {
      final created = await _exportService.createExportRequestFromOrder(
        order.id,
      );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Export clearance request initiated successfully.'),
            backgroundColor: Color(0xFF2E7D32),
          ),
        );

        Navigator.of(context)
            .push(
              MaterialPageRoute(
                builder: (_) =>
                    ExportRequestDetailScreen(requestId: created.id),
              ),
            )
            .then((_) => _load());
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString().replaceFirst('Exception: ', '')),
            backgroundColor: Colors.red.shade800,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _startingExport.remove(order.id));
    }
  }

  Future<void> _sellerAction(
    OrderModel order,
    Future<OrderModel> Function() action,
    String successMessage,
  ) async {
    if (_busyOrders.contains(order.id)) return;
    setState(() => _busyOrders.add(order.id));
    try {
      await action();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(successMessage),
            backgroundColor: const Color(0xFF2E7D32),
          ),
        );
        await _load();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString().replaceFirst('Exception: ', '')),
            backgroundColor: Colors.red.shade800,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _busyOrders.remove(order.id));
    }
  }

  Future<void> _reject(OrderModel order) async {
    final reason = TextEditingController();
    final form = GlobalKey<FormState>();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Reject order request'),
        content: Form(
          key: form,
          child: TextFormField(
            controller: reason,
            maxLength: 1000,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: 'Reason for the buyer',
              border: OutlineInputBorder(),
            ),
            validator: (value) => value == null || value.trim().isEmpty
                ? 'Please enter a reason'
                : null,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Back'),
          ),
          FilledButton(
            onPressed: () {
              if (form.currentState!.validate()) Navigator.pop(context, true);
            },
            child: const Text('Reject order'),
          ),
        ],
      ),
    );
    final value = reason.text.trim();
    await Future<void>.delayed(const Duration(milliseconds: 250));
    reason.dispose();
    if (confirmed == true && mounted) {
      await _sellerAction(
        order,
        () => _marketplaceService.rejectOrder(order.id, reason: value),
        'Order request rejected.',
      );
    }
  }

  Future<void> _handover(OrderModel order) async {
    final courier = TextEditingController();
    final tracking = TextEditingController();
    final note = TextEditingController();
    final form = GlobalKey<FormState>();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        scrollable: true,
        title: const Text('Hand over to rider'),
        content: Form(
          key: form,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: courier,
                decoration: const InputDecoration(labelText: 'Courier / rider'),
                validator: (value) => value == null || value.trim().isEmpty
                    ? 'Courier name is required'
                    : null,
              ),
              TextFormField(
                controller: tracking,
                decoration: const InputDecoration(labelText: 'Tracking number'),
                validator: (value) => value == null || value.trim().isEmpty
                    ? 'Tracking number is required'
                    : null,
              ),
              TextFormField(
                controller: note,
                decoration: const InputDecoration(labelText: 'Dispatch note'),
                maxLines: 2,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Back'),
          ),
          FilledButton(
            onPressed: () {
              if (form.currentState!.validate()) Navigator.pop(context, true);
            },
            child: const Text('Confirm handover'),
          ),
        ],
      ),
    );
    final courierName = courier.text.trim();
    final trackingNumber = tracking.text.trim();
    final dispatchNote = note.text.trim();
    await Future<void>.delayed(const Duration(milliseconds: 250));
    courier.dispose();
    tracking.dispose();
    note.dispose();
    if (confirmed == true && mounted) {
      await _sellerAction(
        order,
        () => _marketplaceService.handOverToCourier(
          order.id,
          courierName: courierName,
          trackingNumber: trackingNumber,
          dispatchNote: dispatchNote,
        ),
        'Order handed over to the rider.',
      );
    }
  }

  Widget _buildLifecycleActions(OrderModel order) {
    final busy = _busyOrders.contains(order.id);
    final buttons = <Widget>[];
    if (order.status == 'Pending') {
      buttons.addAll([
        FilledButton.icon(
          onPressed: busy
              ? null
              : () => _sellerAction(
                  order,
                  () => _marketplaceService.approveOrder(order.id),
                  'Order approved. The buyer can now pay.',
                ),
          icon: const Icon(Icons.check_circle_outline, size: 18),
          label: const Text('Approve'),
        ),
        OutlinedButton(
          onPressed: busy ? null : () => _reject(order),
          child: const Text('Reject'),
        ),
      ]);
    } else if (order.status == 'Paid' && order.fulfillmentStatus == 'Pending') {
      buttons.add(
        FilledButton.icon(
          onPressed: busy
              ? null
              : () => _sellerAction(
                  order,
                  () => _marketplaceService.startPreparing(order.id),
                  'Gemstone preparation started.',
                ),
          icon: const Icon(Icons.inventory_2_outlined, size: 18),
          label: const Text('Start preparing'),
        ),
      );
    } else if (order.fulfillmentStatus == 'Preparing') {
      buttons.add(
        FilledButton.icon(
          onPressed: busy
              ? null
              : () => _sellerAction(
                  order,
                  () => _marketplaceService.markReadyForDispatch(order.id),
                  'Order is ready for dispatch.',
                ),
          icon: const Icon(Icons.inventory_outlined, size: 18),
          label: const Text('Ready for dispatch'),
        ),
      );
    } else if (order.fulfillmentStatus == 'ReadyForDispatch') {
      buttons.add(
        FilledButton.icon(
          onPressed: busy ? null : () => _handover(order),
          icon: const Icon(Icons.delivery_dining_outlined, size: 18),
          label: const Text('Hand over to rider'),
        ),
      );
    } else if (order.fulfillmentStatus == 'HandedOverToCourier') {
      buttons.add(
        FilledButton(
          onPressed: busy
              ? null
              : () => _sellerAction(
                  order,
                  () => _marketplaceService.markInTransit(order.id),
                  'Shipment is in transit.',
                ),
          child: const Text('Mark in transit'),
        ),
      );
    } else if (order.fulfillmentStatus == 'InTransit') {
      buttons.add(
        FilledButton(
          onPressed: busy
              ? null
              : () => _sellerAction(
                  order,
                  () => _marketplaceService.markOutForDelivery(order.id),
                  'Shipment is out for delivery.',
                ),
          child: const Text('Out for delivery'),
        ),
      );
    } else if (order.fulfillmentStatus == 'OutForDelivery') {
      buttons.add(
        FilledButton.icon(
          onPressed: busy
              ? null
              : () => _sellerAction(
                  order,
                  () => _marketplaceService.markDelivered(order.id),
                  'Order marked delivered. Waiting for buyer confirmation.',
                ),
          icon: const Icon(Icons.task_alt, size: 18),
          label: const Text('Mark delivered'),
        ),
      );
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFF3F7F5),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFD8E5DF)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Order & Delivery Workflow',
            style: TextStyle(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 5),
          Text(
            'Order: ${_formatStatusLabel(order.status)} · Delivery: ${_formatStatusLabel(order.fulfillmentStatus)}',
          ),
          if (order.shipment != null) ...[
            const SizedBox(height: 5),
            Text(
              '${order.shipment!.courierName} · ${order.shipment!.trackingNumber}',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ],
          if (buttons.isNotEmpty) ...[
            const SizedBox(height: 12),
            Wrap(spacing: 8, runSpacing: 8, children: buttons),
          ] else ...[
            const SizedBox(height: 8),
            const Text(
              'No seller action is required at this stage.',
              style: TextStyle(color: Colors.black54),
            ),
          ],
          if (busy) ...[
            const SizedBox(height: 10),
            const LinearProgressIndicator(),
          ],
        ],
      ),
    );
  }

  Color _getStatusColor(String? status) {
    return GemoraColors.getStatusColor(status);
  }

  String _formatStatusLabel(String? status) {
    if (status == null || status.isEmpty) return 'Not Started';
    switch (status.toLowerCase()) {
      case 'undercompliancereview':
        return 'Compliance Review';
      case 'underofficerreview':
        return 'Officer Review';
      case 'revisionrequired':
        return 'Revision Required';
      case 'notrequired':
        return 'Not Required';
      case 'notstarted':
        return 'Not Started';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        title: const Text('Orders & Export Clearance'),
        backgroundColor: _darkEmerald,
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _load,
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator(color: _gold));
    }

    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 54, color: Colors.red),
              const SizedBox(height: 16),
              Text(
                _error!,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 15),
              ),
              const SizedBox(height: 20),
              ElevatedButton.icon(
                onPressed: _load,
                icon: const Icon(Icons.refresh),
                label: const Text('Try Again'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: _darkEmerald,
                  foregroundColor: Colors.white,
                ),
              ),
            ],
          ),
        ),
      );
    }

    if (_orders.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.inbox_outlined, size: 64, color: Colors.grey.shade400),
              const SizedBox(height: 16),
              const Text(
                'No Orders Received',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                'Buyer orders will appear here for order processing and export clearance.',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 14, color: Colors.grey.shade600),
              ),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      color: _gold,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _orders.length,
        itemBuilder: (context, index) {
          final order = _orders[index];
          final exportStatus = order.exportStatus ?? 'NotStarted';
          final isNotStarted = exportStatus.toLowerCase() == 'notstarted';
          final isDomestic = exportStatus.toLowerCase() == 'notrequired';
          final hasExport =
              !isNotStarted && !isDomestic && order.exportRequestId != null;
          final isStarting = _startingExport.contains(order.id);
          final statusColor = _getStatusColor(exportStatus);

          return Card(
            margin: const EdgeInsets.only(bottom: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
              side: const BorderSide(color: _border),
            ),
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          order.gemTitle,
                          style: const TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF10241F),
                          ),
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: const Color(0xFFF0F4F2),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          order.status,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: _darkEmerald,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${order.orderNumber} • Buyer: ${order.buyerName}',
                    style: TextStyle(fontSize: 13, color: Colors.grey.shade700),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${order.currency} ${order.agreedPrice.toStringAsFixed(2)}',
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: _gold,
                    ),
                  ),
                  const SizedBox(height: 16),
                  _buildLifecycleActions(order),
                  const Divider(height: 24),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF8F9FA),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.grey.shade200),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Export Clearance',
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF10241F),
                          ),
                        ),
                        const SizedBox(height: 8),
                        Row(
                          children: [
                            const Text(
                              'Destination: ',
                              style: TextStyle(
                                fontSize: 13,
                                color: Colors.black87,
                              ),
                            ),
                            Text(
                              order.shippingCountryCode ?? 'International',
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            const Text(
                              'Status: ',
                              style: TextStyle(
                                fontSize: 13,
                                color: Colors.black87,
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 8,
                                vertical: 2,
                              ),
                              decoration: BoxDecoration(
                                color: statusColor.withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(
                                _formatStatusLabel(exportStatus),
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                  color: statusColor,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        if (isDomestic)
                          const Row(
                            children: [
                              Icon(
                                Icons.check_circle_outline,
                                size: 16,
                                color: GemoraColors.statusDraft,
                              ),
                              SizedBox(width: 6),
                              Text(
                                'Export clearance not required',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: GemoraColors.mutedText,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          )
                        else if (isNotStarted) ...[
                          const Row(
                            children: [
                              Icon(
                                Icons.info_outline,
                                size: 16,
                                color: GemoraColors.darkGold,
                              ),
                              SizedBox(width: 6),
                              Text(
                                'Export clearance required',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: GemoraColors.darkGold,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 10),
                          ElevatedButton.icon(
                            onPressed: isStarting
                                ? null
                                : () => _startExport(order),
                            icon: isStarting
                                ? const SizedBox(
                                    width: 16,
                                    height: 16,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: Colors.white,
                                    ),
                                  )
                                : const Icon(Icons.flight_takeoff, size: 16),
                            label: Text(
                              isStarting
                                  ? 'Starting...'
                                  : 'Start Export Clearance',
                            ),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: _darkEmerald,
                              foregroundColor: Colors.white,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(10),
                              ),
                            ),
                          ),
                        ] else if (hasExport)
                          OutlinedButton.icon(
                            onPressed: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => ExportRequestDetailScreen(
                                    requestId: order.exportRequestId!,
                                  ),
                                ),
                              );
                            },
                            icon: const Icon(
                              Icons.visibility_outlined,
                              size: 16,
                            ),
                            label: const Text('View Export Clearance'),
                            style: OutlinedButton.styleFrom(
                              foregroundColor: _darkEmerald,
                              side: const BorderSide(color: _darkEmerald),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(10),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

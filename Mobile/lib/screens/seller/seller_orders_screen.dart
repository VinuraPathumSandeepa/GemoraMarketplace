import 'package:flutter/material.dart';

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
      final created = await _exportService.createExportRequestFromOrder(order.id);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Export clearance request initiated successfully.'),
            backgroundColor: Color(0xFF2E7D32),
          ),
        );

        Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => ExportRequestDetailScreen(requestId: created.id),
          ),
        ).then((_) => _load());
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

  Color _getStatusColor(String? status) {
    switch (status?.toLowerCase()) {
      case 'approved':
        return const Color(0xFF2E7D32);
      case 'submitted':
        return const Color(0xFF1976D2);
      case 'undercompliancereview':
        return const Color(0xFF7B1FA2);
      case 'underofficerreview':
        return const Color(0xFFE65100);
      case 'revisionrequired':
        return const Color(0xFFF57F17);
      case 'rejected':
        return const Color(0xFFC62828);
      case 'draft':
        return Colors.blueGrey;
      default:
        return Colors.grey.shade700;
    }
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
          final hasExport = !isNotStarted && !isDomestic && order.exportRequestId != null;
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
                    style: TextStyle(
                      fontSize: 13,
                      color: Colors.grey.shade700,
                    ),
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
                              style: TextStyle(fontSize: 13, color: Colors.black87),
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
                              style: TextStyle(fontSize: 13, color: Colors.black87),
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
                        if (isNotStarted)
                          ElevatedButton.icon(
                            onPressed: isStarting ? null : () => _startExport(order),
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
                            label: Text(isStarting
                                ? 'Starting...'
                                : 'Start Export Request'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: _darkEmerald,
                              foregroundColor: Colors.white,
                            ),
                          )
                        else if (hasExport)
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
                            icon: const Icon(Icons.visibility_outlined, size: 16),
                            label: const Text('View Export Request'),
                            style: OutlinedButton.styleFrom(
                              foregroundColor: _darkEmerald,
                              side: const BorderSide(color: _darkEmerald),
                            ),
                          )
                        else if (isDomestic)
                          Text(
                            'Domestic order — export clearance not required.',
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey.shade600,
                              fontStyle: FontStyle.italic,
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

import 'package:flutter/material.dart';

import '../../models/buyer_export_tracking_model.dart';
import '../../services/export_compliance_service.dart';

class BuyerExportTrackingScreen extends StatefulWidget {
  final dynamic orderId;
  final ExportComplianceService? service;

  const BuyerExportTrackingScreen({
    super.key,
    this.orderId,
    this.service,
  });

  @override
  State<BuyerExportTrackingScreen> createState() =>
      _BuyerExportTrackingScreenState();
}

class _BuyerExportTrackingScreenState extends State<BuyerExportTrackingScreen> {
  late final ExportComplianceService _service;
  bool _loading = true;
  String? _error;
  List<BuyerExportTrackingModel> _items = [];

  static const Color _darkEmerald = Color(0xFF08251E);
  static const Color _gold = Color(0xFFC99242);
  static const Color _ivory = Color(0xFFFAF7F0);
  static const Color _border = Color(0xFFE7DFD2);

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? ExportComplianceService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      if (widget.orderId != null) {
        final item = await _service.getOrderExportTracking(widget.orderId);
        _items = [item];
      } else {
        _items = await _service.getMyExportTracking();
      }
    } catch (e) {
      _error = e.toString().replaceFirst('Exception: ', '');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
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
      case 'notrequired':
        return const Color(0xFF546E7A);
      case 'notstarted':
      case 'draft':
      default:
        return const Color(0xFF78909C);
    }
  }

  String _formatStatusLabel(String status) {
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
      case 'draft':
        return 'In Preparation';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final title = widget.orderId != null ? 'Order Export Tracking' : 'Track My Exports';

    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        title: Text(title),
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
      return const Center(
        child: CircularProgressIndicator(color: _gold),
      );
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
                style: const TextStyle(fontSize: 15, color: Colors.black87),
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

    if (_items.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.flight_takeoff, size: 64, color: Colors.grey.shade400),
              const SizedBox(height: 18),
              const Text(
                'No Export Clearances Found',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                'Purchased international orders requiring export clearance will appear here.',
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
        itemCount: _items.length,
        itemBuilder: (context, index) {
          final item = _items[index];
          final statusColor = _getStatusColor(item.status);

          return Card(
            margin: const EdgeInsets.only(bottom: 16),
            elevation: 2,
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
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        width: 44,
                        height: 44,
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Icon(
                          item.isApproved
                              ? Icons.verified
                              : Icons.flight_takeoff,
                          color: statusColor,
                          size: 24,
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              item.gemTitle,
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                                color: Color(0xFF10241F),
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              item.orderNumber,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                                color: Colors.grey.shade700,
                              ),
                            ),
                          ],
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: statusColor.withValues(alpha: 0.3)),
                        ),
                        child: Text(
                          _formatStatusLabel(item.status),
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: statusColor,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const Divider(height: 24),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF6F8F7),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(
                          Icons.info_outline,
                          size: 18,
                          color: Colors.grey.shade700,
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            item.stageDescription,
                            style: const TextStyle(
                              fontSize: 13,
                              color: Color(0xFF2D3748),
                              height: 1.4,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Expanded(
                        child: _buildMetaCol(
                          'Route',
                          '${item.originCountry} → ${item.destinationCountry}',
                        ),
                      ),
                      if (item.submittedAt != null)
                        Expanded(
                          child: _buildMetaCol(
                            'Submitted',
                            '${item.submittedAt!.year}-${item.submittedAt!.month.toString().padLeft(2, '0')}-${item.submittedAt!.day.toString().padLeft(2, '0')}',
                          ),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildMetaCol(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label.toUpperCase(),
          style: TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.w700,
            letterSpacing: 0.8,
            color: Colors.grey.shade600,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          value,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: Color(0xFF10241F),
          ),
        ),
      ],
    );
  }
}

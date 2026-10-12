import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/gemora_colors.dart';
import '../../models/export_request_model.dart';
import '../../providers/auth_provider.dart';
import '../../services/export_compliance_service.dart';
import '../seller/seller_orders_screen.dart';
import 'export_request_detail_screen.dart';

class MyExportRequestsScreen extends StatefulWidget {
  final ExportComplianceService? exportService;

  const MyExportRequestsScreen({super.key, this.exportService});

  @override
  State<MyExportRequestsScreen> createState() => _MyExportRequestsScreenState();
}

class _MyExportRequestsScreenState extends State<MyExportRequestsScreen> {
  late final ExportComplianceService _exportService;

  Future<List<ExportRequestModel>>? _requestsFuture;
  String? _loadedUserId;

  @override
  void initState() {
    super.initState();
    _exportService = widget.exportService ?? ExportComplianceService();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final user = Provider.of<AuthProvider>(context).user;
    if (user != null) {
      final isBuyer = user.role.trim().toLowerCase() == 'buyer';
      if (!isBuyer && (_requestsFuture == null || _loadedUserId != user.id)) {
        _loadedUserId = user.id;
        _loadRequests();
      }
    }
  }

  void _loadRequests() {
    setState(() {
      _requestsFuture = _exportService.getMyExportRequests();
    });
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    final months = [
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
    final month = months[local.month - 1];
    final day = local.day;
    final year = local.year;
    return '$month $day, $year';
  }

  Widget _buildBuyerPlaceholder() {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Container(
          decoration: BoxDecoration(
            color: GemoraColors.white,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: GemoraColors.border),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.04),
                blurRadius: 16,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 72,
                height: 72,
                decoration: BoxDecoration(
                  color: GemoraColors.cream,
                  borderRadius: BorderRadius.circular(36),
                  border: Border.all(color: GemoraColors.border),
                ),
                child: const Icon(
                  Icons.local_shipping_outlined,
                  size: 38,
                  color: GemoraColors.darkGold,
                ),
              ),
              const SizedBox(height: 20),
              const Text(
                'Track My Exports',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.w800,
                  color: GemoraColors.darkGreen,
                  letterSpacing: 0.5,
                ),
              ),
              const SizedBox(height: 12),
              const Text(
                'Export tracking will appear here for purchases linked to your account.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: GemoraColors.deepGreen,
                  height: 1.5,
                ),
              ),
              const SizedBox(height: 12),
              const Text(
                'Buyer export tracking is automatically updated as compliance officers and sellers complete clearance.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 12,
                  color: GemoraColors.mutedText,
                  height: 1.4,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;

    if (user == null) {
      return Scaffold(
        backgroundColor: GemoraColors.cream,
        appBar: AppBar(
          backgroundColor: GemoraColors.darkGreen,
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text(
            'Export Compliance',
            style: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 0.8),
          ),
        ),
        body: const Center(
          child: CircularProgressIndicator(color: GemoraColors.darkGold),
        ),
      );
    }

    final isBuyer = user.role.trim().toLowerCase() == 'buyer';

    if (isBuyer) {
      return Scaffold(
        backgroundColor: GemoraColors.cream,
        appBar: AppBar(
          backgroundColor: GemoraColors.darkGreen,
          foregroundColor: Colors.white,
          elevation: 0,
          title: const Text(
            'Track My Exports',
            style: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 0.8),
          ),
        ),
        body: _buildBuyerPlaceholder(),
      );
    }

    return Scaffold(
      backgroundColor: GemoraColors.cream,
      appBar: AppBar(
        backgroundColor: GemoraColors.darkGreen,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'My Export Requests',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w800,
                letterSpacing: 0.8,
              ),
            ),
            Text(
              'Export & Compliance',
              style: TextStyle(fontSize: 11, color: Colors.white70),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _loadRequests,
          ),
        ],
      ),
      body: _requestsFuture == null
          ? const Center(
              child: CircularProgressIndicator(color: GemoraColors.darkGold),
            )
          : FutureBuilder<List<ExportRequestModel>>(
              future: _requestsFuture,
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const Center(
                    child: CircularProgressIndicator(
                      color: GemoraColors.darkGold,
                    ),
                  );
                }

                if (snapshot.hasError) {
                  return Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Container(
                        padding: const EdgeInsets.all(24),
                        decoration: BoxDecoration(
                          color: GemoraColors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: GemoraColors.border),
                        ),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(
                              Icons.error_outline,
                              size: 56,
                              color: GemoraColors.statusRejected,
                            ),
                            const SizedBox(height: 16),
                            Text(
                              snapshot.error.toString().replaceAll(
                                'Exception: ',
                                '',
                              ),
                              textAlign: TextAlign.center,
                              style: const TextStyle(
                                fontSize: 14,
                                color: GemoraColors.deepGreen,
                              ),
                            ),
                            const SizedBox(height: 20),
                            ElevatedButton.icon(
                              onPressed: _loadRequests,
                              icon: const Icon(Icons.refresh, size: 18),
                              label: const Text('Retry'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: GemoraColors.darkGreen,
                                foregroundColor: Colors.white,
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(10),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  );
                }

                final requests = snapshot.data ?? [];

                if (requests.isEmpty) {
                  return Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Container(
                        padding: const EdgeInsets.all(28),
                        decoration: BoxDecoration(
                          color: GemoraColors.white,
                          borderRadius: BorderRadius.circular(18),
                          border: Border.all(color: GemoraColors.border),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.04),
                              blurRadius: 16,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Container(
                              width: 68,
                              height: 68,
                              decoration: BoxDecoration(
                                color: GemoraColors.cream,
                                borderRadius: BorderRadius.circular(34),
                                border: Border.all(color: GemoraColors.border),
                              ),
                              child: const Icon(
                                Icons.flight_takeoff,
                                size: 36,
                                color: GemoraColors.darkGold,
                              ),
                            ),
                            const SizedBox(height: 20),
                            const Text(
                              'Export clearance starts from an international order.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w700,
                                color: GemoraColors.darkGreen,
                                height: 1.4,
                              ),
                            ),
                            const SizedBox(height: 10),
                            const Text(
                              'Select an international order from your orders list to initiate export clearance.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 13,
                                color: GemoraColors.mutedText,
                                height: 1.4,
                              ),
                            ),
                            const SizedBox(height: 24),
                            ElevatedButton.icon(
                              onPressed: () {
                                Navigator.of(context)
                                    .push(
                                      MaterialPageRoute(
                                        builder: (_) =>
                                            const SellerOrdersScreen(),
                                      ),
                                    )
                                    .then((_) => _loadRequests());
                              },
                              icon: const Icon(Icons.receipt_long, size: 18),
                              label: const Text('View Orders'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: GemoraColors.darkGreen,
                                foregroundColor: Colors.white,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 24,
                                  vertical: 13,
                                ),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  );
                }

                return RefreshIndicator(
                  onRefresh: () async => _loadRequests(),
                  color: GemoraColors.darkGold,
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 20,
                      vertical: 16,
                    ),
                    itemCount: requests.length,
                    itemBuilder: (context, index) {
                      final req = requests[index];
                      final statusColor = GemoraColors.getStatusColor(
                        req.status,
                      );
                      final isLegacy = req.orderId == null;

                      return Card(
                        margin: const EdgeInsets.only(bottom: 16),
                        elevation: 0,
                        color: GemoraColors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                          side: const BorderSide(color: GemoraColors.border),
                        ),
                        child: InkWell(
                          borderRadius: BorderRadius.circular(16),
                          onTap: () {
                            Navigator.of(context)
                                .push(
                                  MaterialPageRoute(
                                    builder: (_) => ExportRequestDetailScreen(
                                      requestId: req.id,
                                    ),
                                  ),
                                )
                                .then((_) => _loadRequests());
                          },
                          child: Padding(
                            padding: const EdgeInsets.all(20),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                // Top row: Route and Status chip
                                Row(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Expanded(
                                      child: Row(
                                        children: [
                                          const Icon(
                                            Icons.flight_takeoff,
                                            size: 18,
                                            color: GemoraColors.darkGold,
                                          ),
                                          const SizedBox(width: 8),
                                          Expanded(
                                            child: Text(
                                              '${req.originCountry} → ${req.destinationCountry}',
                                              style: const TextStyle(
                                                fontWeight: FontWeight.w800,
                                                fontSize: 16,
                                                color: GemoraColors.deepGreen,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                    const SizedBox(width: 10),
                                    Container(
                                      padding: const EdgeInsets.symmetric(
                                        horizontal: 10,
                                        vertical: 5,
                                      ),
                                      decoration: BoxDecoration(
                                        color: statusColor.withValues(
                                          alpha: 0.12,
                                        ),
                                        borderRadius: BorderRadius.circular(10),
                                        border: Border.all(
                                          color: statusColor.withValues(
                                            alpha: 0.35,
                                          ),
                                        ),
                                      ),
                                      child: Text(
                                        GemoraColors.getStatusLabel(req.status),
                                        style: TextStyle(
                                          fontSize: 11,
                                          fontWeight: FontWeight.w800,
                                          color: statusColor,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 10),

                                // Linked Order / Gem details or Legacy badge
                                if (!isLegacy) ...[
                                  if (req.gemListingTitle != null) ...[
                                    Row(
                                      children: [
                                        const Icon(
                                          Icons.diamond_outlined,
                                          size: 14,
                                          color: GemoraColors.darkGold,
                                        ),
                                        const SizedBox(width: 6),
                                        Expanded(
                                          child: Text(
                                            req.gemListingTitle!,
                                            style: const TextStyle(
                                              fontSize: 13,
                                              fontWeight: FontWeight.w700,
                                              color: GemoraColors.deepGreen,
                                            ),
                                            maxLines: 1,
                                            overflow: TextOverflow.ellipsis,
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 6),
                                  ],
                                  Row(
                                    children: [
                                      const Icon(
                                        Icons.receipt_long_outlined,
                                        size: 14,
                                        color: GemoraColors.mutedText,
                                      ),
                                      const SizedBox(width: 6),
                                      Text(
                                        req.orderNumber ?? 'Order Linked',
                                        style: const TextStyle(
                                          fontSize: 12,
                                          fontWeight: FontWeight.w600,
                                          color: GemoraColors.mutedText,
                                        ),
                                      ),
                                    ],
                                  ),
                                ] else ...[
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 8,
                                      vertical: 3,
                                    ),
                                    decoration: BoxDecoration(
                                      color: GemoraColors.cream,
                                      borderRadius: BorderRadius.circular(6),
                                      border: Border.all(
                                        color: GemoraColors.border,
                                      ),
                                    ),
                                    child: const Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Icon(
                                          Icons.history,
                                          size: 12,
                                          color: GemoraColors.mutedText,
                                        ),
                                        SizedBox(width: 4),
                                        Text(
                                          'Legacy Request',
                                          style: TextStyle(
                                            fontSize: 11,
                                            color: GemoraColors.mutedText,
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ],

                                const SizedBox(height: 14),
                                const Divider(
                                  color: GemoraColors.border,
                                  height: 1,
                                ),
                                const SizedBox(height: 14),

                                // Bottom row: Declared Value & Date
                                Row(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  children: [
                                    Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        const Text(
                                          'DECLARED VALUE',
                                          style: TextStyle(
                                            fontSize: 10,
                                            fontWeight: FontWeight.w700,
                                            color: GemoraColors.mutedText,
                                            letterSpacing: 0.6,
                                          ),
                                        ),
                                        const SizedBox(height: 2),
                                        Text(
                                          '${req.currency} ${req.declaredValue.toStringAsFixed(2)}',
                                          style: const TextStyle(
                                            fontSize: 15,
                                            fontWeight: FontWeight.w800,
                                            color: GemoraColors.darkGold,
                                          ),
                                        ),
                                      ],
                                    ),
                                    Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.end,
                                      children: [
                                        const Text(
                                          'DATE',
                                          style: TextStyle(
                                            fontSize: 10,
                                            fontWeight: FontWeight.w700,
                                            color: GemoraColors.mutedText,
                                            letterSpacing: 0.6,
                                          ),
                                        ),
                                        const SizedBox(height: 2),
                                        Row(
                                          mainAxisSize: MainAxisSize.min,
                                          children: [
                                            const Icon(
                                              Icons.calendar_today_outlined,
                                              size: 11,
                                              color: GemoraColors.mutedText,
                                            ),
                                            const SizedBox(width: 4),
                                            Text(
                                              _formatDate(req.createdAt),
                                              style: const TextStyle(
                                                fontSize: 12,
                                                color: GemoraColors.deepGreen,
                                                fontWeight: FontWeight.w600,
                                              ),
                                            ),
                                          ],
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                );
              },
            ),
    );
  }
}

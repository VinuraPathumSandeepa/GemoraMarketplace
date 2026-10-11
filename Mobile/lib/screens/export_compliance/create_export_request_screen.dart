import 'package:flutter/material.dart';

import '../../constants/gemora_colors.dart';
import '../../services/export_compliance_service.dart';
import '../seller/seller_orders_screen.dart';
import 'export_request_detail_screen.dart';

class CreateExportRequestScreen extends StatefulWidget {
  final String? orderId;
  final String? orderNumber;
  final String? destinationCountry;
  final double? declaredValue;
  final String? currency;
  final String? gemTitle;

  const CreateExportRequestScreen({
    super.key,
    this.orderId,
    this.orderNumber,
    this.destinationCountry,
    this.declaredValue,
    this.currency,
    this.gemTitle,
  });

  @override
  State<CreateExportRequestScreen> createState() =>
      _CreateExportRequestScreenState();
}

class _CreateExportRequestScreenState extends State<CreateExportRequestScreen> {
  final _exportService = ExportComplianceService();
  bool _isSubmitting = false;
  String? _errorMessage;

  Future<void> _submitOrderExport() async {
    if (widget.orderId == null || _isSubmitting) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final newRequest = await _exportService.createExportRequestFromOrder(
        widget.orderId!,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Export clearance request initiated successfully.'),
          backgroundColor: Color(0xFF2E7D32),
        ),
      );

      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => ExportRequestDetailScreen(requestId: newRequest.id),
        ),
      );
    } catch (err) {
      setState(() {
        _errorMessage = err.toString().replaceAll('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final hasOrder = widget.orderId != null;

    return Scaffold(
      backgroundColor: GemoraColors.cream,
      appBar: AppBar(
        backgroundColor: GemoraColors.darkGreen,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Text(
          'Start Export Clearance',
          style: TextStyle(fontWeight: FontWeight.w800, letterSpacing: 0.8),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(22),
        child: hasOrder ? _buildOrderBoundForm() : _buildStandaloneNotice(),
      ),
    );
  }

  Widget _buildStandaloneNotice() {
    return Container(
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
              Icons.receipt_long,
              size: 36,
              color: GemoraColors.darkGold,
            ),
          ),
          const SizedBox(height: 20),
          const Text(
            'Order Required for Export Clearance',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w800,
              color: GemoraColors.darkGreen,
            ),
          ),
          const SizedBox(height: 12),
          const Text(
            'Export clearance requests must be started from an eligible international marketplace order. Standalone export requests without an order are not permitted.',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 14,
              color: GemoraColors.deepGreen,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 24),
          ElevatedButton.icon(
            onPressed: () {
              Navigator.of(context).pushReplacement(
                MaterialPageRoute(builder: (_) => const SellerOrdersScreen()),
              );
            },
            icon: const Icon(Icons.arrow_forward, size: 18),
            label: const Text('View Seller Orders'),
            style: ElevatedButton.styleFrom(
              backgroundColor: GemoraColors.darkGreen,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildOrderBoundForm() {
    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        color: GemoraColors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: GemoraColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_errorMessage != null) ...[
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.red.shade50,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: Colors.red.shade200),
              ),
              child: Text(
                _errorMessage!,
                style: TextStyle(color: Colors.red.shade800, fontSize: 13),
              ),
            ),
            const SizedBox(height: 16),
          ],
          const Text(
            'Order Information',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w800,
              color: GemoraColors.darkGreen,
            ),
          ),
          const Divider(height: 20, color: GemoraColors.border),
          _readOnlyRow(
            'Order Reference',
            widget.orderNumber ?? widget.orderId!,
          ),
          if (widget.gemTitle != null)
            _readOnlyRow('Gemstone Listing', widget.gemTitle!),
          _readOnlyRow('Origin Country', 'Sri Lanka (Fixed)'),
          _readOnlyRow(
            'Destination Country',
            widget.destinationCountry ?? 'From Order',
          ),
          if (widget.declaredValue != null)
            _readOnlyRow(
              'Declared Value',
              '${widget.currency ?? 'USD'} ${widget.declaredValue!.toStringAsFixed(2)}',
            ),
          const SizedBox(height: 24),
          ElevatedButton.icon(
            onPressed: _isSubmitting ? null : _submitOrderExport,
            icon: _isSubmitting
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Icon(Icons.flight_takeoff, size: 18),
            label: Text(
              _isSubmitting
                  ? 'Initiating Clearance...'
                  : 'Start Export Clearance',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: GemoraColors.darkGreen,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(vertical: 14),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _readOnlyRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 140,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 13,
                color: GemoraColors.mutedText,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontSize: 14,
                color: GemoraColors.deepGreen,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

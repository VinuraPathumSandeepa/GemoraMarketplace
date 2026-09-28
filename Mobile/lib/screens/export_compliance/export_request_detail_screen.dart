import 'package:flutter/material.dart';

import '../../models/export_request_model.dart';
import '../../services/export_compliance_service.dart';

class ExportRequestDetailScreen extends StatefulWidget {
  final String requestId;

  const ExportRequestDetailScreen({
    super.key,
    required this.requestId,
  });

  @override
  State<ExportRequestDetailScreen> createState() =>
      _ExportRequestDetailScreenState();
}

class _ExportRequestDetailScreenState
    extends State<ExportRequestDetailScreen> {
  final _exportService = ExportComplianceService();

  late Future<ExportRequestModel> _detailFuture;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  void _loadDetail() {
    setState(() {
      _detailFuture = _exportService.getExportRequestById(widget.requestId);
    });
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
        return Colors.blueGrey;
      case 'submitted':
        return Colors.blue;
      case 'undercompliancereview':
        return Colors.purple;
      case 'underofficerreview':
        return Colors.orange;
      case 'revisionrequired':
        return Colors.amber.shade800;
      case 'approved':
        return Colors.green;
      case 'rejected':
        return Colors.red;
      case 'cancelled':
        return Colors.grey;
      default:
        return Colors.blueGrey;
    }
  }

  String _getStatusExplanation(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
        return 'Complete your export request before submitting.';
      case 'submitted':
        return 'Your request has been submitted for compliance processing.';
      case 'undercompliancereview':
        return 'Automated compliance review is in progress or complete.';
      case 'underofficerreview':
        return 'An Export Officer is reviewing your request.';
      case 'revisionrequired':
        return 'Changes are required before the request can be resubmitted.';
      case 'approved':
        return 'Your export compliance request was approved by an Export Officer.';
      case 'rejected':
        return 'Your export compliance request was rejected.';
      case 'cancelled':
        return 'This export request was cancelled.';
      default:
        return 'Status: $status';
    }
  }

  Widget _buildDetailRow(String label, String value) {
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
                fontWeight: FontWeight.w600,
                color: Colors.grey,
                fontSize: 14,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Export Request Details'),
      ),
      body: FutureBuilder<ExportRequestModel>(
        future: _detailFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.error_outline,
                        size: 60, color: Colors.red),
                    const SizedBox(height: 16),
                    Text(
                      snapshot.error.toString().replaceAll('Exception: ', ''),
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 16),
                    ),
                    const SizedBox(height: 20),
                    FilledButton.icon(
                      onPressed: _loadDetail,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            );
          }

          final req = snapshot.data!;
          final statusColor = _getStatusColor(req.status);
          final explanation = _getStatusExplanation(req.status);

          return SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // STATUS CARD
                Card(
                  elevation: 2,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Text(
                              'Current Status',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            Chip(
                              label: Text(
                                req.status,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              backgroundColor: statusColor,
                              padding: const EdgeInsets.symmetric(horizontal: 8),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Text(
                          explanation,
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.grey.shade800,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 20),

                // DETAILS CARD
                Card(
                  elevation: 1,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Request Information',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const Divider(height: 24),
                        _buildDetailRow('Request ID', req.id),
                        _buildDetailRow('Origin Country', req.originCountry),
                        _buildDetailRow('Destination', req.destinationCountry),
                        _buildDetailRow(
                          'Declared Value',
                          '${req.declaredValue.toStringAsFixed(2)} ${req.currency}',
                        ),
                        _buildDetailRow(
                          'Purpose',
                          req.purpose != null && req.purpose!.isNotEmpty
                              ? req.purpose!
                              : 'None specified',
                        ),
                        _buildDetailRow(
                          'Created At',
                          req.createdAt.toLocal().toString().split('.')[0],
                        ),
                        if (req.submittedAt != null)
                          _buildDetailRow(
                            'Submitted At',
                            req.submittedAt!.toLocal().toString().split('.')[0],
                          ),
                        _buildDetailRow(
                          'Updated At',
                          req.updatedAt.toLocal().toString().split('.')[0],
                        ),
                        if (req.reviewedAt != null)
                          _buildDetailRow(
                            'Reviewed At',
                            req.reviewedAt!.toLocal().toString().split('.')[0],
                          ),
                        if (req.reviewNotes != null &&
                            req.reviewNotes!.isNotEmpty)
                          _buildDetailRow('Review Notes', req.reviewNotes!),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 20),

                // PHASE 1 NEXT STEPS INFO BOX
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.blue.shade50,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: Colors.blue.shade200),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.info_outline, color: Colors.blue.shade700),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          'Documents and submission will be available in the next step.',
                          style: TextStyle(
                            color: Colors.blue.shade900,
                            fontSize: 14,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

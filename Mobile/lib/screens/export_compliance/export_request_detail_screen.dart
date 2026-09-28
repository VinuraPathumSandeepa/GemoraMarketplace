import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../../models/compliance_document_model.dart';
import '../../models/export_request_model.dart';
import '../../services/export_compliance_service.dart';
import 'add_compliance_document_dialog.dart';

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

  late Future<ExportRequestModel> _requestFuture;
  late Future<List<ComplianceDocumentModel>> _documentsFuture;

  bool _isUploadingFile = false;
  String? _uploadingDocId;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  void _loadData() {
    setState(() {
      _requestFuture = _exportService.getExportRequestById(widget.requestId);
      _documentsFuture = _exportService.getComplianceDocuments(widget.requestId);
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

  Color _getDocStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'valid':
        return Colors.green;
      case 'invalid':
        return Colors.red;
      case 'expired':
        return Colors.red.shade700;
      case 'requiresreview':
        return Colors.orange;
      case 'pending':
      default:
        return Colors.blue;
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

  bool _isEditableStatus(String status) {
    final lower = status.toLowerCase();
    return lower == 'draft' || lower == 'revisionrequired';
  }

  Future<void> _openAddDocumentDialog() async {
    final result = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) => AddComplianceDocumentDialog(
        exportRequestId: widget.requestId,
      ),
    );

    if (result == true) {
      _loadData();
    }
  }

  Future<void> _uploadFileForExistingDoc(ComplianceDocumentModel doc) async {
    if (_isUploadingFile) return;

    try {
      final files = await FilePicker.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
      );

      if (files.isNotEmpty) {
        final file = files.first;
        final bytes = await file.readAsBytes();

        if (bytes.isEmpty) {
          _showErrorSnackBar('The selected file is empty.');
          return;
        }

        if (bytes.length > 10 * 1024 * 1024) {
          _showErrorSnackBar('The selected file is too large. Maximum file size is 10 MB.');
          return;
        }

        setState(() {
          _isUploadingFile = true;
          _uploadingDocId = doc.id;
        });

        await _exportService.uploadComplianceDocumentFile(
          widget.requestId,
          doc.id,
          bytes,
          file.name,
        );

        if (!mounted) return;

        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Compliance document uploaded successfully.'),
            backgroundColor: Colors.green,
          ),
        );

        _loadData();
      }
    } catch (err) {
      _showErrorSnackBar(err.toString().replaceAll('Exception: ', ''));
    } finally {
      if (mounted) {
        setState(() {
          _isUploadingFile = false;
          _uploadingDocId = null;
        });
      }
    }
  }

  void _showErrorSnackBar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.red.shade800,
      ),
    );
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
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadData,
          ),
        ],
      ),
      body: FutureBuilder<ExportRequestModel>(
        future: _requestFuture,
        builder: (context, reqSnapshot) {
          if (reqSnapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (reqSnapshot.hasError) {
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
                      reqSnapshot.error
                          .toString()
                          .replaceAll('Exception: ', ''),
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 16),
                    ),
                    const SizedBox(height: 20),
                    FilledButton.icon(
                      onPressed: _loadData,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            );
          }

          final req = reqSnapshot.data!;
          final statusColor = _getStatusColor(req.status);
          final explanation = _getStatusExplanation(req.status);
          final canEdit = _isEditableStatus(req.status);

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
                              padding:
                                  const EdgeInsets.symmetric(horizontal: 8),
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

                // COMPLIANCE DOCUMENTS SECTION
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
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Text(
                              'Compliance Documents',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            if (canEdit)
                              OutlinedButton.icon(
                                onPressed: _openAddDocumentDialog,
                                icon: const Icon(Icons.add, size: 18),
                                label: const Text('Add Document'),
                              ),
                          ],
                        ),
                        const Divider(height: 24),
                        FutureBuilder<List<ComplianceDocumentModel>>(
                          future: _documentsFuture,
                          builder: (context, docSnapshot) {
                            if (docSnapshot.connectionState ==
                                ConnectionState.waiting) {
                              return const Padding(
                                padding: EdgeInsets.all(16.0),
                                child: Center(
                                    child: CircularProgressIndicator()),
                              );
                            }

                            if (docSnapshot.hasError) {
                              return Padding(
                                padding: const EdgeInsets.all(12.0),
                                child: Text(
                                  'Error loading documents: ${docSnapshot.error.toString().replaceAll('Exception: ', '')}',
                                  style: const TextStyle(color: Colors.red),
                                ),
                              );
                            }

                            final docs = docSnapshot.data ?? [];

                            if (docs.isEmpty) {
                              return Padding(
                                padding:
                                    const EdgeInsets.symmetric(vertical: 16),
                                child: Column(
                                  children: [
                                    const Center(
                                      child: Text(
                                        'No compliance documents have been added yet.',
                                        style: TextStyle(
                                          fontSize: 14,
                                          color: Colors.grey,
                                          fontWeight: FontWeight.w500,
                                        ),
                                      ),
                                    ),
                                    if (canEdit) ...[
                                      const SizedBox(height: 12),
                                      Center(
                                        child: FilledButton.icon(
                                          onPressed: _openAddDocumentDialog,
                                          icon: const Icon(Icons.add),
                                          label: const Text(
                                              'Add Compliance Document'),
                                        ),
                                      ),
                                    ],
                                  ],
                                ),
                              );
                            }

                            return ListView.separated(
                              shrinkWrap: true,
                              physics: const NeverScrollableScrollPhysics(),
                              itemCount: docs.length,
                              separatorBuilder: (_, _) =>
                                  const SizedBox(height: 10),
                              itemBuilder: (context, index) {
                                final doc = docs[index];
                                final docStatusColor =
                                    _getDocStatusColor(doc.status);
                                final isThisDocUploading = _isUploadingFile &&
                                    _uploadingDocId == doc.id;

                                return Container(
                                  padding: const EdgeInsets.all(12),
                                  decoration: BoxDecoration(
                                    color: Colors.grey.shade50,
                                    borderRadius: BorderRadius.circular(8),
                                    border: Border.all(
                                        color: Colors.grey.shade300),
                                  ),
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        mainAxisAlignment:
                                            MainAxisAlignment.spaceBetween,
                                        children: [
                                          Expanded(
                                            child: Text(
                                              doc.documentType,
                                              style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                fontSize: 15,
                                              ),
                                            ),
                                          ),
                                          Chip(
                                            label: Text(
                                              doc.status,
                                              style: const TextStyle(
                                                color: Colors.white,
                                                fontSize: 11,
                                                fontWeight: FontWeight.bold,
                                              ),
                                            ),
                                            backgroundColor: docStatusColor,
                                            padding: EdgeInsets.zero,
                                            materialTapTargetSize:
                                                MaterialTapTargetSize
                                                    .shrinkWrap,
                                          ),
                                        ],
                                      ),
                                      if (doc.documentNumber != null &&
                                          doc.documentNumber!.isNotEmpty)
                                        Padding(
                                          padding:
                                              const EdgeInsets.only(top: 4),
                                          child: Text(
                                            'Number: ${doc.documentNumber}',
                                            style: const TextStyle(
                                                fontSize: 13),
                                          ),
                                        ),
                                      if (doc.issuer != null &&
                                          doc.issuer!.isNotEmpty)
                                        Padding(
                                          padding:
                                              const EdgeInsets.only(top: 2),
                                          child: Text(
                                            'Issuer: ${doc.issuer}',
                                            style: const TextStyle(
                                                fontSize: 13),
                                          ),
                                        ),
                                      Row(
                                        children: [
                                          if (doc.issueDate != null)
                                            Padding(
                                              padding: const EdgeInsets.only(
                                                  top: 4, right: 12),
                                              child: Text(
                                                'Issue: ${doc.issueDate!.toString().split(' ')[0]}',
                                                style: TextStyle(
                                                  fontSize: 12,
                                                  color: Colors.grey.shade700,
                                                ),
                                              ),
                                            ),
                                          if (doc.expiryDate != null)
                                            Padding(
                                              padding: const EdgeInsets.only(
                                                  top: 4),
                                              child: Text(
                                                'Expiry: ${doc.expiryDate!.toString().split(' ')[0]}',
                                                style: TextStyle(
                                                  fontSize: 12,
                                                  color: Colors.grey.shade700,
                                                ),
                                              ),
                                            ),
                                        ],
                                      ),
                                      const SizedBox(height: 8),

                                      // FILE ATTACHMENT STATUS
                                      Row(
                                        mainAxisAlignment:
                                            MainAxisAlignment.spaceBetween,
                                        children: [
                                          Row(
                                            children: [
                                              Icon(
                                                doc.hasUploadedFile
                                                    ? Icons.check_circle
                                                    : Icons.warning_amber_rounded,
                                                size: 16,
                                                color: doc.hasUploadedFile
                                                    ? Colors.green
                                                    : Colors.orange.shade800,
                                              ),
                                              const SizedBox(width: 6),
                                              Text(
                                                doc.hasUploadedFile
                                                    ? 'File attached'
                                                    : 'File: Not uploaded',
                                                style: TextStyle(
                                                  fontSize: 13,
                                                  fontWeight: FontWeight.w600,
                                                  color: doc.hasUploadedFile
                                                      ? Colors.green.shade800
                                                      : Colors.orange.shade900,
                                                ),
                                              ),
                                            ],
                                          ),
                                          if (!doc.hasUploadedFile && canEdit)
                                            ElevatedButton.icon(
                                              onPressed: isThisDocUploading
                                                  ? null
                                                  : () =>
                                                      _uploadFileForExistingDoc(
                                                          doc),
                                              icon: isThisDocUploading
                                                  ? const SizedBox(
                                                      width: 14,
                                                      height: 14,
                                                      child:
                                                          CircularProgressIndicator(
                                                        strokeWidth: 2,
                                                      ),
                                                    )
                                                  : const Icon(Icons.upload_file,
                                                      size: 16),
                                              label: Text(isThisDocUploading
                                                  ? 'Uploading...'
                                                  : 'Upload File'),
                                              style: ElevatedButton.styleFrom(
                                                padding:
                                                    const EdgeInsets.symmetric(
                                                        horizontal: 12,
                                                        vertical: 6),
                                                textStyle: const TextStyle(
                                                    fontSize: 12),
                                              ),
                                            ),
                                        ],
                                      ),
                                    ],
                                  ),
                                );
                              },
                            );
                          },
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 20),

                // PHASE 2 INFORMATIONAL NOTICE
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
                          'Review your request and documents before submission.',
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

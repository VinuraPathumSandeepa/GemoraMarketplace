import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../../models/compliance_analysis_result_model.dart';
import '../../models/compliance_document_model.dart';
import '../../models/export_request_model.dart';
import '../../services/export_compliance_service.dart';
import 'add_compliance_document_dialog.dart';
import 'edit_export_request_screen.dart';

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

  bool _isSubmitting = false;
  bool _isRunningAnalysis = false;
  ComplianceWorkflowAnalysisResultModel? _recentAnalysisResult;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  void _loadData() {
    setState(() {
      _requestFuture = _exportService.getExportRequestById(widget.requestId);
      _documentsFuture =
          _exportService.getComplianceDocuments(widget.requestId);
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
        return 'The Export Officer requested changes before this request can continue.';
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

  Future<void> _openEditRequestScreen(ExportRequestModel req) async {
    final result = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => EditExportRequestScreen(request: req),
      ),
    );

    if (result == true) {
      setState(() {
        _recentAnalysisResult = null;
      });
      _loadData();
    }
  }

  Future<void> _uploadFileForExistingDoc(ComplianceDocumentModel doc) async {
    if (_isUploadingFile || _isSubmitting || _isRunningAnalysis) return;

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
          _showErrorSnackBar(
              'The selected file is too large. Maximum file size is 10 MB.');
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

  Future<void> _confirmAndSubmit() async {
    if (_isSubmitting || _isRunningAnalysis) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Submit export request?'),
        content: const Text(
          'After submission, the request will move into compliance processing and editing may be restricted.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Submit Request'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() {
      _isSubmitting = true;
      _recentAnalysisResult = null;
    });

    try {
      await _exportService.submitExportRequest(widget.requestId);
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Export request submitted successfully.'),
          backgroundColor: Colors.green,
        ),
      );

      _loadData();
    } catch (err) {
      if (!mounted) return;
      _showErrorSnackBar(err.toString().replaceAll('Exception: ', ''));
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  Future<void> _confirmAndResubmit() async {
    if (_isSubmitting || _isRunningAnalysis) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Resubmit export request?'),
        content: const Text(
          'This will send your revised request back into compliance processing.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Resubmit Request'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() {
      _isSubmitting = true;
      _recentAnalysisResult = null;
    });

    try {
      await _exportService.submitExportRequest(widget.requestId);
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Export request resubmitted successfully.'),
          backgroundColor: Colors.green,
        ),
      );

      _loadData();
    } catch (err) {
      if (!mounted) return;
      _showErrorSnackBar(err.toString().replaceAll('Exception: ', ''));
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  Future<void> _confirmAndRunComplianceAnalysis() async {
    if (_isSubmitting || _isRunningAnalysis) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Start compliance analysis?'),
        content: const Text(
          'The system will check your request and document metadata, run an AI-assisted compliance assessment, and prepare the request for Export Officer review.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Start Analysis'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() {
      _isRunningAnalysis = true;
      _recentAnalysisResult = null;
    });

    try {
      final result =
          await _exportService.runComplianceAnalysis(widget.requestId);

      if (!mounted) return;

      if (result.success) {
        setState(() {
          _recentAnalysisResult = result;
        });

        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Compliance analysis completed. Your request is ready for Export Officer review.',
            ),
            backgroundColor: Colors.green,
          ),
        );
      }

      _loadData();
    } catch (err) {
      if (!mounted) return;
      final errorMessage = err.toString().replaceAll('Exception: ', '');

      _loadData();

      try {
        final refreshedReq =
            await _exportService.getExportRequestById(widget.requestId);
        if (refreshedReq.status.toLowerCase() == 'undercompliancereview') {
          _showErrorSnackBar(
            'Automated compliance analysis could not be completed. Your request remains in compliance review and can continue to Export Officer review.',
          );
          return;
        }
      } catch (_) {}

      _showErrorSnackBar(errorMessage);
    } finally {
      if (mounted) {
        setState(() {
          _isRunningAnalysis = false;
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

  Widget _buildDraftActionSection(bool canEdit) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Ready to Submit',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              'Review your request and compliance documents before submitting.',
              style: TextStyle(
                fontSize: 14,
                color: Colors.grey.shade800,
              ),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _isSubmitting || _isUploadingFile
                    ? null
                    : _confirmAndSubmit,
                icon: _isSubmitting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.send),
                label: Text(
                  _isSubmitting ? 'Submitting request...' : 'Submit Export Request',
                  style: const TextStyle(fontSize: 16),
                ),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSubmittedActionSection() {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Compliance Analysis',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              'Start the automated compliance assessment before Export Officer review.',
              style: TextStyle(
                fontSize: 14,
                color: Colors.grey.shade800,
              ),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.amber.shade50,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.amber.shade200),
              ),
              child: Row(
                children: [
                  Icon(Icons.info_outline, color: Colors.amber.shade900, size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'The automated assessment supports the Export Officer. It does not make the final export decision.',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.amber.shade900,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _isRunningAnalysis ? null : _confirmAndRunComplianceAnalysis,
                icon: _isRunningAnalysis
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.analytics_outlined),
                label: Text(
                  _isRunningAnalysis
                      ? 'Running compliance analysis...'
                      : 'Start Compliance Analysis',
                  style: const TextStyle(fontSize: 16),
                ),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  backgroundColor: Colors.purple.shade700,
                ),
              ),
            ),
            if (_isRunningAnalysis) ...[
              const SizedBox(height: 8),
              Center(
                child: Text(
                  'This may take a few moments.',
                  style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildUnderComplianceReviewSection() {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Icon(Icons.hourglass_top_rounded, color: Colors.purple.shade700, size: 28),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Compliance Review in Progress',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Your request is in compliance review and is ready for Export Officer evaluation.',
                    style: TextStyle(fontSize: 13, color: Colors.grey.shade800),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildUnderOfficerReviewSection() {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Icon(Icons.assignment_ind, color: Colors.orange.shade800, size: 28),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Officer Review',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'An Export Officer is reviewing your request.',
                    style: TextStyle(fontSize: 13, color: Colors.grey.shade800),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRevisionRequiredSection(ExportRequestModel req) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Card(
          elevation: 2,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(Icons.edit_note_rounded,
                        color: Colors.amber.shade900, size: 28),
                    const SizedBox(width: 12),
                    const Expanded(
                      child: Text(
                        'Revision Required',
                        style: TextStyle(
                            fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Text(
                  'The Export Officer requested changes before this request can continue.',
                  style: TextStyle(fontSize: 14, color: Colors.grey.shade800),
                ),
                if (req.reviewNotes != null && req.reviewNotes!.isNotEmpty) ...[
                  const SizedBox(height: 14),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.amber.shade50,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: Colors.amber.shade300),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Officer Revision Instructions',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: Colors.amber.shade900,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          req.reviewNotes!,
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.amber.shade900,
                            height: 1.3,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 16),
                SizedBox(
                  width: double.infinity,
                  child: OutlinedButton.icon(
                    onPressed: _isSubmitting || _isUploadingFile
                        ? null
                        : () => _openEditRequestScreen(req),
                    icon: const Icon(Icons.edit),
                    label: const Text('Edit Request Details'),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 20),
        Card(
          elevation: 2,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Ready to Resubmit',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  'After making the requested corrections, resubmit the request for another compliance review.',
                  style: TextStyle(
                    fontSize: 14,
                    color: Colors.grey.shade800,
                  ),
                ),
                const SizedBox(height: 16),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: _isSubmitting || _isUploadingFile
                        ? null
                        : _confirmAndResubmit,
                    icon: _isSubmitting
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(Icons.send),
                    label: Text(
                      _isSubmitting
                          ? 'Resubmitting request...'
                          : 'Resubmit Export Request',
                      style: const TextStyle(fontSize: 16),
                    ),
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      backgroundColor: Colors.amber.shade900,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildAssessmentSummaryCard(ComplianceAgentResultModel assessment) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Automated Compliance Assessment',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
                Chip(
                  label: const Text(
                    'Advisory only',
                    style: TextStyle(fontSize: 11, color: Colors.white),
                  ),
                  backgroundColor: Colors.purple.shade600,
                  padding: EdgeInsets.zero,
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
              ],
            ),
            const Divider(height: 20),
            Text(
              assessment.summary,
              style: const TextStyle(fontSize: 14, height: 1.4),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Chip(
                  avatar: const Icon(Icons.speed, size: 16),
                  label: Text(
                    'Confidence: ${(assessment.confidence * 100).toStringAsFixed(0)}%',
                    style: const TextStyle(fontSize: 12),
                  ),
                  backgroundColor: Colors.grey.shade200,
                ),
                const SizedBox(width: 8),
                if (assessment.requiresOfficerAttention)
                  Chip(
                    avatar: const Icon(Icons.warning, size: 16, color: Colors.orange),
                    label: const Text(
                      'Officer Attention Needed',
                      style: TextStyle(fontSize: 12),
                    ),
                    backgroundColor: Colors.orange.shade50,
                  ),
              ],
            ),
            if (assessment.disclaimer.isNotEmpty) ...[
              const SizedBox(height: 10),
              Text(
                assessment.disclaimer,
                style: TextStyle(
                  fontSize: 12,
                  fontStyle: FontStyle.italic,
                  color: Colors.grey.shade600,
                ),
              ),
            ],
          ],
        ),
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
          final statusLower = req.status.toLowerCase();

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

                // PHASE 3 & PHASE 4 ACTION SECTIONS BASED ON STATUS
                if (statusLower == 'draft') ...[
                  _buildDraftActionSection(canEdit),
                  const SizedBox(height: 20),
                ] else if (statusLower == 'submitted') ...[
                  _buildSubmittedActionSection(),
                  const SizedBox(height: 20),
                ] else if (statusLower == 'undercompliancereview') ...[
                  _buildUnderComplianceReviewSection(),
                  const SizedBox(height: 20),
                ] else if (statusLower == 'underofficerreview') ...[
                  _buildUnderOfficerReviewSection(),
                  const SizedBox(height: 20),
                ] else if (statusLower == 'revisionrequired') ...[
                  _buildRevisionRequiredSection(req),
                  const SizedBox(height: 20),
                ],

                // ADVISORY ASSESSMENT SUMMARY (Current session if available)
                if (_recentAnalysisResult?.assessment != null) ...[
                  _buildAssessmentSummaryCard(_recentAnalysisResult!.assessment!),
                  const SizedBox(height: 20),
                ],

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
                                onPressed: _isSubmitting || _isUploadingFile
                                    ? null
                                    : _openAddDocumentDialog,
                                icon: const Icon(Icons.add, size: 18),
                                label: const Text('Add Document'),
                              ),
                          ],
                        ),
                        if (statusLower == 'revisionrequired') ...[
                          const SizedBox(height: 6),
                          Text(
                            'Update the requested information or provide corrected documents before resubmitting.',
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.amber.shade900,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        ],
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
                                          onPressed: _isSubmitting || _isUploadingFile
                                              ? null
                                              : _openAddDocumentDialog,
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
                                              onPressed: isThisDocUploading || _isSubmitting || _isRunningAnalysis
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
              ],
            ),
          );
        },
      ),
    );
  }
}

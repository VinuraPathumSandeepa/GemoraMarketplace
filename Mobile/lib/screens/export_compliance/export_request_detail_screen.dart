import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/gemora_colors.dart';
import '../../models/compliance_analysis_result_model.dart';
import '../../models/compliance_document_model.dart';
import '../../models/export_request_model.dart';
import '../../providers/auth_provider.dart';
import '../../services/export_compliance_service.dart';
import 'add_compliance_document_dialog.dart';
import 'edit_export_request_screen.dart';

class ExportRequestDetailScreen extends StatefulWidget {
  final String requestId;
  final ExportComplianceService? exportService;

  const ExportRequestDetailScreen({
    super.key,
    required this.requestId,
    this.exportService,
  });

  @override
  State<ExportRequestDetailScreen> createState() =>
      _ExportRequestDetailScreenState();
}

class _ExportRequestDetailScreenState extends State<ExportRequestDetailScreen> {
  late final ExportComplianceService _exportService;

  late Future<ExportRequestModel> _requestFuture;
  late Future<List<ComplianceDocumentModel>> _documentsFuture;

  bool _isUploadingFile = false;
  String? _uploadingDocId;

  bool _isSubmitting = false;
  bool _isRunningAnalysis = false;
  bool _isRetryingAnalysis = false;
  ComplianceWorkflowAnalysisResultModel? _recentAnalysisResult;

  @override
  void initState() {
    super.initState();
    _exportService = widget.exportService ?? ExportComplianceService();
    _loadData();
  }

  void _loadData() {
    setState(() {
      _requestFuture = _exportService.getExportRequestById(widget.requestId);
      _documentsFuture = _exportService.getComplianceDocuments(
        widget.requestId,
      );
    });
  }

  Color _getStatusColor(String status) {
    return GemoraColors.getStatusColor(status);
  }

  Color _getDocStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'valid':
        return const Color(0xFF2E7D32);
      case 'invalid':
        return const Color(0xFFC62828);
      case 'expired':
        return const Color(0xFFB71C1C);
      case 'requiresreview':
        return const Color(0xFFD97706);
      case 'pending':
      default:
        return GemoraColors.darkGold;
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

  int _getStepIndex(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
        return 0;
      case 'submitted':
        return 1;
      case 'undercompliancereview':
        return 2;
      case 'underofficerreview':
        return 3;
      case 'revisionrequired':
        return 3;
      case 'approved':
      case 'rejected':
        return 4;
      default:
        return 1;
    }
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    const months = [
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
    final hour24 = local.hour;
    final hour12 = hour24 == 0 ? 12 : (hour24 > 12 ? hour24 - 12 : hour24);
    final minute = local.minute.toString().padLeft(2, '0');
    final ampm = hour24 >= 12 ? 'PM' : 'AM';
    return '$month $day, $year • $hour12:$minute $ampm';
  }

  String _shortenId(String id) {
    if (id.length > 16) {
      return '${id.substring(0, 8)}…${id.substring(id.length - 4)}';
    }
    return id;
  }

  bool _isEditableStatus(String status) {
    final lower = status.toLowerCase();
    return lower == 'draft' || lower == 'revisionrequired';
  }

  Future<void> _openAddDocumentDialog() async {
    final result = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) =>
          AddComplianceDocumentDialog(exportRequestId: widget.requestId),
    );

    if (result == true) {
      _loadData();
    }
  }

  Future<void> _openEditRequestScreen(ExportRequestModel req) async {
    final result = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => EditExportRequestScreen(request: req)),
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
            'The selected file is too large. Maximum file size is 10 MB.',
          );
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
            backgroundColor: Color(0xFF2E7D32),
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
      builder: (dialogContext) => Dialog(
        backgroundColor: GemoraColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
          side: const BorderSide(color: GemoraColors.border),
        ),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  color: GemoraColors.cream,
                  borderRadius: BorderRadius.circular(26),
                  border: Border.all(color: GemoraColors.border),
                ),
                child: const Icon(
                  Icons.send_rounded,
                  color: GemoraColors.darkGold,
                  size: 24,
                ),
              ),
              const SizedBox(height: 18),
              const Text(
                'Submit Export Request?',
                style: TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w800,
                  color: GemoraColors.darkGreen,
                ),
              ),
              const SizedBox(height: 10),
              const Text(
                'After submission, the request will move into compliance processing and editing may be restricted.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: GemoraColors.deepGreen,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(dialogContext).pop(false),
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: GemoraColors.border),
                        foregroundColor: GemoraColors.darkGreen,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Cancel'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed: () => Navigator.of(dialogContext).pop(true),
                      style: FilledButton.styleFrom(
                        backgroundColor: GemoraColors.darkGreen,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Submit Request'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
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
          backgroundColor: Color(0xFF2E7D32),
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
      builder: (dialogContext) => Dialog(
        backgroundColor: GemoraColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
          side: const BorderSide(color: GemoraColors.border),
        ),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  color: GemoraColors.cream,
                  borderRadius: BorderRadius.circular(26),
                  border: Border.all(color: GemoraColors.border),
                ),
                child: const Icon(
                  Icons.replay_rounded,
                  color: GemoraColors.darkGold,
                  size: 26,
                ),
              ),
              const SizedBox(height: 18),
              const Text(
                'Resubmit Export Request?',
                style: TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w800,
                  color: GemoraColors.darkGreen,
                ),
              ),
              const SizedBox(height: 10),
              const Text(
                'This will send your revised request back into compliance processing.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: GemoraColors.deepGreen,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(dialogContext).pop(false),
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: GemoraColors.border),
                        foregroundColor: GemoraColors.darkGreen,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Cancel'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed: () => Navigator.of(dialogContext).pop(true),
                      style: FilledButton.styleFrom(
                        backgroundColor: GemoraColors.darkGold,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Resubmit Request'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
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
          backgroundColor: Color(0xFF2E7D32),
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

  bool _isRetryEligible(ExportRequestModel req) {
    final statusLower = req.status.toLowerCase();
    if (statusLower == 'approved' ||
        statusLower == 'rejected' ||
        statusLower == 'cancelled') {
      return false;
    }

    if (_recentAnalysisResult == null) {
      return false;
    }

    if (_recentAnalysisResult!.success) {
      return false;
    }

    final code = _recentAnalysisResult!.errorCode?.toUpperCase() ?? '';

    const retryableErrorCodes = {
      'AI_PROVIDER_UNAVAILABLE',
      'AI_RATE_LIMITED',
      'AI_TIMEOUT',
      'AI_PROVIDER_ERROR',
    };

    return retryableErrorCodes.contains(code);
  }

  Future<void> _confirmAndRunComplianceAnalysis() async {
    if (_isSubmitting || _isRunningAnalysis || _isRetryingAnalysis) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => Dialog(
        backgroundColor: GemoraColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
          side: const BorderSide(color: GemoraColors.border),
        ),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  color: GemoraColors.cream,
                  borderRadius: BorderRadius.circular(26),
                  border: Border.all(color: GemoraColors.border),
                ),
                child: const Icon(
                  Icons.auto_awesome,
                  color: GemoraColors.darkGold,
                  size: 26,
                ),
              ),
              const SizedBox(height: 18),
              const Text(
                'Start Compliance Analysis?',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w800,
                  color: GemoraColors.darkGreen,
                ),
              ),
              const SizedBox(height: 12),
              const Text(
                'Gemora will review your export request, document metadata and applicable compliance requirements.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: GemoraColors.deepGreen,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFFFEF3C7),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: const Color(0xFFFDE68A)),
                ),
                child: const Row(
                  children: [
                    Icon(
                      Icons.info_outline,
                      color: Color(0xFFB45309),
                      size: 18,
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'This assessment is advisory. The Export Officer makes the final decision.',
                        style: TextStyle(
                          fontSize: 12,
                          color: Color(0xFF92400E),
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(dialogContext).pop(false),
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: GemoraColors.border),
                        foregroundColor: GemoraColors.darkGreen,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Cancel'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: () => Navigator.of(dialogContext).pop(true),
                      icon: const Icon(
                        Icons.auto_awesome,
                        size: 16,
                        color: GemoraColors.gold,
                      ),
                      label: const Text('Start Analysis'),
                      style: FilledButton.styleFrom(
                        backgroundColor: GemoraColors.darkGreen,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );

    if (confirmed != true) return;

    setState(() {
      _isRunningAnalysis = true;
    });

    try {
      final result = await _exportService.runComplianceAnalysis(
        widget.requestId,
      );

      if (!mounted) return;

      setState(() {
        _recentAnalysisResult = result;
      });

      if (result.success) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Compliance analysis completed. Your request is ready for Export Officer review.',
            ),
            backgroundColor: Color(0xFF2E7D32),
          ),
        );
      } else {
        final msgLower = result.message.toLowerCase();
        final codeUpper = result.errorCode?.toUpperCase() ?? '';

        if (codeUpper.contains('WORKFLOW_ALREADY_ACTIVE') ||
            msgLower.contains('active export compliance workflow') ||
            msgLower.contains('already exists')) {
          _showErrorSnackBar(
            'An AI compliance assessment is already active or has progressed to officer review. Refresh the request to see the latest status.',
          );
        } else {
          _showErrorSnackBar(result.message);
        }
      }

      _loadData();
    } catch (err) {
      if (!mounted) return;
      final errorMessage = err.toString().replaceAll('Exception: ', '');
      final errLower = errorMessage.toLowerCase();

      if (errLower.contains('active export compliance workflow') ||
          errLower.contains('already exists')) {
        _showErrorSnackBar(
          'An AI compliance assessment is already active or has progressed to officer review. Refresh the request to see the latest status.',
        );
      } else {
        _showErrorSnackBar(errorMessage);
      }

      _loadData();
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
        backgroundColor: const Color(0xFFC62828),
      ),
    );
  }

  Future<void> _confirmAndRetryComplianceAnalysis() async {
    if (_isSubmitting || _isRunningAnalysis || _isRetryingAnalysis) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => Dialog(
        backgroundColor: GemoraColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
          side: const BorderSide(color: GemoraColors.border),
        ),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  color: GemoraColors.cream,
                  borderRadius: BorderRadius.circular(26),
                  border: Border.all(color: GemoraColors.border),
                ),
                child: const Icon(
                  Icons.refresh_rounded,
                  color: GemoraColors.darkGold,
                  size: 26,
                ),
              ),
              const SizedBox(height: 18),
              const Text(
                'Retry Compliance Analysis?',
                style: TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w800,
                  color: GemoraColors.darkGreen,
                ),
              ),
              const SizedBox(height: 10),
              const Text(
                'This will trigger a new AI compliance analysis attempt for this request.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: GemoraColors.deepGreen,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(dialogContext).pop(false),
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: GemoraColors.border),
                        foregroundColor: GemoraColors.darkGreen,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Cancel'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed: () => Navigator.of(dialogContext).pop(true),
                      style: FilledButton.styleFrom(
                        backgroundColor: GemoraColors.darkGreen,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: const Text('Retry Analysis'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );

    if (confirmed != true) return;

    setState(() {
      _isRetryingAnalysis = true;
    });

    try {
      final result = await _exportService.retryComplianceAnalysis(
        widget.requestId,
      );

      if (!mounted) return;

      setState(() {
        _recentAnalysisResult = result;
      });

      if (result.success) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Compliance analysis retried successfully.'),
            backgroundColor: Color(0xFF2E7D32),
          ),
        );
      } else {
        final msgLower = result.message.toLowerCase();
        final codeUpper = result.errorCode?.toUpperCase() ?? '';

        if (codeUpper.contains('WORKFLOW_ALREADY_ACTIVE') ||
            msgLower.contains('active export compliance workflow') ||
            msgLower.contains('already exists')) {
          _showErrorSnackBar(
            'An AI compliance assessment is already active or has progressed to officer review. Refresh the request to see the latest status.',
          );
        } else {
          _showErrorSnackBar(result.message);
        }
      }

      _loadData();
    } catch (err) {
      if (!mounted) return;
      final errorMessage = err.toString().replaceAll('Exception: ', '');
      final errLower = errorMessage.toLowerCase();

      if (errLower.contains('active export compliance workflow') ||
          errLower.contains('already exists')) {
        _showErrorSnackBar(
          'An AI compliance assessment is already active or has progressed to officer review. Refresh the request to see the latest status.',
        );
      } else {
        _showErrorSnackBar(errorMessage);
      }

      _loadData();
    } finally {
      if (mounted) {
        setState(() {
          _isRetryingAnalysis = false;
        });
      }
    }
  }

  Widget _buildTimelineProgress(String status) {
    final currentStep = _getStepIndex(status);
    final statusLower = status.toLowerCase();
    final isRejected = statusLower == 'rejected';
    final isRevision = statusLower == 'revisionrequired';

    final steps = [
      'Draft',
      'Submitted',
      'Compliance',
      'Officer',
      isRejected ? 'Rejected' : (isRevision ? 'Revision' : 'Decision'),
    ];

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 4),
      child: Row(
        children: List.generate(steps.length * 2 - 1, (index) {
          if (index.isOdd) {
            final stepBefore = index ~/ 2;
            final isPassed = stepBefore < currentStep;
            return Expanded(
              child: Container(
                height: 2.5,
                color: isPassed ? GemoraColors.darkGold : GemoraColors.border,
              ),
            );
          }

          final stepIdx = index ~/ 2;
          final isCompleted = stepIdx < currentStep;
          final isCurrent = stepIdx == currentStep;

          Color circleColor;
          Widget innerWidget;

          if (isCompleted) {
            circleColor = GemoraColors.darkGreen;
            innerWidget = const Icon(
              Icons.check,
              size: 12,
              color: GemoraColors.gold,
            );
          } else if (isCurrent) {
            if (isRejected) {
              circleColor = const Color(0xFFC62828);
              innerWidget = const Icon(
                Icons.close,
                size: 12,
                color: Colors.white,
              );
            } else if (isRevision) {
              circleColor = const Color(0xFFD97706);
              innerWidget = const Icon(
                Icons.edit,
                size: 11,
                color: Colors.white,
              );
            } else {
              circleColor = GemoraColors.darkGold;
              innerWidget = Container(
                width: 6,
                height: 6,
                decoration: const BoxDecoration(
                  color: Colors.white,
                  shape: BoxShape.circle,
                ),
              );
            }
          } else {
            circleColor = GemoraColors.border;
            innerWidget = Container(
              width: 5,
              height: 5,
              decoration: const BoxDecoration(
                color: GemoraColors.mutedText,
                shape: BoxShape.circle,
              ),
            );
          }

          return Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 22,
                height: 22,
                decoration: BoxDecoration(
                  color: circleColor,
                  shape: BoxShape.circle,
                  border: isCurrent
                      ? Border.all(color: GemoraColors.cream, width: 2)
                      : null,
                  boxShadow: isCurrent
                      ? [
                          BoxShadow(
                            color: circleColor.withValues(alpha: 0.35),
                            blurRadius: 6,
                            offset: const Offset(0, 2),
                          ),
                        ]
                      : null,
                ),
                alignment: Alignment.center,
                child: innerWidget,
              ),
              const SizedBox(height: 6),
              Text(
                steps[stepIdx],
                style: TextStyle(
                  fontSize: 10,
                  fontWeight: isCurrent ? FontWeight.w800 : FontWeight.w600,
                  color: isCurrent
                      ? GemoraColors.darkGreen
                      : (isCompleted
                            ? GemoraColors.deepGreen
                            : GemoraColors.mutedText),
                ),
              ),
            ],
          );
        }),
      ),
    );
  }

  Widget _buildStatusCard(ExportRequestModel req, bool isBuyer) {
    final statusColor = _getStatusColor(req.status);
    final friendlyStatus = GemoraColors.getStatusLabel(req.status);
    final explanation = _getStatusExplanation(req.status);

    return Card(
      elevation: 0,
      color: GemoraColors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: GemoraColors.border),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                const Row(
                  children: [
                    Icon(
                      Icons.shield_outlined,
                      size: 18,
                      color: GemoraColors.darkGold,
                    ),
                    SizedBox(width: 8),
                    Text(
                      'Request Status',
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                        color: GemoraColors.mutedText,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 5,
                  ),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(
                      color: statusColor.withValues(alpha: 0.3),
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 7,
                        height: 7,
                        decoration: BoxDecoration(
                          color: statusColor,
                          shape: BoxShape.circle,
                        ),
                      ),
                      const SizedBox(width: 6),
                      Text(
                        isBuyer
                            ? _getBuyerFriendlyStage(req.status)
                            : friendlyStatus,
                        style: TextStyle(
                          color: statusColor,
                          fontWeight: FontWeight.w800,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Text(
              explanation,
              style: const TextStyle(
                fontSize: 14,
                color: GemoraColors.deepGreen,
                height: 1.4,
              ),
            ),
            const Divider(height: 24, color: GemoraColors.border),
            _buildTimelineProgress(req.status),
          ],
        ),
      ),
    );
  }

  Widget _buildLegacyBanner() {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: GemoraColors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: GemoraColors.border),
      ),
      child: const Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.history, color: GemoraColors.mutedText, size: 18),
          SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Legacy export request',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: GemoraColors.deepGreen,
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'Created before marketplace order linking was introduced.',
                  style: TextStyle(fontSize: 11, color: GemoraColors.mutedText),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildOrderInfoCard(ExportRequestModel req) {
    return Card(
      elevation: 0,
      color: GemoraColors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: GemoraColors.border),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(
                  Icons.receipt_long_outlined,
                  color: GemoraColors.darkGold,
                  size: 20,
                ),
                SizedBox(width: 8),
                Text(
                  'ORDER INFORMATION',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w800,
                    color: GemoraColors.mutedText,
                    letterSpacing: 0.8,
                  ),
                ),
              ],
            ),
            const Divider(height: 22, color: GemoraColors.border),
            _buildInfoRow(
              icon: Icons.receipt_outlined,
              label: 'Order Reference',
              value: req.orderNumber ?? '#${req.orderId!.substring(0, 8)}',
              valueColor: GemoraColors.darkGold,
            ),
            if (req.gemListingTitle != null && req.gemListingTitle!.isNotEmpty)
              _buildInfoRow(
                icon: Icons.diamond_outlined,
                label: 'Gemstone Listing',
                value: req.gemListingTitle!,
              ),
            _buildInfoRow(
              icon: Icons.flight_takeoff,
              label: 'Route',
              value: '${req.originCountry} → ${req.destinationCountry}',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildExportInfoCard(ExportRequestModel req) {
    return Card(
      elevation: 0,
      color: GemoraColors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: GemoraColors.border),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.public, color: GemoraColors.darkGold, size: 20),
                SizedBox(width: 8),
                Text(
                  'EXPORT INFORMATION',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w800,
                    color: GemoraColors.mutedText,
                    letterSpacing: 0.8,
                  ),
                ),
              ],
            ),
            const Divider(height: 22, color: GemoraColors.border),
            _buildInfoRow(
              icon: Icons.fingerprint,
              label: 'Request ID',
              value: _shortenId(req.id),
              fullValue: req.id,
            ),
            _buildInfoRow(
              icon: Icons.location_on_outlined,
              label: 'Origin',
              value: req.originCountry,
            ),
            _buildInfoRow(
              icon: Icons.flight_land,
              label: 'Destination',
              value: req.destinationCountry,
            ),
            _buildInfoRow(
              icon: Icons.payments_outlined,
              label: 'Declared Value',
              value: '${req.declaredValue.toStringAsFixed(2)} ${req.currency}',
            ),
            _buildInfoRow(
              icon: Icons.assignment_outlined,
              label: 'Purpose',
              value: (req.purpose != null && req.purpose!.trim().isNotEmpty)
                  ? req.purpose!
                  : 'Commercial Export',
            ),
            _buildInfoRow(
              icon: Icons.access_time,
              label: 'Created',
              value: _formatDate(req.createdAt),
            ),
            if (req.submittedAt != null)
              _buildInfoRow(
                icon: Icons.send_outlined,
                label: 'Submitted',
                value: _formatDate(req.submittedAt!),
              ),
            _buildInfoRow(
              icon: Icons.update,
              label: 'Updated',
              value: _formatDate(req.updatedAt),
            ),
            if (req.reviewedAt != null)
              _buildInfoRow(
                icon: Icons.gavel_outlined,
                label: 'Reviewed',
                value: _formatDate(req.reviewedAt!),
              ),
            if (req.reviewNotes != null && req.reviewNotes!.trim().isNotEmpty)
              _buildInfoRow(
                icon: Icons.notes,
                label: 'Review Notes',
                value: req.reviewNotes!,
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow({
    IconData? icon,
    required String label,
    required String value,
    String? fullValue,
    Color? valueColor,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 7),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 16, color: GemoraColors.mutedText),
            const SizedBox(width: 8),
          ],
          SizedBox(
            width: 120,
            child: Text(
              label,
              style: const TextStyle(
                fontWeight: FontWeight.w600,
                color: GemoraColors.mutedText,
                fontSize: 13,
              ),
            ),
          ),
          Expanded(
            child: Tooltip(
              message: fullValue ?? value,
              child: Text(
                value,
                textAlign: TextAlign.end,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: valueColor ?? GemoraColors.deepGreen,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDraftActionSection(bool canEdit) {
    return Card(
      elevation: 0,
      color: GemoraColors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: GemoraColors.border),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Ready to Submit',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w800,
                color: GemoraColors.darkGreen,
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'Review your request and compliance documents before submitting.',
              style: TextStyle(
                fontSize: 13,
                color: GemoraColors.deepGreen,
                height: 1.4,
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
                    : const Icon(Icons.send_rounded, size: 18),
                label: Text(
                  _isSubmitting
                      ? 'Submitting request...'
                      : 'Submit Export Request',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                style: FilledButton.styleFrom(
                  backgroundColor: GemoraColors.darkGreen,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
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
      elevation: 0,
      color: GemoraColors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: GemoraColors.border),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(
                  Icons.auto_awesome,
                  color: GemoraColors.darkGold,
                  size: 20,
                ),
                SizedBox(width: 8),
                Text(
                  'Run Compliance Analysis',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                    color: GemoraColors.darkGreen,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            const Text(
              'Check export requirements and document metadata before officer review.',
              style: TextStyle(
                fontSize: 13,
                color: GemoraColors.deepGreen,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFFEF3C7),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFFDE68A)),
              ),
              child: const Row(
                children: [
                  Icon(Icons.info_outline, color: Color(0xFFB45309), size: 18),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'The automated assessment supports the Export Officer. It does not make the final export decision.',
                      style: TextStyle(
                        fontSize: 12,
                        color: Color(0xFF92400E),
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 18),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _isRunningAnalysis
                    ? null
                    : _confirmAndRunComplianceAnalysis,
                icon: _isRunningAnalysis
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(
                        Icons.auto_awesome,
                        size: 18,
                        color: GemoraColors.gold,
                      ),
                label: Text(
                  _isRunningAnalysis
                      ? 'Running compliance analysis...'
                      : 'Start Analysis',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  backgroundColor: GemoraColors.darkGreen,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
              ),
            ),
            if (_isRunningAnalysis) ...[
              const SizedBox(height: 8),
              const Center(
                child: Text(
                  'This may take a few moments.',
                  style: TextStyle(fontSize: 12, color: GemoraColors.mutedText),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildUnderComplianceReviewSection(ExportRequestModel req) {
    final isRetryEligible = _isRetryEligible(req);

    if (_isRunningAnalysis) {
      return Card(
        elevation: 0,
        color: GemoraColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(18),
          side: const BorderSide(color: GemoraColors.border),
        ),
        child: const Padding(
          padding: EdgeInsets.all(20),
          child: Row(
            children: [
              SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(
                  strokeWidth: 2.5,
                  color: GemoraColors.darkGold,
                ),
              ),
              SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Compliance Review in Progress',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                        color: GemoraColors.darkGreen,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Automated compliance analysis is currently running.',
                      style: TextStyle(
                        fontSize: 13,
                        color: GemoraColors.mutedText,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      );
    }

    if (isRetryEligible) {
      return Card(
        elevation: 0,
        color: GemoraColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(18),
          side: BorderSide(color: Colors.amber.shade300),
        ),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.warning_amber_rounded,
                    color: Colors.amber.shade900,
                    size: 26,
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'AI Assessment Temporarily Unavailable',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                            color: Colors.amber.shade900,
                          ),
                        ),
                        const SizedBox(height: 4),
                        const Text(
                          'The deterministic compliance check completed, but the AI assessment could not be reached. You may retry the AI assessment.',
                          style: TextStyle(
                            fontSize: 13,
                            color: GemoraColors.deepGreen,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed:
                      _isRetryingAnalysis || _isRunningAnalysis || _isSubmitting
                      ? null
                      : _confirmAndRetryComplianceAnalysis,
                  icon: _isRetryingAnalysis
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.refresh, size: 18),
                  label: Text(
                    _isRetryingAnalysis
                        ? 'Retrying AI analysis...'
                        : 'Retry AI Compliance Assessment',
                  ),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: GemoraColors.darkGreen,
                    side: const BorderSide(color: GemoraColors.darkGreen),
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      );
    }

    // Ready for Export Officer Review
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFF0F7F4),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: GemoraColors.darkGreen.withValues(alpha: 0.3),
        ),
      ),
      padding: const EdgeInsets.all(20),
      child: const Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            Icons.verified_user_rounded,
            color: GemoraColors.darkGreen,
            size: 28,
          ),
          SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '✓ Ready for Export Officer Review',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    color: GemoraColors.darkGreen,
                  ),
                ),
                SizedBox(height: 6),
                Text(
                  'Automated compliance assessment is complete. An authorized Export Officer will make the final decision.',
                  style: TextStyle(
                    fontSize: 13,
                    color: GemoraColors.deepGreen,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildUnderOfficerReviewSection() {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFFFFBEB),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFFDE68A)),
      ),
      padding: const EdgeInsets.all(20),
      child: const Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            Icons.assignment_ind_outlined,
            color: GemoraColors.darkGold,
            size: 26,
          ),
          SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Under Officer Review',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    color: GemoraColors.darkGreen,
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  'An authorized Export Officer is reviewing your request.',
                  style: TextStyle(fontSize: 13, color: GemoraColors.deepGreen),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRevisionRequiredSection(ExportRequestModel req) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Card(
          elevation: 0,
          color: GemoraColors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
            side: BorderSide(color: Colors.amber.shade300),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.edit_note_rounded,
                      color: Colors.amber.shade900,
                      size: 26,
                    ),
                    const SizedBox(width: 10),
                    const Expanded(
                      child: Text(
                        'Revision Required',
                        style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w800,
                          color: GemoraColors.darkGreen,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                const Text(
                  'The Export Officer requested changes before this request can continue.',
                  style: TextStyle(fontSize: 14, color: GemoraColors.deepGreen),
                ),
                if (req.reviewNotes != null && req.reviewNotes!.isNotEmpty) ...[
                  const SizedBox(height: 14),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFEF3C7),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: const Color(0xFFFDE68A)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Officer Revision Instructions',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w800,
                            color: Color(0xFF92400E),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          req.reviewNotes!,
                          style: const TextStyle(
                            fontSize: 13,
                            color: Color(0xFF78350F),
                            height: 1.35,
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
                    icon: const Icon(Icons.edit, size: 18),
                    label: const Text('Edit Request Details'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: GemoraColors.darkGreen,
                      side: const BorderSide(color: GemoraColors.darkGreen),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        Card(
          elevation: 0,
          color: GemoraColors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
            side: const BorderSide(color: GemoraColors.border),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Ready to Resubmit',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                    color: GemoraColors.darkGreen,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  'After making the requested corrections, resubmit the request for another compliance review.',
                  style: TextStyle(fontSize: 13, color: GemoraColors.deepGreen),
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
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      backgroundColor: GemoraColors.darkGold,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
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
    final confidencePct = (assessment.confidence * 100).toStringAsFixed(0);

    return Card(
      elevation: 0,
      color: GemoraColors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: GemoraColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            height: 4,
            decoration: const BoxDecoration(
              color: GemoraColors.darkGold,
              borderRadius: BorderRadius.vertical(top: Radius.circular(18)),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Row(
                      children: [
                        Icon(
                          Icons.auto_awesome,
                          color: GemoraColors.darkGold,
                          size: 20,
                        ),
                        SizedBox(width: 8),
                        Text(
                          'AI Compliance Advisory',
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w800,
                            color: GemoraColors.darkGreen,
                          ),
                        ),
                      ],
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: GemoraColors.darkGreen,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Text(
                        'Advisory only',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                          color: GemoraColors.gold,
                        ),
                      ),
                    ),
                  ],
                ),
                const Divider(height: 22, color: GemoraColors.border),
                Text(
                  assessment.summary,
                  style: const TextStyle(
                    fontSize: 14,
                    color: GemoraColors.deepGreen,
                    height: 1.45,
                  ),
                ),
                const SizedBox(height: 16),

                // Confidence
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'AI Confidence',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: GemoraColors.mutedText,
                          ),
                        ),
                        Text(
                          '$confidencePct%',
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w800,
                            color: GemoraColors.darkGreen,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: assessment.confidence.clamp(0.0, 1.0),
                        backgroundColor: GemoraColors.cream,
                        valueColor: const AlwaysStoppedAnimation<Color>(
                          GemoraColors.darkGold,
                        ),
                        minHeight: 6,
                      ),
                    ),
                  ],
                ),

                if (assessment.requiresOfficerAttention) ...[
                  const SizedBox(height: 16),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFEF3C7),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: const Color(0xFFFDE68A)),
                    ),
                    child: const Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(
                          Icons.warning_amber_rounded,
                          color: Color(0xFFB45309),
                          size: 20,
                        ),
                        SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Officer Attention Needed',
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w800,
                                  color: Color(0xFF92400E),
                                ),
                              ),
                              SizedBox(height: 2),
                              Text(
                                'Specific items require manual evaluation during officer review.',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Color(0xFF92400E),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ],

                if (assessment.missingRequirements.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  const Text(
                    'Missing Requirements',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                      color: GemoraColors.darkGreen,
                    ),
                  ),
                  const SizedBox(height: 6),
                  ...assessment.missingRequirements.map(
                    (r) => Padding(
                      padding: const EdgeInsets.only(bottom: 4),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(
                            Icons.circle,
                            size: 6,
                            color: GemoraColors.darkGold,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              r,
                              style: const TextStyle(
                                fontSize: 13,
                                color: GemoraColors.deepGreen,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],

                if (assessment.warnings.isNotEmpty) ...[
                  const SizedBox(height: 14),
                  const Text(
                    'Warnings',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                      color: GemoraColors.darkGreen,
                    ),
                  ),
                  const SizedBox(height: 6),
                  ...assessment.warnings.map(
                    (w) => Padding(
                      padding: const EdgeInsets.only(bottom: 4),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(
                            Icons.warning_amber_rounded,
                            size: 14,
                            color: Colors.orange,
                          ),
                          const SizedBox(width: 6),
                          Expanded(
                            child: Text(
                              w,
                              style: const TextStyle(
                                fontSize: 13,
                                color: GemoraColors.deepGreen,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],

                const SizedBox(height: 16),
                const Divider(height: 1, color: GemoraColors.border),
                const SizedBox(height: 12),
                const Row(
                  children: [
                    Icon(
                      Icons.shield_outlined,
                      size: 14,
                      color: GemoraColors.mutedText,
                    ),
                    SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        'Final export decisions are made by an authorized Export Officer.',
                        style: TextStyle(
                          fontSize: 12,
                          color: GemoraColors.mutedText,
                          fontStyle: FontStyle.italic,
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDocumentsEmptyState(bool canEdit) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
      child: Column(
        children: [
          Container(
            width: 56,
            height: 56,
            decoration: BoxDecoration(
              color: GemoraColors.cream,
              borderRadius: BorderRadius.circular(28),
              border: Border.all(color: GemoraColors.border),
            ),
            child: const Icon(
              Icons.description_outlined,
              size: 28,
              color: GemoraColors.darkGold,
            ),
          ),
          const SizedBox(height: 14),
          const Text(
            'No compliance documents yet',
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: GemoraColors.darkGreen,
            ),
          ),
          const SizedBox(height: 6),
          const Text(
            'Add the required export documents before final review.',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 13, color: GemoraColors.mutedText),
          ),
          if (canEdit) ...[
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: _isSubmitting || _isUploadingFile
                  ? null
                  : _openAddDocumentDialog,
              icon: const Icon(Icons.add, size: 18),
              label: const Text('+ Add Document'),
              style: FilledButton.styleFrom(
                backgroundColor: GemoraColors.darkGreen,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(
                  horizontal: 20,
                  vertical: 11,
                ),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  String _getBuyerFriendlyStage(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
        return 'Preparing Export';
      case 'submitted':
        return 'Submitted';
      case 'undercompliancereview':
        return 'Compliance Review';
      case 'underofficerreview':
      case 'waitingforapproval':
        return 'Waiting for Export Officer';
      case 'revisionrequired':
        return 'Revision Required';
      case 'approved':
        return 'Approved';
      case 'rejected':
        return 'Rejected';
      case 'cancelled':
        return 'Cancelled';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final isBuyer = user?.role.trim().toLowerCase() == 'buyer';

    return Scaffold(
      backgroundColor: GemoraColors.cream,
      appBar: AppBar(
        backgroundColor: GemoraColors.darkGreen,
        foregroundColor: Colors.white,
        elevation: 0,
        title: Text(
          isBuyer ? 'Export Tracking' : 'Export Request Details',
          style: const TextStyle(
            fontWeight: FontWeight.w800,
            letterSpacing: 0.8,
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _loadData,
          ),
        ],
      ),
      body: FutureBuilder<ExportRequestModel>(
        future: _requestFuture,
        builder: (context, reqSnapshot) {
          if (reqSnapshot.connectionState == ConnectionState.waiting) {
            return const Center(
              child: CircularProgressIndicator(color: GemoraColors.darkGold),
            );
          }

          if (reqSnapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      size: 60,
                      color: Color(0xFFC62828),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      reqSnapshot.error.toString().replaceAll(
                        'Exception: ',
                        '',
                      ),
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 16),
                    ),
                    const SizedBox(height: 20),
                    FilledButton.icon(
                      onPressed: _loadData,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                      style: FilledButton.styleFrom(
                        backgroundColor: GemoraColors.darkGreen,
                        foregroundColor: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
            );
          }

          final req = reqSnapshot.data!;
          final canEdit = !isBuyer && _isEditableStatus(req.status);
          final statusLower = req.status.toLowerCase();

          return SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // 1. STATUS & TIMELINE PROGRESS CARD
                _buildStatusCard(req, isBuyer),
                const SizedBox(height: 16),

                // 2. PHASE ACTIONS (SELLER ONLY)
                if (!isBuyer) ...[
                  if (statusLower == 'draft') ...[
                    _buildDraftActionSection(canEdit),
                    const SizedBox(height: 16),
                  ] else if (statusLower == 'submitted') ...[
                    _buildSubmittedActionSection(),
                    const SizedBox(height: 16),
                  ] else if (statusLower == 'undercompliancereview') ...[
                    _buildUnderComplianceReviewSection(req),
                    const SizedBox(height: 16),
                  ] else if (statusLower == 'underofficerreview') ...[
                    _buildUnderOfficerReviewSection(),
                    const SizedBox(height: 16),
                  ] else if (statusLower == 'revisionrequired') ...[
                    _buildRevisionRequiredSection(req),
                    const SizedBox(height: 16),
                  ],

                  if (_recentAnalysisResult?.assessment != null) ...[
                    _buildAssessmentSummaryCard(
                      _recentAnalysisResult!.assessment!,
                    ),
                    const SizedBox(height: 16),
                  ],
                ],

                // 3. ORDER INFORMATION CARD (OR COMPACT LEGACY BANNER)
                if (req.orderId != null) ...[
                  _buildOrderInfoCard(req),
                  const SizedBox(height: 16),
                ] else ...[
                  _buildLegacyBanner(),
                ],

                // 4. EXPORT INFORMATION CARD
                _buildExportInfoCard(req),
                const SizedBox(height: 16),

                // 5. COMPLIANCE DOCUMENTS SECTION
                Card(
                  elevation: 0,
                  color: GemoraColors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(18),
                    side: const BorderSide(color: GemoraColors.border),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: FutureBuilder<List<ComplianceDocumentModel>>(
                      future: _documentsFuture,
                      builder: (context, docSnapshot) {
                        final docs = docSnapshot.data ?? [];
                        final hasDocs = docs.isNotEmpty;

                        Widget docsContent;
                        if (docSnapshot.connectionState ==
                            ConnectionState.waiting) {
                          docsContent = const Padding(
                            padding: EdgeInsets.all(16.0),
                            child: Center(
                              child: CircularProgressIndicator(
                                color: GemoraColors.darkGold,
                              ),
                            ),
                          );
                        } else if (docSnapshot.hasError) {
                          docsContent = Padding(
                            padding: const EdgeInsets.all(12.0),
                            child: Text(
                              'Error loading documents: ${docSnapshot.error.toString().replaceAll('Exception: ', '')}',
                              style: const TextStyle(color: Color(0xFFC62828)),
                            ),
                          );
                        } else if (!hasDocs) {
                          docsContent = _buildDocumentsEmptyState(canEdit);
                        } else {
                          docsContent = ListView.separated(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: docs.length,
                            separatorBuilder: (_, _) =>
                                const SizedBox(height: 12),
                            itemBuilder: (context, index) {
                              final doc = docs[index];
                              final displayStatus =
                                  doc.effectiveStatus.isNotEmpty
                                  ? doc.effectiveStatus
                                  : doc.status;
                              final docStatusColor = _getDocStatusColor(
                                displayStatus,
                              );
                              final isThisDocUploading =
                                  _isUploadingFile && _uploadingDocId == doc.id;

                              return Container(
                                padding: const EdgeInsets.all(14),
                                decoration: BoxDecoration(
                                  color: GemoraColors.subtleSurface,
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: GemoraColors.border,
                                  ),
                                ),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.spaceBetween,
                                      children: [
                                        Expanded(
                                          child: Text(
                                            doc.documentType,
                                            style: const TextStyle(
                                              fontWeight: FontWeight.w700,
                                              fontSize: 15,
                                              color: GemoraColors.darkGreen,
                                            ),
                                          ),
                                        ),
                                        Container(
                                          padding: const EdgeInsets.symmetric(
                                            horizontal: 10,
                                            vertical: 4,
                                          ),
                                          decoration: BoxDecoration(
                                            color: docStatusColor.withValues(
                                              alpha: 0.12,
                                            ),
                                            borderRadius: BorderRadius.circular(
                                              12,
                                            ),
                                            border: Border.all(
                                              color: docStatusColor.withValues(
                                                alpha: 0.3,
                                              ),
                                            ),
                                          ),
                                          child: Text(
                                            displayStatus,
                                            style: TextStyle(
                                              color: docStatusColor,
                                              fontSize: 11,
                                              fontWeight: FontWeight.w800,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                    if (doc.effectiveStatusReason != null &&
                                        doc.effectiveStatusReason!.isNotEmpty)
                                      Padding(
                                        padding: const EdgeInsets.only(top: 4),
                                        child: Text(
                                          doc.effectiveStatusReason!,
                                          style: const TextStyle(
                                            fontSize: 12,
                                            color: Color(0xFFC62828),
                                            fontStyle: FontStyle.italic,
                                          ),
                                        ),
                                      ),
                                    if (doc.documentNumber != null &&
                                        doc.documentNumber!.isNotEmpty)
                                      Padding(
                                        padding: const EdgeInsets.only(top: 6),
                                        child: Text(
                                          'Number: ${doc.documentNumber}',
                                          style: const TextStyle(
                                            fontSize: 13,
                                            color: GemoraColors.deepGreen,
                                          ),
                                        ),
                                      ),
                                    if (doc.issuer != null &&
                                        doc.issuer!.isNotEmpty)
                                      Padding(
                                        padding: const EdgeInsets.only(top: 2),
                                        child: Text(
                                          'Issuer: ${doc.issuer}',
                                          style: const TextStyle(
                                            fontSize: 13,
                                            color: GemoraColors.mutedText,
                                          ),
                                        ),
                                      ),
                                    Row(
                                      children: [
                                        if (doc.issueDate != null)
                                          Padding(
                                            padding: const EdgeInsets.only(
                                              top: 4,
                                              right: 12,
                                            ),
                                            child: Text(
                                              'Issue: ${doc.issueDate!.toString().split(' ')[0]}',
                                              style: const TextStyle(
                                                fontSize: 12,
                                                color: GemoraColors.mutedText,
                                              ),
                                            ),
                                          ),
                                        if (doc.expiryDate != null)
                                          Padding(
                                            padding: const EdgeInsets.only(
                                              top: 4,
                                            ),
                                            child: Text(
                                              'Expiry: ${doc.expiryDate!.toString().split(' ')[0]}',
                                              style: const TextStyle(
                                                fontSize: 12,
                                                color: GemoraColors.mutedText,
                                              ),
                                            ),
                                          ),
                                      ],
                                    ),
                                    const SizedBox(height: 10),

                                    // File status & upload
                                    Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.spaceBetween,
                                      children: [
                                        Row(
                                          children: [
                                            Icon(
                                              doc.hasUploadedFile
                                                  ? Icons.check_circle_rounded
                                                  : Icons.warning_amber_rounded,
                                              size: 16,
                                              color: doc.hasUploadedFile
                                                  ? const Color(0xFF2E7D32)
                                                  : const Color(0xFFD97706),
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
                                                    ? const Color(0xFF2E7D32)
                                                    : const Color(0xFFB45309),
                                              ),
                                            ),
                                          ],
                                        ),
                                        if (!doc.hasUploadedFile && canEdit)
                                          FilledButton.icon(
                                            onPressed:
                                                isThisDocUploading ||
                                                    _isSubmitting ||
                                                    _isRunningAnalysis
                                                ? null
                                                : () =>
                                                      _uploadFileForExistingDoc(
                                                        doc,
                                                      ),
                                            icon: isThisDocUploading
                                                ? const SizedBox(
                                                    width: 14,
                                                    height: 14,
                                                    child:
                                                        CircularProgressIndicator(
                                                          strokeWidth: 2,
                                                          color: Colors.white,
                                                        ),
                                                  )
                                                : const Icon(
                                                    Icons.upload_file,
                                                    size: 16,
                                                  ),
                                            label: Text(
                                              isThisDocUploading
                                                  ? 'Uploading...'
                                                  : 'Upload File',
                                            ),
                                            style: FilledButton.styleFrom(
                                              backgroundColor:
                                                  GemoraColors.darkGreen,
                                              foregroundColor: Colors.white,
                                              padding:
                                                  const EdgeInsets.symmetric(
                                                    horizontal: 14,
                                                    vertical: 8,
                                                  ),
                                              textStyle: const TextStyle(
                                                fontSize: 12,
                                                fontWeight: FontWeight.w700,
                                              ),
                                              shape: RoundedRectangleBorder(
                                                borderRadius:
                                                    BorderRadius.circular(8),
                                              ),
                                            ),
                                          ),
                                      ],
                                    ),
                                  ],
                                ),
                              );
                            },
                          );
                        }

                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              crossAxisAlignment: CrossAxisAlignment.center,
                              children: [
                                const Row(
                                  children: [
                                    Icon(
                                      Icons.folder_outlined,
                                      color: GemoraColors.darkGold,
                                      size: 20,
                                    ),
                                    SizedBox(width: 8),
                                    Text(
                                      'COMPLIANCE DOCUMENTS',
                                      style: TextStyle(
                                        fontSize: 13,
                                        fontWeight: FontWeight.w800,
                                        color: GemoraColors.mutedText,
                                        letterSpacing: 0.8,
                                      ),
                                    ),
                                  ],
                                ),
                                if (canEdit && hasDocs)
                                  OutlinedButton.icon(
                                    onPressed: _isSubmitting || _isUploadingFile
                                        ? null
                                        : _openAddDocumentDialog,
                                    icon: const Icon(Icons.add, size: 16),
                                    label: const Text('Add Document'),
                                    style: OutlinedButton.styleFrom(
                                      foregroundColor: GemoraColors.darkGreen,
                                      side: const BorderSide(
                                        color: GemoraColors.border,
                                      ),
                                      padding: const EdgeInsets.symmetric(
                                        horizontal: 12,
                                        vertical: 6,
                                      ),
                                      textStyle: const TextStyle(fontSize: 12),
                                      shape: RoundedRectangleBorder(
                                        borderRadius: BorderRadius.circular(8),
                                      ),
                                    ),
                                  ),
                              ],
                            ),
                            if (statusLower == 'revisionrequired') ...[
                              const SizedBox(height: 8),
                              const Text(
                                'Update the requested information or provide corrected documents before resubmitting.',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Color(0xFFB45309),
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ],
                            const Divider(
                              height: 22,
                              color: GemoraColors.border,
                            ),
                            docsContent,
                          ],
                        );
                      },
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

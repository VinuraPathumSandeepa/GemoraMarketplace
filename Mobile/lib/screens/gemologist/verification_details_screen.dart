import 'dart:async';

import 'package:flutter/material.dart';

import '../../config/api_config.dart';
import '../../models/gem_ai_analysis_model.dart';
import '../../models/gem_verification_model.dart';
import '../../services/gem_verification_service.dart';
import 'certificate_viewer_screen.dart';

class VerificationDetailsScreen extends StatefulWidget {
  final int verificationId;

  const VerificationDetailsScreen({super.key, required this.verificationId});

  @override
  State<VerificationDetailsScreen> createState() =>
      _VerificationDetailsScreenState();
}

class _VerificationDetailsScreenState extends State<VerificationDetailsScreen> {
  final GemVerificationService _service = GemVerificationService();

  GemVerificationModel? _verification;
  GemAiAnalysisModel? _aiResult;

  bool _loading = true;
  bool _runningAi = false;
  bool _reviewing = false;
  bool _loadingCertificate = false;

  int _aiProgressStep = 0;

  Timer? _aiProgressTimer;

  String? _error;

  static const Color _darkEmerald = Color(0xFF08251E);

  static const Color _emerald = Color(0xFF16483B);

  static const Color _gold = Color(0xFFC99242);

  static const Color _ivory = Color(0xFFFAF7F0);

  static const Color _border = Color(0xFFE7DFD2);

  static const Color _muted = Color(0xFF697771);

  static const List<String> _aiProgressSteps = [
    'Preparing verification evidence',
    'Analyzing available gemstone evidence',
    'Evaluating verification signals',
    'Preparing professional findings',
  ];

  @override
  void initState() {
    super.initState();

    _load();
  }

  @override
  void dispose() {
    _aiProgressTimer?.cancel();

    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final verification = await _service.getVerification(
        widget.verificationId,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _verification = verification;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _error = _cleanError(error);
      });
    } finally {
      if (mounted) {
        setState(() {
          _loading = false;
        });
      }
    }
  }

  // ============================================================
  // AI ANALYSIS
  // ============================================================

  Future<void> _runAiAnalysis() async {
    if (_runningAi) {
      return;
    }

    _aiProgressTimer?.cancel();

    setState(() {
      _runningAi = true;
      _aiProgressStep = 0;
    });

    _aiProgressTimer = Timer.periodic(const Duration(milliseconds: 1400), (
      timer,
    ) {
      if (!mounted) {
        timer.cancel();
        return;
      }

      if (_aiProgressStep < _aiProgressSteps.length - 1) {
        setState(() {
          _aiProgressStep++;
        });
      }
    });

    try {
      final result = await _service.runAiAnalysis(widget.verificationId);

      final refreshed = await _service.getVerification(widget.verificationId);

      if (!mounted) {
        return;
      }

      _aiProgressTimer?.cancel();

      setState(() {
        _verification = refreshed;
        _aiResult = result;
        _runningAi = false;
        _aiProgressStep = _aiProgressSteps.length - 1;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          behavior: SnackBarBehavior.floating,
          content: Row(
            children: [
              const Icon(Icons.auto_awesome_rounded, color: Colors.white),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  'AI analysis completed: '
                  '${_humanize(result.status)}',
                ),
              ),
            ],
          ),
        ),
      );
    } catch (error) {
      _aiProgressTimer?.cancel();

      if (!mounted) {
        return;
      }

      setState(() {
        _runningAi = false;
        _aiProgressStep = 0;
      });

      _showError(_cleanError(error));
    }
  }

  // ============================================================
  // CERTIFICATE VIEWER
  // ============================================================

  Future<void> _openCertificate() async {
    final verification = _verification;

    if (verification == null || _loadingCertificate) {
      return;
    }

    setState(() {
      _loadingCertificate = true;
    });

    try {
      final document = await _service.loadProtectedCertificate(
        verification.gemListingId,
      );

      if (!mounted) {
        return;
      }

      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => CertificateViewerScreen(
            document: document,
            certificateNumber: verification.certificateNumber,
            certificateAuthority: verification.certificateAuthority,
          ),
        ),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }

      _showError(_cleanError(error));
    } finally {
      if (mounted) {
        setState(() {
          _loadingCertificate = false;
        });
      }
    }
  }

  // ============================================================
  // HUMAN REVIEW
  // ============================================================

  Future<void> _startReview(String decision) async {
    if (_reviewing || _runningAi) {
      return;
    }

    final requiresNotes =
        decision == 'ChangesRequested' || decision == 'Rejected';

    final notesController = TextEditingController();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) {
        return AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          title: Row(
            children: [
              Container(
                width: 42,
                height: 42,
                decoration: BoxDecoration(
                  color: _decisionColor(decision).withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(
                  _decisionIcon(decision),
                  color: _decisionColor(decision),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(child: Text(_decisionTitle(decision))),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(_decisionMessage(decision)),
              const SizedBox(height: 18),
              TextField(
                controller: notesController,
                minLines: 3,
                maxLines: 6,
                maxLength: 2000,
                decoration: InputDecoration(
                  labelText: requiresNotes
                      ? 'Review Notes *'
                      : 'Review Notes (Optional)',
                  hintText: requiresNotes
                      ? 'Explain the reason clearly to the seller.'
                      : 'Add professional review notes if required.',
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
              ),
              if (requiresNotes)
                const Row(
                  children: [
                    Icon(
                      Icons.info_outline_rounded,
                      color: Color(0xFFB7791F),
                      size: 16,
                    ),
                    SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        'Review notes are required for this decision.',
                        style: TextStyle(
                          color: Color(0xFF8A651E),
                          fontSize: 11,
                        ),
                      ),
                    ),
                  ],
                ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(dialogContext, false);
              },
              child: const Text('Cancel'),
            ),
            FilledButton.icon(
              style: FilledButton.styleFrom(
                backgroundColor: _decisionColor(decision),
              ),
              onPressed: () {
                if (requiresNotes && notesController.text.trim().isEmpty) {
                  ScaffoldMessenger.of(dialogContext).showSnackBar(
                    const SnackBar(
                      content: Text(
                        'Please enter review notes before continuing.',
                      ),
                    ),
                  );

                  return;
                }

                Navigator.pop(dialogContext, true);
              },
              icon: Icon(_decisionIcon(decision)),
              label: Text(_decisionButtonLabel(decision)),
            ),
          ],
        );
      },
    );

    if (confirmed != true) {
      notesController.dispose();
      return;
    }

    final notes = notesController.text.trim();

    notesController.dispose();

    await _submitReview(decision: decision, notes: notes);
  }

  Future<void> _submitReview({
    required String decision,
    required String notes,
  }) async {
    setState(() {
      _reviewing = true;
    });

    try {
      final updated = await _service.reviewVerification(
        verificationId: widget.verificationId,
        decision: decision,
        reviewNotes: notes.isEmpty ? null : notes,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _verification = updated;
      });

      await showDialog<void>(
        context: context,
        builder: (dialogContext) {
          return AlertDialog(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(20),
            ),
            icon: Icon(
              _decisionIcon(decision),
              color: _decisionColor(decision),
              size: 46,
            ),
            title: Text(_decisionTitle(decision)),
            content: Text(
              'The verification was successfully updated to '
              '${_humanize(decision)}.',
              textAlign: TextAlign.center,
            ),
            actionsAlignment: MainAxisAlignment.center,
            actions: [
              FilledButton(
                onPressed: () {
                  Navigator.pop(dialogContext);
                },
                child: const Text('Done'),
              ),
            ],
          );
        },
      );

      if (!mounted) {
        return;
      }

      Navigator.pop(context, true);
    } catch (error) {
      if (!mounted) {
        return;
      }

      _showError(_cleanError(error));
    } finally {
      if (mounted) {
        setState(() {
          _reviewing = false;
        });
      }
    }
  }

  // ============================================================
  // HELPERS
  // ============================================================

  String? _imageUrl(GemVerificationModel verification) {
    final value = verification.primaryImageUrl;

    if (value == null || value.trim().isEmpty) {
      return null;
    }

    if (value.startsWith('http://') || value.startsWith('https://')) {
      return value;
    }

    return '${ApiConfig.serverUrl}'
        '${value.startsWith('/') ? '' : '/'}'
        '$value';
  }

  String _cleanError(Object error) {
    return error.toString().replaceFirst('Exception: ', '');
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(behavior: SnackBarBehavior.floating, content: Text(message)),
    );
  }

  String _humanize(String value) {
    switch (value.toLowerCase()) {
      case 'pendingverification':
        return 'Pending Verification';

      case 'changesrequested':
        return 'Changes Requested';

      case 'needsmoreevidence':
        return 'Needs More Evidence';

      case 'awaitingmodelanalysis':
        return 'Awaiting Model Analysis';

      case 'notstarted':
        return 'Not Started';

      case 'processing':
        return 'Processing';

      case 'completed':
        return 'Completed';

      default:
        return value;
    }
  }

  String _dateText(DateTime? value) {
    if (value == null) {
      return '—';
    }

    final local = value.toLocal();

    final month = local.month.toString().padLeft(2, '0');

    final day = local.day.toString().padLeft(2, '0');

    final hour = local.hour.toString().padLeft(2, '0');

    final minute = local.minute.toString().padLeft(2, '0');

    return '${local.year}-$month-$day $hour:$minute';
  }

  String _decisionTitle(String decision) {
    switch (decision) {
      case 'Approved':
        return 'Approve Gemstone';

      case 'ChangesRequested':
        return 'Request Changes';

      case 'Rejected':
        return 'Reject Gemstone';

      default:
        return decision;
    }
  }

  String _decisionButtonLabel(String decision) {
    switch (decision) {
      case 'Approved':
        return 'Approve';

      case 'ChangesRequested':
        return 'Request Changes';

      case 'Rejected':
        return 'Reject';

      default:
        return 'Confirm';
    }
  }

  IconData _decisionIcon(String decision) {
    switch (decision) {
      case 'Approved':
        return Icons.verified_rounded;

      case 'ChangesRequested':
        return Icons.edit_note_rounded;

      case 'Rejected':
        return Icons.cancel_outlined;

      default:
        return Icons.fact_check_outlined;
    }
  }

  String _decisionMessage(String decision) {
    switch (decision) {
      case 'Approved':
        return 'Confirm that the submitted gemstone evidence is '
            'acceptable and approve this listing.';

      case 'ChangesRequested':
        return 'Send the listing back to the seller with clear '
            'instructions about the evidence or information that '
            'must be improved before resubmission.';

      case 'Rejected':
        return 'Reject this verification request. A clear '
            'professional reason must be provided to the seller.';

      default:
        return '';
    }
  }

  static Color _decisionColor(String decision) {
    switch (decision) {
      case 'Approved':
        return const Color(0xFF247A50);

      case 'ChangesRequested':
        return const Color(0xFFB7791F);

      case 'Rejected':
        return const Color(0xFFA5433A);

      default:
        return _emerald;
    }
  }

  double _normalizedConfidence(double? value) {
    if (value == null) {
      return 0;
    }

    final normalized = value > 1 ? value / 100 : value;

    return normalized.clamp(0.0, 1.0);
  }

  String _confidenceText(double? value) {
    if (value == null) {
      return 'Not available';
    }

    return '${(_normalizedConfidence(value) * 100).toStringAsFixed(1)}%';
  }

  Color _confidenceColor(double? value) {
    final score = _normalizedConfidence(value);

    if (score >= 0.75) {
      return const Color(0xFF247A50);
    }

    if (score >= 0.50) {
      return const Color(0xFFB7791F);
    }

    return const Color(0xFFA5433A);
  }

  // ============================================================
  // PAGE
  // ============================================================

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        backgroundColor: _darkEmerald,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Verification Review',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            Text(
              'Gemologist Workspace',
              style: TextStyle(fontSize: 11, color: Colors.white60),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            onPressed: _loading || _runningAi || _reviewing ? null : _load,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.error_outline_rounded,
                color: Color(0xFFA5433A),
                size: 60,
              ),
              const SizedBox(height: 16),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 20),
              FilledButton(onPressed: _load, child: const Text('Try Again')),
            ],
          ),
        ),
      );
    }

    final verification = _verification;

    if (verification == null) {
      return const Center(child: Text('Verification was not found.'));
    }

    final image = _imageUrl(verification);

    final isPending = verification.decision.toLowerCase() == 'pending';

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 18, 16, 40),
      children: [
        Container(
          height: 230,
          decoration: BoxDecoration(
            color: const Color(0xFFF0ECE3),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: _border),
          ),
          clipBehavior: Clip.antiAlias,
          child: image == null
              ? const Center(
                  child: Icon(Icons.diamond_outlined, color: _gold, size: 82),
                )
              : Image.network(
                  image,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) {
                    return const Center(
                      child: Icon(
                        Icons.broken_image_outlined,
                        color: _gold,
                        size: 64,
                      ),
                    );
                  },
                ),
        ),

        const SizedBox(height: 20),

        Text(
          verification.title,
          style: const TextStyle(
            color: _darkEmerald,
            fontSize: 24,
            fontWeight: FontWeight.w800,
          ),
        ),

        const SizedBox(height: 6),

        Text(
          verification.gemType,
          style: const TextStyle(
            color: _gold,
            fontSize: 14,
            fontWeight: FontWeight.w700,
          ),
        ),

        const SizedBox(height: 12),

        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _statusChip(
              'Decision: ${_humanize(verification.decision)}',
              const Color(0xFF3E6688),
            ),
            _statusChip(
              'Listing: ${_humanize(verification.listingStatus)}',
              _gold,
            ),
            _statusChip('AI: ${_humanize(verification.aiStatus)}', _emerald),
          ],
        ),

        const SizedBox(height: 24),

        _section(
          title: 'Gemstone Information',
          icon: Icons.diamond_outlined,
          children: [
            _detailRow('Seller', verification.sellerName),
            _detailRow(
              'Carat Weight',
              '${verification.caratWeight.toStringAsFixed(2)} ct',
            ),
            _detailRow('Color', verification.color),
            _detailRow('Clarity', verification.clarity),
            _detailRow('Cut', verification.cut),
            _detailRow(
              'Price',
              '${verification.currency} '
                  '${verification.price.toStringAsFixed(2)}',
            ),
            _detailRow('Submitted', _dateText(verification.createdAt)),
            const SizedBox(height: 12),
            Text(
              verification.description,
              style: const TextStyle(color: _muted, fontSize: 13, height: 1.55),
            ),
          ],
        ),

        const SizedBox(height: 16),

        _section(
          title: 'Certificate Evidence',
          icon: Icons.workspace_premium_outlined,
          children: [
            _detailRow(
              'Certificate Number',
              verification.certificateNumber ?? 'Not provided',
            ),
            _detailRow(
              'Authority',
              verification.certificateAuthority ?? 'Not provided',
            ),

            const SizedBox(height: 8),

            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFF5F8F6),
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Row(
                children: [
                  Icon(Icons.shield_outlined, color: _emerald, size: 19),
                  SizedBox(width: 9),
                  Expanded(
                    child: Text(
                      'The certificate opens securely inside Gemora. '
                      'Private storage links are never shown to the user.',
                      style: TextStyle(
                        color: _muted,
                        fontSize: 11,
                        height: 1.45,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 14),

            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: _loadingCertificate ? null : _openCertificate,
                icon: _loadingCertificate
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.visibility_outlined),
                label: Text(
                  _loadingCertificate
                      ? 'Opening Secure Certificate...'
                      : 'View Protected Certificate',
                ),
              ),
            ),
          ],
        ),

        const SizedBox(height: 16),

        _section(
          title: 'AI-Assisted Analysis',
          icon: Icons.psychology_alt_outlined,
          children: [
            const Text(
              'AI provides supporting evidence only. '
              'The final verification decision always remains '
              'with the Gemologist.',
              style: TextStyle(color: _muted, height: 1.5, fontSize: 12),
            ),

            const SizedBox(height: 14),

            if (!_runningAi)
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  style: FilledButton.styleFrom(
                    backgroundColor: _emerald,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  onPressed: !isPending ? null : _runAiAnalysis,
                  icon: const Icon(Icons.auto_awesome_rounded),
                  label: const Text('Run AI Analysis'),
                ),
              ),

            AnimatedSwitcher(
              duration: const Duration(milliseconds: 350),
              child: _runningAi
                  ? _aiProcessingCard()
                  : _aiResult != null
                  ? Padding(
                      padding: const EdgeInsets.only(top: 16),
                      child: _aiResultView(_aiResult!),
                    )
                  : verification.aiSuggestedGemType != null ||
                        verification.aiFindings != null ||
                        verification.aiRiskFlags != null
                  ? Padding(
                      padding: const EdgeInsets.only(top: 16),
                      child: _storedAiView(verification),
                    )
                  : const SizedBox.shrink(),
            ),
          ],
        ),

        const SizedBox(height: 16),

        if (isPending)
          _section(
            title: 'Final Gemologist Review',
            icon: Icons.fact_check_outlined,
            children: [
              const Text(
                'Review all available evidence before making '
                'the final professional decision.',
                style: TextStyle(color: _muted, fontSize: 12, height: 1.5),
              ),

              if (_runningAi) ...[
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFFF7E8),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Row(
                    children: [
                      Icon(
                        Icons.hourglass_top_rounded,
                        color: Color(0xFFB7791F),
                        size: 18,
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          'Final review controls are temporarily '
                          'disabled while AI analysis is running.',
                          style: TextStyle(
                            color: Color(0xFF7A5A1B),
                            fontSize: 11,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],

              const SizedBox(height: 16),

              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFF247A50),
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  onPressed: _reviewing || _runningAi
                      ? null
                      : () => _startReview('Approved'),
                  icon: const Icon(Icons.verified_rounded),
                  label: const Text('Approve'),
                ),
              ),

              const SizedBox(height: 10),

              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  style: OutlinedButton.styleFrom(
                    foregroundColor: const Color(0xFFB7791F),
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  onPressed: _reviewing || _runningAi
                      ? null
                      : () => _startReview('ChangesRequested'),
                  icon: const Icon(Icons.edit_note_rounded),
                  label: const Text('Request Changes'),
                ),
              ),

              const SizedBox(height: 10),

              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  style: OutlinedButton.styleFrom(
                    foregroundColor: const Color(0xFFA5433A),
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  onPressed: _reviewing || _runningAi
                      ? null
                      : () => _startReview('Rejected'),
                  icon: const Icon(Icons.cancel_outlined),
                  label: const Text('Reject'),
                ),
              ),

              if (_reviewing) ...[
                const SizedBox(height: 16),
                const Center(child: CircularProgressIndicator()),
              ],
            ],
          )
        else
          _section(
            title: 'Review Completed',
            icon: Icons.verified_outlined,
            children: [
              _detailRow('Decision', _humanize(verification.decision)),
              _detailRow('Gemologist', verification.gemologistName ?? '—'),
              _detailRow('Reviewed At', _dateText(verification.reviewedAt)),
              if (verification.reviewNotes != null) ...[
                const SizedBox(height: 12),
                Text(
                  verification.reviewNotes!,
                  style: const TextStyle(color: _muted, height: 1.5),
                ),
              ],
            ],
          ),
      ],
    );
  }

  // ============================================================
  // AI PROCESSING UI
  // ============================================================

  Widget _aiProcessingCard() {
    return Container(
      key: const ValueKey('ai-processing'),
      margin: const EdgeInsets.only(top: 16),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF0D332A), Color(0xFF174D3E)],
        ),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(
                  strokeWidth: 2.2,
                  color: Color(0xFFE4BC74),
                ),
              ),
              SizedBox(width: 11),
              Expanded(
                child: Text(
                  'Gemora AI is analyzing this verification',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          ),

          const SizedBox(height: 8),

          const Text(
            'Processing time depends on the available image '
            'and certificate evidence. Progress shown below '
            'is an estimated interface status.',
            style: TextStyle(color: Colors.white60, fontSize: 10, height: 1.4),
          ),

          const SizedBox(height: 16),

          LinearProgressIndicator(
            value: (_aiProgressStep + 1) / (_aiProgressSteps.length + 1),
            minHeight: 5,
            borderRadius: BorderRadius.circular(20),
            backgroundColor: Colors.white.withValues(alpha: 0.12),
            color: const Color(0xFFE4BC74),
          ),

          const SizedBox(height: 18),

          for (var index = 0; index < _aiProgressSteps.length; index++)
            _aiProgressRow(index, _aiProgressSteps[index]),
        ],
      ),
    );
  }

  Widget _aiProgressRow(int index, String title) {
    final completed = index < _aiProgressStep;

    final active = index == _aiProgressStep;

    return Padding(
      padding: const EdgeInsets.only(bottom: 11),
      child: Row(
        children: [
          SizedBox(
            width: 24,
            height: 24,
            child: completed
                ? const Icon(
                    Icons.check_circle_rounded,
                    color: Color(0xFF7DD3A7),
                    size: 20,
                  )
                : active
                ? const Padding(
                    padding: EdgeInsets.all(4),
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Color(0xFFE4BC74),
                    ),
                  )
                : Icon(
                    Icons.radio_button_unchecked_rounded,
                    color: Colors.white.withValues(alpha: 0.30),
                    size: 19,
                  ),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              title,
              style: TextStyle(
                color: completed || active
                    ? Colors.white
                    : Colors.white.withValues(alpha: 0.38),
                fontSize: 11,
                fontWeight: active ? FontWeight.w700 : FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ============================================================
  // AI RESULT
  // ============================================================

  Widget _aiResultView(GemAiAnalysisModel result) {
    final confidence = _normalizedConfidence(result.confidenceScore);

    final confidenceColor = _confidenceColor(result.confidenceScore);

    return Container(
      key: const ValueKey('ai-result'),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFF7FAF8),
        border: Border.all(color: const Color(0xFFD8E7E0)),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: _emerald.withValues(alpha: 0.09),
                  borderRadius: BorderRadius.circular(11),
                ),
                child: const Icon(
                  Icons.auto_awesome_rounded,
                  color: _emerald,
                  size: 21,
                ),
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'AI Analysis Result',
                      style: TextStyle(
                        color: _darkEmerald,
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      _humanize(result.status),
                      style: const TextStyle(color: _muted, fontSize: 11),
                    ),
                  ],
                ),
              ),
              const Icon(
                Icons.check_circle_outline_rounded,
                color: Color(0xFF247A50),
              ),
            ],
          ),

          const SizedBox(height: 18),

          if (result.suggestedGemType != null)
            _detailRow('Suggested Type', result.suggestedGemType!),

          _detailRow('Image Analyzed', result.imageAnalyzed ? 'Yes' : 'No'),

          const SizedBox(height: 8),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Confidence',
                style: TextStyle(
                  color: _muted,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                _confidenceText(result.confidenceScore),
                style: TextStyle(
                  color: confidenceColor,
                  fontSize: 12,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),

          const SizedBox(height: 7),

          ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: LinearProgressIndicator(
              value: confidence,
              minHeight: 7,
              backgroundColor: const Color(0xFFE4E7E5),
              color: confidenceColor,
            ),
          ),

          if (result.findings.trim().isNotEmpty) ...[
            const SizedBox(height: 20),
            _resultHeading(Icons.description_outlined, 'Findings'),
            const SizedBox(height: 7),
            Text(
              result.findings,
              style: const TextStyle(color: _muted, fontSize: 12, height: 1.55),
            ),
          ],

          _resultList(
            icon: Icons.visibility_outlined,
            title: 'Visual Observations',
            values: result.visualObservations,
            iconColor: const Color(0xFF3E6688),
          ),

          _resultList(
            icon: Icons.warning_amber_rounded,
            title: 'Risk Flags',
            values: result.riskFlags,
            iconColor: const Color(0xFFA5433A),
          ),

          _resultList(
            icon: Icons.report_problem_outlined,
            title: 'Validation Issues',
            values: result.validationIssues,
            iconColor: const Color(0xFFB7791F),
          ),

          _resultList(
            icon: Icons.task_alt_rounded,
            title: 'Agent Steps Completed',
            values: result.stepsCompleted,
            iconColor: const Color(0xFF247A50),
          ),
        ],
      ),
    );
  }

  Widget _storedAiView(GemVerificationModel verification) {
    final confidence = _normalizedConfidence(verification.aiConfidenceScore);

    final confidenceColor = _confidenceColor(verification.aiConfidenceScore);

    return Container(
      key: const ValueKey('stored-ai-result'),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFF7FAF8),
        border: Border.all(color: const Color(0xFFD8E7E0)),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _detailRow('AI Status', _humanize(verification.aiStatus)),

          if (verification.aiSuggestedGemType != null)
            _detailRow('Suggested Type', verification.aiSuggestedGemType!),

          const SizedBox(height: 8),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Confidence',
                style: TextStyle(
                  color: _muted,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                _confidenceText(verification.aiConfidenceScore),
                style: TextStyle(
                  color: confidenceColor,
                  fontSize: 12,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),

          const SizedBox(height: 7),

          ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: LinearProgressIndicator(
              value: confidence,
              minHeight: 7,
              backgroundColor: const Color(0xFFE4E7E5),
              color: confidenceColor,
            ),
          ),

          if (verification.aiFindings != null) ...[
            const SizedBox(height: 18),
            _resultHeading(Icons.description_outlined, 'Stored Findings'),
            const SizedBox(height: 7),
            Text(
              verification.aiFindings!,
              style: const TextStyle(color: _muted, fontSize: 12, height: 1.55),
            ),
          ],

          if (verification.aiRiskFlags != null) ...[
            const SizedBox(height: 18),
            _resultHeading(
              Icons.warning_amber_rounded,
              'Stored Risk Flags',
              color: const Color(0xFFA5433A),
            ),
            const SizedBox(height: 7),
            Text(
              verification.aiRiskFlags!,
              style: const TextStyle(
                color: Color(0xFFA5433A),
                fontSize: 12,
                height: 1.5,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _resultHeading(IconData icon, String title, {Color color = _emerald}) {
    return Row(
      children: [
        Icon(icon, size: 18, color: color),
        const SizedBox(width: 7),
        Text(
          title,
          style: TextStyle(
            color: _darkEmerald,
            fontSize: 13,
            fontWeight: FontWeight.w800,
          ),
        ),
      ],
    );
  }

  Widget _resultList({
    required IconData icon,
    required String title,
    required List<String> values,
    required Color iconColor,
  }) {
    if (values.isEmpty) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.only(top: 18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _resultHeading(icon, title, color: iconColor),
          const SizedBox(height: 8),
          ...values.map(
            (value) => Container(
              width: double.infinity,
              margin: const EdgeInsets.only(bottom: 7),
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: _border),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.circle, color: iconColor, size: 7),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      value,
                      style: const TextStyle(
                        color: _muted,
                        fontSize: 11,
                        height: 1.45,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ============================================================
  // REUSABLE UI
  // ============================================================

  Widget _section({
    required String title,
    required IconData icon,
    required List<Widget> children,
  }) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: _border),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: _gold),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(
                    color: _darkEmerald,
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          ...children,
        ],
      ),
    );
  }

  Widget _detailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 9),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 125,
            child: Text(
              label,
              style: const TextStyle(
                color: _muted,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                color: _darkEmerald,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _statusChip(String text, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(30),
      ),
      child: Text(
        text,
        style: TextStyle(
          color: color,
          fontSize: 10,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

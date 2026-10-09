import 'package:flutter/material.dart';

import '../../config/api_config.dart';
import '../../models/gem_verification_model.dart';
import '../../services/gem_verification_service.dart';
import 'verification_details_screen.dart';

class VerificationQueueScreen extends StatefulWidget {
  const VerificationQueueScreen({super.key});

  @override
  State<VerificationQueueScreen> createState() =>
      _VerificationQueueScreenState();
}

class _VerificationQueueScreenState extends State<VerificationQueueScreen> {
  final GemVerificationService _service = GemVerificationService();

  List<GemVerificationModel> _verifications = [];

  bool _loading = true;

  String? _error;

  static const Color _darkEmerald = Color(0xFF08251E);

  static const Color _emerald = Color(0xFF16483B);

  static const Color _gold = Color(0xFFC99242);

  static const Color _ivory = Color(0xFFFAF7F0);

  static const Color _border = Color(0xFFE7DFD2);

  static const Color _muted = Color(0xFF697771);

  @override
  void initState() {
    super.initState();

    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final data = await _service.getPendingVerifications();

      if (!mounted) {
        return;
      }

      setState(() {
        _verifications = data;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _error = error.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _loading = false;
        });
      }
    }
  }

  Future<void> _openVerification(GemVerificationModel verification) async {
    await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => VerificationDetailsScreen(
          verificationId: verification.verificationId,
        ),
      ),
    );

    if (!mounted) {
      return;
    }

    await _load();
  }

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

  String _aiStatusLabel(String status) {
    switch (status.toLowerCase()) {
      case 'notstarted':
        return 'AI Not Started';

      case 'processing':
        return 'AI Processing';

      case 'needsmoreevidence':
        return 'Needs More Evidence';

      case 'awaitingmodelanalysis':
        return 'Awaiting AI Analysis';

      case 'completed':
        return 'AI Completed';

      default:
        return status.isEmpty ? 'AI Not Started' : status;
    }
  }

  Color _aiStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'completed':
        return const Color(0xFF247A50);

      case 'needsmoreevidence':
        return const Color(0xFFB7791F);

      case 'processing':
      case 'awaitingmodelanalysis':
        return const Color(0xFF3E6688);

      default:
        return _muted;
    }
  }

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
              'Verification Queue',
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
            tooltip: 'Refresh queue',
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: RefreshIndicator(onRefresh: _load, child: _buildBody()),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 90),
          const Icon(
            Icons.error_outline_rounded,
            size: 64,
            color: Color(0xFFA5433A),
          ),
          const SizedBox(height: 18),
          const Text(
            'Unable to load verification queue',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: _darkEmerald,
              fontSize: 20,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            _error!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: _muted, height: 1.5),
          ),
          const SizedBox(height: 22),
          FilledButton(
            onPressed: _load,
            style: FilledButton.styleFrom(backgroundColor: _emerald),
            child: const Text('Try Again'),
          ),
        ],
      );
    }

    if (_verifications.isEmpty) {
      return ListView(
        padding: const EdgeInsets.all(28),
        children: const [
          SizedBox(height: 100),
          Icon(Icons.verified_outlined, size: 72, color: _gold),
          SizedBox(height: 18),
          Text(
            'Verification queue is clear',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: _darkEmerald,
              fontSize: 22,
              fontWeight: FontWeight.w800,
            ),
          ),
          SizedBox(height: 10),
          Text(
            'There are currently no gemstone listings waiting for Gemologist review.',
            textAlign: TextAlign.center,
            style: TextStyle(color: _muted, height: 1.5),
          ),
        ],
      );
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 18, 16, 32),
      children: [
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: _darkEmerald,
            borderRadius: BorderRadius.circular(18),
          ),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Icon(
                  Icons.fact_check_outlined,
                  color: Color(0xFFE4BC74),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${_verifications.length} pending verification${_verifications.length == 1 ? '' : 's'}',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 17,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'Review submitted gemstone evidence before making a final human decision.',
                      style: TextStyle(
                        color: Colors.white70,
                        fontSize: 11,
                        height: 1.4,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 18),
        ..._verifications.map(_verificationCard),
      ],
    );
  }

  Widget _verificationCard(GemVerificationModel verification) {
    final image = _imageUrl(verification);

    final aiColor = _aiStatusColor(verification.aiStatus);

    return Card(
      elevation: 0,
      margin: const EdgeInsets.only(bottom: 14),
      color: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: _border),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => _openVerification(verification),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 92,
                height: 92,
                decoration: BoxDecoration(
                  color: const Color(0xFFF2EFE8),
                  borderRadius: BorderRadius.circular(14),
                ),
                clipBehavior: Clip.antiAlias,
                child: image == null
                    ? const Icon(Icons.diamond_outlined, color: _gold, size: 40)
                    : Image.network(
                        image,
                        fit: BoxFit.cover,
                        errorBuilder: (context, error, stackTrace) =>
                            const Icon(
                              Icons.diamond_outlined,
                              color: _gold,
                              size: 40,
                            ),
                      ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      verification.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _darkEmerald,
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      verification.gemType,
                      style: const TextStyle(
                        color: _gold,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      '${verification.caratWeight.toStringAsFixed(2)} ct • ${verification.currency} ${verification.price.toStringAsFixed(2)}',
                      style: const TextStyle(color: _muted, fontSize: 11),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      'Seller: ${verification.sellerName}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(color: _muted, fontSize: 11),
                    ),
                    const SizedBox(height: 9),
                    Wrap(
                      spacing: 6,
                      runSpacing: 6,
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 9,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: const Color(0xFF3E6688)
                                .withValues(alpha: 0.10),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: const Text(
                            'Pending Review',
                            style: TextStyle(
                              color: Color(0xFF3E6688),
                              fontSize: 10,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 9,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: aiColor.withValues(alpha: 0.10),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Text(
                            _aiStatusLabel(verification.aiStatus),
                            style: TextStyle(
                              color: aiColor,
                              fontSize: 10,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 6),
              const Icon(Icons.chevron_right_rounded, color: _muted),
            ],
          ),
        ),
      ),
    );
  }
}

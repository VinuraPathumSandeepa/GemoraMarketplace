import 'package:flutter/material.dart';

import '../../config/api_config.dart';
import '../../models/gem_listing_model.dart';
import '../../services/gem_listing_service.dart';
import 'edit_gem_listing_screen.dart';

class GemListingDetailsScreen extends StatefulWidget {
  final int listingId;

  const GemListingDetailsScreen({super.key, required this.listingId});

  @override
  State<GemListingDetailsScreen> createState() =>
      _GemListingDetailsScreenState();
}

class _GemListingDetailsScreenState extends State<GemListingDetailsScreen> {
  final GemListingService _service = GemListingService();

  GemListingModel? _listing;

  bool _loading = true;
  bool _working = false;

  String? _error;

  static const Color _darkEmerald = Color(0xFF08251E);

  static const Color _emerald = Color(0xFF16483B);

  static const Color _gold = Color(0xFFC99242);

  static const Color _ivory = Color(0xFFFAF7F0);

  static const Color _border = Color(0xFFE7DFD2);

  static const Color _muted = Color(0xFF697771);

  static const Color _danger = Color(0xFFA5433A);

  static const Color _success = Color(0xFF247A50);

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
      final listing = await _service.getListing(widget.listingId);

      if (!mounted) {
        return;
      }

      setState(() {
        _listing = listing;
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

  String? _imageUrl() {
    final url = _listing?.primaryImageUrl;

    if (url == null || url.trim().isEmpty) {
      return null;
    }

    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }

    return '${ApiConfig.serverUrl}'
        '${url.startsWith('/') ? '' : '/'}'
        '$url';
  }

  String _statusLabel(String status) {
    switch (status.toLowerCase()) {
      case 'pendingverification':
        return 'Pending Verification';

      case 'changesrequested':
        return 'Changes Requested';

      case 'approved':
        return 'Approved';

      case 'rejected':
        return 'Rejected';

      default:
        return status;
    }
  }

  Future<void> _edit() async {
    final listing = _listing;

    if (listing == null) {
      return;
    }

    final result = await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => EditGemListingScreen(listing: listing)),
    );

    if (result != null) {
      await _load();
    }
  }

  Future<void> _submit() async {
    final listing = _listing;

    if (listing == null || !listing.canEdit) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Submit for Verification?'),
          content: const Text(
            'After submission the listing will be sent to the gemologist verification workflow.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Submit'),
            ),
          ],
        );
      },
    );

    if (confirmed != true) {
      return;
    }

    setState(() {
      _working = true;
    });

    try {
      final updated = await _service.submitForVerification(listing.id);

      if (!mounted) {
        return;
      }

      setState(() {
        _listing = updated;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Listing submitted for verification.'),
          backgroundColor: _success,
        ),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error.toString().replaceFirst('Exception: ', '')),
          backgroundColor: _danger,
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _working = false;
        });
      }
    }
  }

  Future<void> _delete() async {
    final listing = _listing;

    if (listing == null || !listing.isDraft) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Delete Listing?'),
          content: const Text(
            'This Draft listing will be permanently deleted.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: _danger),
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Delete'),
            ),
          ],
        );
      },
    );

    if (confirmed != true) {
      return;
    }

    setState(() {
      _working = true;
    });

    try {
      await _service.deleteListing(listing.id);

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(true);
    } catch (error) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error.toString().replaceFirst('Exception: ', '')),
          backgroundColor: _danger,
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _working = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        backgroundColor: _darkEmerald,
        foregroundColor: Colors.white,
        title: const Text('Gem Listing Details'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? _errorView()
          : _content(),
    );
  }

  Widget _errorView() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, color: _danger, size: 54),
            const SizedBox(height: 16),
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 18),
            FilledButton(onPressed: _load, child: const Text('Try Again')),
          ],
        ),
      ),
    );
  }

  Widget _content() {
    final listing = _listing!;

    final imageUrl = _imageUrl();

    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 20, 18, 80),
      children: [
        Container(
          height: 280,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: _border),
          ),
          clipBehavior: Clip.antiAlias,
          child: imageUrl == null
              ? const Center(
                  child: Icon(Icons.diamond_outlined, size: 70, color: _gold),
                )
              : Image.network(
                  imageUrl,
                  fit: BoxFit.contain,
                  errorBuilder: (context, error, stackTrace) {
                    return const Center(
                      child: Icon(
                        Icons.broken_image_outlined,
                        size: 60,
                        color: _gold,
                      ),
                    );
                  },
                ),
        ),

        const SizedBox(height: 18),

        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: _darkEmerald,
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                _statusLabel(listing.status),
                style: const TextStyle(
                  color: _gold,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                listing.title,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 25,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                listing.gemType,
                style: const TextStyle(color: Colors.white70),
              ),
            ],
          ),
        ),

        const SizedBox(height: 18),

        _detailsCard(
          title: 'Gem Characteristics',
          children: [
            _detail('Carat Weight', '${listing.caratWeight} ct'),
            _detail('Color', listing.color),
            _detail('Clarity', listing.clarity),
            _detail('Cut', listing.cut),
          ],
        ),

        const SizedBox(height: 18),

        _detailsCard(
          title: 'Description',
          children: [
            Text(
              listing.description,
              style: const TextStyle(color: _muted, height: 1.6),
            ),
          ],
        ),

        const SizedBox(height: 18),

        _detailsCard(
          title: 'Pricing',
          children: [
            _detail(
              'Price',
              '${listing.currency} ${listing.price.toStringAsFixed(2)}',
            ),
          ],
        ),

        const SizedBox(height: 18),

        _detailsCard(
          title: 'Certificate Information',
          children: [
            _detail(
              'Certificate Number',
              listing.certificateNumber ?? 'Not provided',
            ),
            _detail(
              'Certificate Authority',
              listing.certificateAuthority ?? 'Not provided',
            ),
            _detail(
              'Certificate File',
              listing.certificateUrl != null ? 'Uploaded' : 'Not uploaded',
            ),
          ],
        ),

        const SizedBox(height: 22),

        if (listing.canEdit)
          SizedBox(
            height: 50,
            child: FilledButton.icon(
              onPressed: _working ? null : _edit,
              style: FilledButton.styleFrom(backgroundColor: _emerald),
              icon: const Icon(Icons.edit_outlined),
              label: const Text('Edit Listing'),
            ),
          ),

        if (listing.canEdit) const SizedBox(height: 12),

        if (listing.canEdit)
          SizedBox(
            height: 50,
            child: FilledButton.icon(
              onPressed: _working ? null : _submit,
              style: FilledButton.styleFrom(
                backgroundColor: _gold,
                foregroundColor: _darkEmerald,
              ),
              icon: const Icon(Icons.verified_outlined),
              label: Text(
                _working ? 'Processing...' : 'Submit for Verification',
              ),
            ),
          ),

        if (listing.isDraft) const SizedBox(height: 12),

        if (listing.isDraft)
          SizedBox(
            height: 50,
            child: OutlinedButton.icon(
              onPressed: _working ? null : _delete,
              style: OutlinedButton.styleFrom(
                foregroundColor: _danger,
                side: const BorderSide(color: _danger),
              ),
              icon: const Icon(Icons.delete_outline),
              label: const Text('Delete Draft'),
            ),
          ),
      ],
    );
  }

  Widget _detailsCard({required String title, required List<Widget> children}) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              color: _darkEmerald,
              fontSize: 17,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 14),
          ...children,
        ],
      ),
    );
  }

  Widget _detail(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(color: _muted, fontSize: 12),
            ),
          ),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: const TextStyle(
                color: _darkEmerald,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

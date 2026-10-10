import 'package:flutter/material.dart';

import '../../config/api_config.dart';
import '../../models/gem_listing_model.dart';
import '../../services/gem_listing_service.dart';
import 'create_gem_listing_screen.dart';
import 'gem_listing_details_screen.dart';

class MyGemListingsScreen extends StatefulWidget {
  const MyGemListingsScreen({super.key});

  @override
  State<MyGemListingsScreen> createState() => _MyGemListingsScreenState();
}

class _MyGemListingsScreenState extends State<MyGemListingsScreen> {
  final GemListingService _service = GemListingService();

  List<GemListingModel> _listings = [];

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
      final listings = await _service.getMyListings();

      if (!mounted) {
        return;
      }

      setState(() {
        _listings = listings;
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

  Future<void> _create() async {
    final result = await Navigator.of(
      context,
    ).push(MaterialPageRoute(builder: (_) => const CreateGemListingScreen()));

    if (result != null) {
      await _load();
    }
  }

  Future<void> _openDetails(GemListingModel listing) async {
    final result = await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => GemListingDetailsScreen(listingId: listing.id),
      ),
    );

    if (result != null) {
      await _load();
    } else {
      await _load();
    }
  }

  String? _imageUrl(GemListingModel listing) {
    final value = listing.primaryImageUrl;

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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        backgroundColor: _darkEmerald,
        foregroundColor: Colors.white,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'My Gem Listings',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            Text(
              'Seller Workspace',
              style: TextStyle(fontSize: 11, color: Colors.white60),
            ),
          ],
        ),
        actions: [
          IconButton(
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _create,
        backgroundColor: _gold,
        foregroundColor: _darkEmerald,
        icon: const Icon(Icons.add),
        label: const Text('Create Listing'),
      ),
      body: RefreshIndicator(onRefresh: _load, child: _body()),
    );
  }

  Widget _body() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 100),
          const Icon(Icons.error_outline, size: 60),
          const SizedBox(height: 16),
          Text(_error!, textAlign: TextAlign.center),
          const SizedBox(height: 16),
          FilledButton(onPressed: _load, child: const Text('Try Again')),
        ],
      );
    }

    if (_listings.isEmpty) {
      return ListView(
        padding: const EdgeInsets.all(28),
        children: [
          const SizedBox(height: 100),
          const Icon(Icons.diamond_outlined, color: _gold, size: 70),
          const SizedBox(height: 18),
          const Text(
            'No gem listings yet',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: _darkEmerald,
              fontSize: 22,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'Create your first gemstone listing.',
            textAlign: TextAlign.center,
            style: TextStyle(color: _muted),
          ),
          const SizedBox(height: 22),
          FilledButton.icon(
            onPressed: _create,
            icon: const Icon(Icons.add),
            label: const Text('Create Listing'),
          ),
        ],
      );
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 18, 16, 100),
      children: [..._listings.map(_listingCard)],
    );
  }

  Widget _listingCard(GemListingModel listing) {
    final image = _imageUrl(listing);

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
        onTap: () => _openDetails(listing),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              Container(
                width: 90,
                height: 90,
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
                            const Icon(Icons.diamond_outlined, color: _gold),
                      ),
              ),

              const SizedBox(width: 14),

              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      listing.title,
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
                      listing.gemType,
                      style: const TextStyle(
                        color: _gold,
                        fontWeight: FontWeight.w700,
                      ),
                    ),

                    const SizedBox(height: 8),

                    Text(
                      '${listing.caratWeight} ct • ${listing.currency} ${listing.price.toStringAsFixed(2)}',
                      style: const TextStyle(color: _muted, fontSize: 11),
                    ),

                    const SizedBox(height: 8),

                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 9,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: _emerald.withValues(alpha: 0.09),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        _statusLabel(listing.status),
                        style: const TextStyle(
                          color: _emerald,
                          fontSize: 10,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              const Icon(Icons.chevron_right, color: _muted),
            ],
          ),
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';

import '../../models/gem_listing_model.dart';
import '../../services/admin_gem_listing_service.dart';

class AdminGemListingsScreen extends StatefulWidget {
  final AdminGemListingService? service;

  const AdminGemListingsScreen({super.key, this.service});

  @override
  State<AdminGemListingsScreen> createState() => _AdminGemListingsScreenState();
}

class _AdminGemListingsScreenState extends State<AdminGemListingsScreen> {
  late final AdminGemListingService _service;
  List<GemListingModel> _listings = [];
  String _query = '';
  String? _error;
  bool _loading = true;
  int? _deletingId;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? AdminGemListingService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final listings = await _service.getAll();
      if (mounted) setState(() => _listings = List.of(listings));
    } catch (error) {
      if (mounted) {
        setState(
          () => _error = error.toString().replaceFirst('Exception: ', ''),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _delete(GemListingModel listing) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete gem listing?'),
        content: Text(
          'Remove “${listing.title}” from gem listings? Its order history will remain available.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _deletingId = listing.id);
    try {
      await _service.delete(listing.id);
      if (mounted) {
        setState(() => _listings.removeWhere((item) => item.id == listing.id));
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(error.toString().replaceFirst('Exception: ', '')),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _deletingId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final needle = _query.trim().toLowerCase();
    final visible = _listings
        .where(
          (listing) => [
            listing.title,
            listing.gemType,
            listing.status,
            listing.id.toString(),
          ].any((value) => value.toLowerCase().contains(needle)),
        )
        .toList();

    return Scaffold(
      backgroundColor: const Color(0xFFFAF7F0),
      appBar: AppBar(
        title: const Text('Manage Gem Listings'),
        backgroundColor: const Color(0xFF08251E),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: _load,
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 20, 16, 48),
          children: [
            const Text(
              'Seller gemstones',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.w800,
                color: Color(0xFF08251E),
              ),
            ),
            const SizedBox(height: 5),
            const Text(
              'Sold gems remain visible here. Deleting a listing keeps its order history.',
              style: TextStyle(color: Color(0xFF697771)),
            ),
            const SizedBox(height: 18),
            TextField(
              decoration: InputDecoration(
                hintText: 'Search title, type, status or ID',
                prefixIcon: const Icon(Icons.search),
                filled: true,
                fillColor: Colors.white,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
              onChanged: (value) => setState(() => _query = value),
            ),
            const SizedBox(height: 18),
            if (_loading)
              const Center(
                child: Padding(
                  padding: EdgeInsets.all(32),
                  child: CircularProgressIndicator(),
                ),
              ),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.all(16),
                child: Text(_error!, style: const TextStyle(color: Colors.red)),
              ),
            if (!_loading && _error == null && visible.isEmpty)
              const Padding(
                padding: EdgeInsets.all(28),
                child: Center(child: Text('No gem listings found.')),
              ),
            if (!_loading && _error == null)
              for (final listing in visible)
                Card(
                  margin: const EdgeInsets.only(bottom: 11),
                  color: Colors.white,
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                    side: const BorderSide(color: Color(0xFFE7DFD2)),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: Row(
                      children: [
                        Container(
                          width: 48,
                          height: 48,
                          decoration: BoxDecoration(
                            color: const Color(0xFFF0F4EF),
                            borderRadius: BorderRadius.circular(11),
                          ),
                          child: const Icon(
                            Icons.diamond_outlined,
                            color: Color(0xFF16483B),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                listing.title,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w800,
                                  color: Color(0xFF08251E),
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                '#${listing.id} · ${listing.gemType} · ${listing.status}',
                                style: TextStyle(
                                  fontSize: 11,
                                  color: listing.status == 'Sold'
                                      ? const Color(0xFFA66536)
                                      : const Color(0xFF697771),
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Delete ${listing.title}',
                          onPressed: _deletingId == null
                              ? () => _delete(listing)
                              : null,
                          icon: _deletingId == listing.id
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(
                                  Icons.delete_outline,
                                  color: Color(0xFFAD4A40),
                                ),
                        ),
                      ],
                    ),
                  ),
                ),
          ],
        ),
      ),
    );
  }
}

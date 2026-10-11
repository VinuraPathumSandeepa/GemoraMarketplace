import 'package:flutter/material.dart';

import '../../models/marketplace/marketplace_gem.dart';
import '../../services/marketplace_service.dart';
import 'gem_detail_screen.dart';

class MarketplaceHomeScreen extends StatefulWidget {
  const MarketplaceHomeScreen({super.key});
  @override
  State<MarketplaceHomeScreen> createState() => _MarketplaceHomeScreenState();
}

class _MarketplaceHomeScreenState extends State<MarketplaceHomeScreen> {
  final _search = TextEditingController();
  final _service = MarketplaceService();
  bool _loading = true;
  String? _error;
  List<MarketplaceGem> _gems = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    if (!mounted) return;

    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final gems = await _service.getGems(search: _search.text.trim());
      if (mounted) setState(() => _gems = gems);
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _search,
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => _load(),
            decoration: InputDecoration(
              hintText: 'Search sapphire, ruby, color, cut...',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: IconButton(
                onPressed: _load,
                icon: const Icon(Icons.arrow_forward),
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(14),
              ),
            ),
          ),
          const SizedBox(height: 16),
          if (_loading)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(40),
                child: CircularProgressIndicator(),
              ),
            ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.all(20),
              child: Text(_error!, style: const TextStyle(color: Colors.red)),
            ),
          if (!_loading && _error == null && _gems.isEmpty)
            const Padding(
              padding: EdgeInsets.all(30),
              child: Center(child: Text('No eligible gemstones found.')),
            ),
          ..._gems.map(
            (g) => Card(
              margin: const EdgeInsets.only(bottom: 14),
              child: ListTile(
                contentPadding: const EdgeInsets.all(14),
                leading: ClipRRect(
                  borderRadius: BorderRadius.circular(10),
                  child: SizedBox(
                    width: 52,
                    height: 52,
                    child: g.resolvedImageUrl == null
                        ? const CircleAvatar(
                            backgroundColor: Color(0xFFF2EFE8),
                            child: Icon(
                              Icons.diamond_outlined,
                              color: Color(0xFFC99242),
                            ),
                          )
                        : Image.network(
                            g.resolvedImageUrl!,
                            fit: BoxFit.cover,
                            loadingBuilder: (context, child, progress) {
                              if (progress == null) return child;
                              return const Center(
                                child: SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                ),
                              );
                            },
                            errorBuilder: (context, error, stackTrace) =>
                                const CircleAvatar(
                                  backgroundColor: Color(0xFFF2EFE8),
                                  child: Icon(
                                    Icons.diamond_outlined,
                                    color: Color(0xFFC99242),
                                  ),
                                ),
                          ),
                  ),
                ),
                title: Text(
                  g.title,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                subtitle: Text(
                  '${g.gemType} • ${g.caratWeight} ct • ${g.currency} ${g.price.toStringAsFixed(0)}',
                ),
                trailing: const Icon(Icons.chevron_right),
                onTap: () async {
                  final changed = await Navigator.push<bool>(
                    context,
                    MaterialPageRoute(builder: (_) => GemDetailScreen(gem: g)),
                  );
                  if (changed == true) _load();
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}

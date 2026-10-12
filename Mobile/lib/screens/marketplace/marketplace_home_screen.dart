import 'package:flutter/material.dart';

import '../../constants/gemora_colors.dart';
import '../../models/marketplace/marketplace_gem.dart';
import '../../services/marketplace_service.dart';
import '../../widgets/buyer_glass.dart';
import 'marketplace_screen.dart';

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
  String _category = 'All';

  static const _categories = ['All', 'Sapphire', 'Ruby', 'Emerald', 'Garnet'];

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
      final gems = await _service.getGems(
        search: _search.text.trim(),
        gemType: _category,
      );
      if (mounted) setState(() => _gems = gems);
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

  Future<void> _open(MarketplaceGem gem) async {
    final changed = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => MarketplaceGemDetailsScreen(
          gemId: gem.id,
          isAuthenticated: true,
          role: 'Buyer',
          service: _service,
        ),
      ),
    );
    if (changed == true) await _load();
  }

  @override
  Widget build(BuildContext context) {
    return BuyerGradientBackground(
      child: RefreshIndicator(
        color: GemoraColors.green,
        onRefresh: _load,
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverToBoxAdapter(child: _header()),
            SliverToBoxAdapter(child: _categoriesList()),
            if (_loading)
              const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: CircularProgressIndicator(color: GemoraColors.green),
                ),
              )
            else if (_error != null)
              SliverFillRemaining(hasScrollBody: false, child: _errorState())
            else if (_gems.isEmpty)
              const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: Text(
                    'No verified gemstones match your search.',
                    style: TextStyle(color: GemoraColors.mutedText),
                  ),
                ),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 5, 16, 110),
                sliver: SliverGrid(
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                    childAspectRatio: .64,
                  ),
                  delegate: SliverChildBuilderDelegate(
                    (context, index) => TweenAnimationBuilder<double>(
                      tween: Tween(begin: 0, end: 1),
                      duration: Duration(milliseconds: 260 + index * 45),
                      curve: Curves.easeOutCubic,
                      builder: (context, value, child) => Opacity(
                        opacity: value,
                        child: Transform.translate(
                          offset: Offset(0, 16 * (1 - value)),
                          child: child,
                        ),
                      ),
                      child: _GemCard(
                        gem: _gems[index],
                        onTap: () => _open(_gems[index]),
                      ),
                    ),
                    childCount: _gems.length,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _header() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 20, 16, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const BuyerSectionHeading(
            eyebrow: 'Verified marketplace',
            title: 'Explore rare beauty',
            description: 'Professionally reviewed gemstones, ready for a confident purchase.',
          ),
          const SizedBox(height: 17),
          TextField(
            controller: _search,
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => _load(),
            decoration: InputDecoration(
              hintText: 'Search sapphire, ruby, colour or cut',
              prefixIcon: const Icon(Icons.search_rounded),
              suffixIcon: IconButton(
                tooltip: 'Search',
                onPressed: _load,
                icon: const Icon(Icons.arrow_forward_rounded),
              ),
              filled: true,
              fillColor: Colors.white.withValues(alpha: .78),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(18),
                borderSide: BorderSide.none,
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(18),
                borderSide: BorderSide(
                  color: GemoraColors.darkGreen.withValues(alpha: .09),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _categoriesList() {
    return SizedBox(
      height: 57,
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
        scrollDirection: Axis.horizontal,
        itemCount: _categories.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (context, index) {
          final category = _categories[index];
          final selected = category == _category;
          return ChoiceChip(
            selected: selected,
            showCheckmark: false,
            onSelected: (_) {
              setState(() => _category = category);
              _load();
            },
            label: Text(category),
            selectedColor: GemoraColors.darkGreen,
            backgroundColor: Colors.white.withValues(alpha: .7),
            side: BorderSide(
              color: selected
                  ? GemoraColors.darkGreen
                  : Colors.white.withValues(alpha: .85),
            ),
            labelStyle: TextStyle(
              color: selected ? Colors.white : GemoraColors.darkGreen,
              fontSize: 11,
              fontWeight: FontWeight.w700,
            ),
          );
        },
      ),
    );
  }

  Widget _errorState() => Center(
    child: Padding(
      padding: const EdgeInsets.all(30),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_rounded,
            size: 48,
            color: GemoraColors.darkGold,
          ),
          const SizedBox(height: 12),
          Text(_error!, textAlign: TextAlign.center),
          const SizedBox(height: 14),
          FilledButton(onPressed: _load, child: const Text('Try again')),
        ],
      ),
    ),
  );
}

class _GemCard extends StatelessWidget {
  final MarketplaceGem gem;
  final VoidCallback onTap;

  const _GemCard({required this.gem, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return BuyerGlassPanel(
      onTap: onTap,
      padding: EdgeInsets.zero,
      borderRadius: BorderRadius.circular(22),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Stack(
              fit: StackFit.expand,
              children: [
                if (gem.resolvedImageUrl != null)
                  Image.network(
                    gem.resolvedImageUrl!,
                    fit: BoxFit.cover,
                    errorBuilder: (_, _, _) => _placeholder(),
                  )
                else
                  _placeholder(),
                const DecoratedBox(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [Colors.transparent, Color(0x99031F1A)],
                    ),
                  ),
                ),
                Positioned(
                  top: 10,
                  left: 10,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 5,
                    ),
                    decoration: BoxDecoration(
                      color: GemoraColors.darkGreen.withValues(alpha: .85),
                      borderRadius: BorderRadius.circular(99),
                    ),
                    child: const Row(
                      children: [
                        Icon(
                          Icons.verified_rounded,
                          color: GemoraColors.gold,
                          size: 12,
                        ),
                        SizedBox(width: 4),
                        Text(
                          'VERIFIED',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 7,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                Positioned(
                  left: 11,
                  right: 11,
                  bottom: 10,
                  child: Text(
                    '${gem.caratWeight.toStringAsFixed(2)} ct · ${gem.cut}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: Colors.white70, fontSize: 9),
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  gem.gemType.toUpperCase(),
                  style: const TextStyle(
                    color: GemoraColors.darkGold,
                    fontSize: 8,
                    fontWeight: FontWeight.w900,
                    letterSpacing: 1,
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  gem.title,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: GemoraColors.darkGreen,
                    fontSize: 13,
                    height: 1.2,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 9),
                Text(
                  '${gem.currency} ${gem.price.toStringAsFixed(0)}',
                  style: const TextStyle(
                    color: GemoraColors.green,
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _placeholder() => const ColoredBox(
    color: Color(0xFFEAE5DA),
    child: Center(
      child: Icon(
        Icons.diamond_outlined,
        color: GemoraColors.darkGold,
        size: 44,
      ),
    ),
  );
}

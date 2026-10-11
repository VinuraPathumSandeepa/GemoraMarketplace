import 'package:flutter/material.dart';

import '../../config/api_config.dart';
import '../../models/marketplace/marketplace_gem.dart';
import '../../services/marketplace_service.dart';
import '../../widgets/glass_capsule_navigation.dart';
import 'my_orders_screen.dart';
import '../shipments/seller_shipment_list.dart';

class MarketplaceScreen extends StatefulWidget {
  final bool isAuthenticated;

  final String? displayName;
  final String? role;

  final VoidCallback? onLogin;
  final VoidCallback? onRegister;
  final VoidCallback? onWorkspace;
  final VoidCallback? onProfile;

  const MarketplaceScreen({
    super.key,
    this.isAuthenticated = false,
    this.displayName,
    this.role,
    this.onLogin,
    this.onRegister,
    this.onWorkspace,
    this.onProfile,
  });

  @override
  State<MarketplaceScreen> createState() => _MarketplaceScreenState();
}

class _MarketplaceScreenState extends State<MarketplaceScreen> {
  final MarketplaceService _service = MarketplaceService();

  final TextEditingController _searchController = TextEditingController();

  final ScrollController _scrollController = ScrollController();

  final GlobalKey _exploreSectionKey = GlobalKey();

  List<MarketplaceGem> _gems = [];

  bool _loading = true;

  String? _error;

  String _selectedCategory = 'All';

  int _bottomIndex = 0;

  static const Color _darkGreen = Color(0xFF08251E);

  static const Color _green = Color(0xFF16483B);

  static const Color _gold = Color(0xFFC99242);

  static const Color _lightGold = Color(0xFFE4BC74);

  static const Color _cream = Color(0xFFFAF7F0);

  static const Color _text = Color(0xFF10241F);

  static const Color _muted = Color(0xFF697771);

  static const Color _border = Color(0xFFE7DFD2);

  final List<String> _categories = const [
    'All',
    'Sapphire',
    'Ruby',
    'Emerald',
    'Garnet',
  ];

  @override
  void initState() {
    super.initState();

    _loadGems();
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();

    super.dispose();
  }

  Future<void> _loadGems() async {
    if (mounted) {
      setState(() {
        _loading = true;
        _error = null;
      });
    }

    try {
      final gems = await _service.getGems(
        search: _searchController.text,
        gemType: _selectedCategory,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _gems = gems;
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

  void _selectCategory(String category) {
    setState(() {
      _selectedCategory = category;
      _bottomIndex = 1;
    });

    _loadGems();
  }

  Future<void> _goHome() async {
    FocusScope.of(context).unfocus();

    final needsReload =
        _selectedCategory != 'All' || _searchController.text.trim().isNotEmpty;

    setState(() {
      _bottomIndex = 0;
      _selectedCategory = 'All';
      _searchController.clear();
    });

    if (needsReload) {
      await _loadGems();
    }

    if (!mounted) {
      return;
    }

    if (_scrollController.hasClients) {
      await _scrollController.animateTo(
        0,
        duration: const Duration(milliseconds: 350),
        curve: Curves.easeOutCubic,
      );
    }
  }

  void _goExplore() {
    FocusScope.of(context).unfocus();

    setState(() {
      _bottomIndex = 1;
    });

    WidgetsBinding.instance.addPostFrameCallback((_) {
      final exploreContext = _exploreSectionKey.currentContext;

      if (exploreContext == null) {
        return;
      }

      Scrollable.ensureVisible(
        exploreContext,
        duration: const Duration(milliseconds: 420),
        curve: Curves.easeOutCubic,
        alignment: 0.02,
      );
    });
  }

  String get _firstName {
    final value = widget.displayName?.trim();

    if (value == null || value.isEmpty) {
      return 'Gem Explorer';
    }

    return value.split(' ').first;
  }

  String get _roleLabel {
    final role = widget.role?.trim();

    if (role == null || role.isEmpty) {
      return 'Gemora Member';
    }

    return '$role Account';
  }

  IconData _categoryIcon(String category) {
    switch (category) {
      case 'Sapphire':
        return Icons.diamond_outlined;

      case 'Ruby':
        return Icons.auto_awesome;

      case 'Emerald':
        return Icons.hexagon_outlined;

      case 'Garnet':
        return Icons.blur_circular_outlined;

      default:
        return Icons.grid_view_rounded;
    }
  }

  Future<void> _openGem(MarketplaceGem gem) async {
    final result = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => MarketplaceGemDetailsScreen(
          gemId: gem.id,
          isAuthenticated: widget.isAuthenticated,
          role: widget.role,
          onLogin: widget.onLogin,
        ),
      ),
    );

    if (result == true && mounted) {
      _loadGems();
    }
  }

  void _handleBottomNavigation(int index) {
    if (_showShipments && index == 4) {
      Navigator.of(context)
          .push(MaterialPageRoute(builder: (_) => const SellerShipmentList()));
      return;
    }
    switch (index) {
      case 0:
        _goHome();
        return;

      case 1:
        _goExplore();
        return;

      case 2:
        if (widget.isAuthenticated) {
          widget.onWorkspace?.call();
        } else {
          _showGuestAccountSheet();
        }
        return;

      case 3:
        if (widget.isAuthenticated) {
          widget.onProfile?.call();
        } else {
          _showGuestAccountSheet();
        }
        return;
    }
  }

  void _showGuestAccountSheet() {
    showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) {
        return Container(
          padding: const EdgeInsets.fromLTRB(22, 22, 22, 28),
          decoration: const BoxDecoration(
            color: _cream,
            borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Icon(Icons.diamond_outlined, color: _gold, size: 40),
              const SizedBox(height: 14),
              const Text(
                'Join Gemora',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: _darkGreen,
                  fontSize: 22,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 7),
              const Text(
                'Browse freely. Sign in when you are ready to buy, sell or access your workspace.',
                textAlign: TextAlign.center,
                style: TextStyle(color: _muted, height: 1.5),
              ),
              const SizedBox(height: 20),
              FilledButton(
                onPressed: () {
                  Navigator.pop(context);

                  widget.onLogin?.call();
                },
                style: FilledButton.styleFrom(
                  backgroundColor: _darkGreen,
                  padding: const EdgeInsets.symmetric(vertical: 15),
                ),
                child: const Text('Sign In'),
              ),
              const SizedBox(height: 8),
              OutlinedButton(
                onPressed: () {
                  Navigator.pop(context);

                  widget.onRegister?.call();
                },
                style: OutlinedButton.styleFrom(
                  foregroundColor: _darkGreen,
                  side: const BorderSide(color: _gold),
                  padding: const EdgeInsets.symmetric(vertical: 15),
                ),
                child: const Text('Create Account'),
              ),
            ],
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBody: true,
      backgroundColor: _cream,
      body: SafeArea(
        child: RefreshIndicator(
          color: _gold,
          onRefresh: _loadGems,
          child: CustomScrollView(
            controller: _scrollController,
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverToBoxAdapter(child: _buildHeader()),
              SliverToBoxAdapter(child: _buildSearch()),
              SliverToBoxAdapter(
                child: KeyedSubtree(
                  key: _exploreSectionKey,
                  child: _buildCategorySection(),
                ),
              ),
              SliverToBoxAdapter(child: _buildMarketplaceHeading()),
              if (_loading)
                const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.all(50),
                    child: Center(
                      child: CircularProgressIndicator(color: _green),
                    ),
                  ),
                )
              else if (_error != null)
                SliverToBoxAdapter(child: _buildError())
              else if (_gems.isEmpty)
                SliverToBoxAdapter(child: _buildEmpty())
              else
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(18, 0, 18, 110),
                  sliver: SliverGrid(
                    delegate: SliverChildBuilderDelegate((context, index) {
                      return _buildGemCard(_gems[index]);
                    }, childCount: _gems.length),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 2,
                          crossAxisSpacing: 12,
                          mainAxisSpacing: 12,
                          childAspectRatio: 0.66,
                        ),
                  ),
                ),
            ],
          ),
        ),
      ),
      bottomNavigationBar: _buildBottomNavigation(),
    );
  }

  Widget _buildHeader() {
    return Container(
      margin: const EdgeInsets.fromLTRB(14, 12, 14, 0),
      padding: const EdgeInsets.fromLTRB(20, 18, 20, 24),
      decoration: BoxDecoration(
        color: _darkGreen,
        borderRadius: BorderRadius.circular(26),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.08),
            blurRadius: 24,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 46,
                height: 46,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: _lightGold.withValues(alpha: 0.24)),
                ),
                child: const Icon(
                  Icons.diamond_outlined,
                  color: _lightGold,
                  size: 28,
                ),
              ),
              const SizedBox(width: 12),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'GEMORA',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 19,
                        letterSpacing: 1.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      'Intelligent Gem Marketplace',
                      style: TextStyle(color: Colors.white60, fontSize: 9),
                    ),
                  ],
                ),
              ),
              if (!widget.isAuthenticated)
                TextButton(
                  onPressed: widget.onLogin,
                  child: const Text(
                    'Sign In',
                    style: TextStyle(
                      color: _lightGold,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                )
              else
                CircleAvatar(
                  radius: 20,
                  backgroundColor: _lightGold,
                  child: Text(
                    _firstName.substring(0, 1).toUpperCase(),
                    style: const TextStyle(
                      color: _darkGreen,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 24),
          Text(
            widget.isAuthenticated ? 'Welcome back,' : 'Discover Sri Lanka\'s',
            style: const TextStyle(color: Colors.white60, fontSize: 13),
          ),
          const SizedBox(height: 3),
          Text(
            widget.isAuthenticated ? _firstName : 'trusted gemstones.',
            style: const TextStyle(
              color: Colors.white,
              fontSize: 28,
              fontWeight: FontWeight.w800,
              height: 1.1,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            widget.isAuthenticated
                ? 'Explore professionally verified gemstones available on Gemora.'
                : 'Browse approved gemstones before creating an account.',
            style: const TextStyle(
              color: Colors.white70,
              fontSize: 12,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 14),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
            decoration: BoxDecoration(
              color: _lightGold.withValues(alpha: 0.14),
              borderRadius: BorderRadius.circular(99),
              border: Border.all(color: _lightGold.withValues(alpha: 0.28)),
            ),
            child: Text(
              widget.isAuthenticated ? _roleLabel : 'Guest Marketplace',
              style: const TextStyle(
                color: _lightGold,
                fontSize: 10,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSearch() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 20, 18, 0),
      child: TextField(
        controller: _searchController,
        textInputAction: TextInputAction.search,
        onSubmitted: (_) {
          setState(() {
            _bottomIndex = 1;
          });

          _loadGems();
        },
        decoration: InputDecoration(
          hintText: 'Search gemstones...',
          prefixIcon: const Icon(Icons.search),
          suffixIcon: IconButton(
            onPressed: () {
              setState(() {
                _bottomIndex = 1;
              });

              _loadGems();
            },
            icon: const Icon(Icons.tune_rounded),
          ),
          filled: true,
          fillColor: Colors.white,
          contentPadding: const EdgeInsets.symmetric(vertical: 16),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16),
            borderSide: const BorderSide(color: _border),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16),
            borderSide: const BorderSide(color: _border),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16),
            borderSide: const BorderSide(color: _green),
          ),
        ),
      ),
    );
  }

  Widget _buildCategorySection() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 26, 0, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.only(right: 18),
            child: Text(
              'Explore Gems',
              style: TextStyle(
                color: _text,
                fontSize: 20,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(height: 4),
          const Padding(
            padding: EdgeInsets.only(right: 18),
            child: Text(
              'Browse gemstones by category',
              style: TextStyle(color: _muted, fontSize: 11),
            ),
          ),
          const SizedBox(height: 14),
          SizedBox(
            height: 92,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.only(right: 18),
              itemCount: _categories.length,
              separatorBuilder: (_, _) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                final category = _categories[index];

                final selected = category == _selectedCategory;

                return InkWell(
                  onTap: () => _selectCategory(category),
                  borderRadius: BorderRadius.circular(16),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 180),
                    width: 82,
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: selected ? _darkGreen : Colors.white,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: selected ? _darkGreen : _border,
                      ),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          _categoryIcon(category),
                          color: selected ? _lightGold : _green,
                          size: 23,
                        ),
                        const SizedBox(height: 7),
                        Text(
                          category,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: selected ? Colors.white : _text,
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMarketplaceHeading() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 28, 18, 15),
      child: Row(
        children: [
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Verified Gems',
                  style: TextStyle(
                    color: _text,
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                SizedBox(height: 3),
                Text(
                  'Professionally approved marketplace listings',
                  style: TextStyle(color: _muted, fontSize: 10),
                ),
              ],
            ),
          ),
          if (!_loading && _error == null)
            Text(
              '${_gems.length}',
              style: const TextStyle(
                color: _gold,
                fontSize: 18,
                fontWeight: FontWeight.w900,
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildGemCard(MarketplaceGem gem) {
    final imageUrl = gem.resolvedImageUrl;

    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        onTap: () => _openGem(gem),
        borderRadius: BorderRadius.circular(18),
        child: Container(
          decoration: BoxDecoration(
            border: Border.all(color: _border),
            borderRadius: BorderRadius.circular(18),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Container(
                  width: double.infinity,
                  color: const Color(0xFFF2EFE8),
                  child: imageUrl == null
                      ? const Center(
                          child: Icon(
                            Icons.diamond_outlined,
                            color: _gold,
                            size: 48,
                          ),
                        )
                      : Image.network(
                          imageUrl,
                          fit: BoxFit.cover,
                          loadingBuilder: (context, child, progress) {
                            if (progress == null) return child;
                            return const Center(
                              child: SizedBox(
                                width: 24,
                                height: 24,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: _gold,
                                ),
                              ),
                            );
                          },
                          errorBuilder: (context, error, stackTrace) {
                            return const Center(
                              child: Icon(
                                Icons.diamond_outlined,
                                color: _gold,
                                size: 48,
                              ),
                            );
                          },
                        ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            gem.title,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _text,
                              fontSize: 13,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                        const Icon(
                          Icons.verified_rounded,
                          color: Color(0xFF247A50),
                          size: 16,
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      gem.gemType,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(color: _muted, fontSize: 9),
                    ),
                    const SizedBox(height: 7),
                    Text(
                      '${gem.caratWeight.toStringAsFixed(2)} ct',
                      style: const TextStyle(
                        color: _green,
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      '${gem.currency} ${gem.price.toStringAsFixed(2)}',
                      style: const TextStyle(
                        color: _gold,
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildError() {
    return Padding(
      padding: const EdgeInsets.all(30),
      child: Column(
        children: [
          const Icon(Icons.wifi_off_rounded, size: 45, color: _gold),
          const SizedBox(height: 12),
          Text(
            _error!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: _muted),
          ),
          const SizedBox(height: 14),
          FilledButton(
            onPressed: _loadGems,
            style: FilledButton.styleFrom(backgroundColor: _green),
            child: const Text('Try Again'),
          ),
        ],
      ),
    );
  }

  Widget _buildEmpty() {
    return const Padding(
      padding: EdgeInsets.all(45),
      child: Column(
        children: [
          Icon(Icons.diamond_outlined, size: 50, color: _gold),
          SizedBox(height: 12),
          Text(
            'No gemstones found.',
            style: TextStyle(color: _text, fontWeight: FontWeight.w700),
          ),
        ],
      ),
    );
  }

  bool get _showShipments =>
      widget.isAuthenticated && widget.role?.trim().toLowerCase() == 'seller';

  Widget _buildBottomNavigation() {
    return GlassCapsuleNavigation(
      selectedIndex: _bottomIndex,
      onDestinationSelected: _handleBottomNavigation,
      destinations: [
        const GlassCapsuleDestination(
          icon: Icons.home_outlined,
          selectedIcon: Icons.home_rounded,
          label: 'Home',
        ),
        const GlassCapsuleDestination(
          icon: Icons.diamond_outlined,
          selectedIcon: Icons.diamond,
          label: 'Explore',
        ),
        GlassCapsuleDestination(
          icon: widget.isAuthenticated
              ? Icons.work_outline_rounded
              : Icons.login_rounded,
          selectedIcon: widget.isAuthenticated
              ? Icons.work_rounded
              : Icons.login_rounded,
          label: widget.isAuthenticated ? 'Workspace' : 'Account',
        ),
        const GlassCapsuleDestination(
          icon: Icons.person_outline_rounded,
          selectedIcon: Icons.person,
          label: 'Profile',
        ),
        if (_showShipments)
          const GlassCapsuleDestination(
            icon: Icons.local_shipping_outlined,
            selectedIcon: Icons.local_shipping,
            label: 'My Shipments',
          ),
      ],
    );
  }
}

class MarketplaceGemDetailsScreen extends StatefulWidget {
  final int gemId;

  final bool isAuthenticated;

  final String? role;

  final VoidCallback? onLogin;

  final MarketplaceService? service;

  const MarketplaceGemDetailsScreen({
    super.key,
    required this.gemId,
    required this.isAuthenticated,
    this.role,
    this.onLogin,
    this.service,
  });

  @override
  State<MarketplaceGemDetailsScreen> createState() =>
      _MarketplaceGemDetailsScreenState();
}

class _MarketplaceGemDetailsScreenState
    extends State<MarketplaceGemDetailsScreen> {
  late final MarketplaceService _service;

  MarketplaceGem? _gem;

  bool _loading = true;

  bool _ordering = false;

  String? _error;

  static const Color _darkGreen = Color(0xFF08251E);

  static const Color _green = Color(0xFF16483B);

  static const Color _gold = Color(0xFFC99242);

  static const Color _cream = Color(0xFFFAF7F0);

  static const Color _text = Color(0xFF10241F);

  static const Color _muted = Color(0xFF697771);

  static const Color _border = Color(0xFFE7DFD2);

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? MarketplaceService();
    _load();
  }

  Future<void> _load() async {
    try {
      final gem = await _service.getGem(widget.gemId);

      if (!mounted) {
        return;
      }

      setState(() {
        _gem = gem;
        _loading = false;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _error = error.toString().replaceFirst('Exception: ', '');

        _loading = false;
      });
    }
  }

  String? _imageUrl(String? value) => ApiConfig.resolveImageUrl(value);

  void _buy() {
    if (!widget.isAuthenticated) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please sign in as a Buyer to purchase gemstones.'),
        ),
      );

      widget.onLogin?.call();

      return;
    }

    final role = widget.role?.trim().toLowerCase();

    if (role != 'buyer') {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            role == 'seller'
                ? 'Seller accounts can browse the marketplace, but purchasing requires a Buyer account.'
                : 'Purchasing gemstones is available to Buyer accounts.',
          ),
        ),
      );

      return;
    }

    final gem = _gem;
    if (gem == null) return;

    _showPurchaseConfirmation(gem);
  }

  Future<void> _showPurchaseConfirmation(MarketplaceGem gem) async {
    String selectedCountry = 'LK';
    final addressController = TextEditingController(
      text: '120 Galle Road, Colombo 03',
    );
    final regionController = TextEditingController(text: 'Western Province');
    final recipientController = TextEditingController();
    final phoneController = TextEditingController();
    final cityController = TextEditingController(text: 'Colombo');
    final districtController = TextEditingController(text: 'Colombo');
    final postalCodeController = TextEditingController(text: '00300');
    final formKey = GlobalKey<FormState>();

    const countries = [
      {
        'code': 'LK',
        'name': 'Sri Lanka (Domestic - LK)',
        'region': 'Western Province',
        'address': '120 Galle Road, Colombo 03',
      },
      {
        'code': 'US',
        'name': 'United States (International - US)',
        'region': 'California',
        'address': '742 Evergreen Terrace, Springfield',
      },
      {
        'code': 'GB',
        'name': 'United Kingdom (International - GB)',
        'region': 'Greater London',
        'address': '10 Downing Street, London',
      },
      {
        'code': 'AU',
        'name': 'Australia (International - AU)',
        'region': 'New South Wales',
        'address': '42 Wallaby Way, Sydney',
      },
      {
        'code': 'SG',
        'name': 'Singapore (International - SG)',
        'region': 'Central Region',
        'address': '10 Marina Boulevard, Singapore',
      },
      {
        'code': 'JP',
        'name': 'Japan (International - JP)',
        'region': 'Tokyo',
        'address': '1-1 Chiyoda, Chiyoda-ku, Tokyo',
      },
      {
        'code': 'DE',
        'name': 'Germany (International - DE)',
        'region': 'Bavaria',
        'address': 'Maximilianstrasse 15, Munich',
      },
      {
        'code': 'AE',
        'name': 'United Arab Emirates (International - AE)',
        'region': 'Dubai',
        'address': 'Sheikh Zayed Road, Dubai',
      },
    ];

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (sheetContext) {
        return StatefulBuilder(
          builder: (context, setModalState) {
            final isDomestic = selectedCountry == 'LK';

            return Padding(
              padding: EdgeInsets.only(
                bottom: MediaQuery.of(context).viewInsets.bottom,
              ),
              child: Container(
                decoration: const BoxDecoration(
                  color: _cream,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
                ),
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
                child: Form(
                  key: formKey,
                  child: SingleChildScrollView(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Center(
                          child: Container(
                            width: 40,
                            height: 4,
                            decoration: BoxDecoration(
                              color: _border,
                              borderRadius: BorderRadius.circular(2),
                            ),
                          ),
                        ),
                        const SizedBox(height: 16),
                        const Text(
                          'Confirm Purchase',
                          style: TextStyle(
                            color: _darkGreen,
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 14),
                        Container(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: _border),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                gem.title,
                                style: const TextStyle(
                                  color: _text,
                                  fontSize: 15,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 6),
                              Row(
                                mainAxisAlignment:
                                    MainAxisAlignment.spaceBetween,
                                children: [
                                  Text(
                                    '${gem.currency} ${gem.price.toStringAsFixed(2)}',
                                    style: const TextStyle(
                                      color: _darkGreen,
                                      fontSize: 16,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                  Text(
                                    'Seller: ${gem.sellerName}',
                                    style: const TextStyle(
                                      color: _muted,
                                      fontSize: 12,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),
                        const Text(
                          'Shipping Country',
                          style: TextStyle(
                            color: _text,
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 6),
                        DropdownButtonFormField<String>(
                          initialValue: selectedCountry,
                          decoration: InputDecoration(
                            filled: true,
                            fillColor: Colors.white,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 12,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: const BorderSide(color: _border),
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: const BorderSide(color: _border),
                            ),
                          ),
                          items: countries.map((c) {
                            return DropdownMenuItem<String>(
                              value: c['code'],
                              child: Text(
                                c['name']!,
                                style: const TextStyle(fontSize: 13),
                              ),
                            );
                          }).toList(),
                          onChanged: (val) {
                            if (val == null) return;
                            setModalState(() {
                              selectedCountry = val;
                              final match = countries.firstWhere(
                                (c) => c['code'] == val,
                                orElse: () => countries.first,
                              );
                              regionController.text = match['region']!;
                              addressController.text = match['address']!;
                              cityController.clear();
                              districtController.clear();
                              postalCodeController.clear();
                            });
                          },
                        ),
                        const SizedBox(height: 14),
                        const Text(
                          'Shipping Address',
                          style: TextStyle(
                            color: _text,
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 6),
                        TextFormField(
                          controller: addressController,
                          decoration: InputDecoration(
                            hintText: 'Enter street address',
                            filled: true,
                            fillColor: Colors.white,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 12,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: const BorderSide(color: _border),
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: const BorderSide(color: _border),
                            ),
                          ),
                          validator: (val) =>
                              (val == null || val.trim().isEmpty)
                              ? 'Shipping address is required'
                              : null,
                        ),
                        const SizedBox(height: 14),
                        const Text(
                          'State / Province / Region',
                          style: TextStyle(
                            color: _text,
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 6),
                        TextFormField(
                          controller: regionController,
                          decoration: InputDecoration(
                            hintText: 'Enter province or region',
                            filled: true,
                            fillColor: Colors.white,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 12,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: const BorderSide(color: _border),
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: const BorderSide(color: _border),
                            ),
                          ),
                          validator: (val) =>
                              (val == null || val.trim().isEmpty)
                              ? 'Region is required'
                              : null,
                        ),
                        const SizedBox(height: 14),
                        _purchaseField(
                          controller: recipientController,
                          label: 'Recipient Name',
                          hint: 'Name of the person receiving the package',
                        ),
                        const SizedBox(height: 14),
                        _purchaseField(
                          controller: phoneController,
                          label: 'Recipient Phone',
                          hint: '+94 77 123 4567',
                          keyboardType: TextInputType.phone,
                        ),
                        const SizedBox(height: 14),
                        _purchaseField(
                          controller: cityController,
                          label: 'City / Town',
                          hint: 'Colombo',
                        ),
                        const SizedBox(height: 14),
                        _purchaseField(
                          controller: districtController,
                          label: 'District',
                          hint: 'Colombo',
                        ),
                        const SizedBox(height: 14),
                        _purchaseField(
                          controller: postalCodeController,
                          label: 'Postal Code',
                          hint: '00300',
                          keyboardType: TextInputType.number,
                        ),
                        const SizedBox(height: 14),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 8,
                          ),
                          decoration: BoxDecoration(
                            color: isDomestic
                                ? const Color(0xFFE8F5E9)
                                : const Color(0xFFFFF8E1),
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(
                              color: isDomestic
                                  ? const Color(0xFFA5D6A7)
                                  : const Color(0xFFFFE082),
                            ),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                isDomestic
                                    ? Icons.check_circle_outline
                                    : Icons.flight_takeoff,
                                size: 16,
                                color: isDomestic
                                    ? const Color(0xFF2E7D32)
                                    : const Color(0xFFC99242),
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  isDomestic
                                      ? 'Domestic delivery: Export clearance not required'
                                      : 'International delivery: Export clearance required (Component 4)',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    color: isDomestic
                                        ? const Color(0xFF2E7D32)
                                        : const Color(0xFF8D6E63),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: _ordering
                                    ? null
                                    : () => Navigator.pop(sheetContext),
                                style: OutlinedButton.styleFrom(
                                  side: const BorderSide(color: _border),
                                  padding: const EdgeInsets.symmetric(
                                    vertical: 14,
                                  ),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                ),
                                child: const Text(
                                  'Cancel',
                                  style: TextStyle(color: _muted),
                                ),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: FilledButton(
                                onPressed: _ordering
                                    ? null
                                    : () async {
                                        if (!formKey.currentState!.validate()) {
                                          return;
                                        }
                                        Navigator.pop(sheetContext);
                                        await _submitOrder(
                                          gemId: gem.id,
                                          shippingAddress: addressController
                                              .text
                                              .trim(),
                                          shippingRegion: regionController.text
                                              .trim(),
                                          shippingCountryCode: selectedCountry,
                                          deliveryDetails: {
                                            'recipientName': recipientController
                                                .text
                                                .trim(),
                                            'recipientPhone': phoneController
                                                .text
                                                .trim(),
                                            'addressLine1': addressController
                                                .text
                                                .trim(),
                                            'city': cityController.text.trim(),
                                            'district': districtController.text
                                                .trim(),
                                            'region': regionController.text
                                                .trim(),
                                            'postalCode': postalCodeController
                                                .text
                                                .trim(),
                                            'countryCode': selectedCountry,
                                          },
                                        );
                                      },
                                style: FilledButton.styleFrom(
                                  backgroundColor: _gold,
                                  foregroundColor: _darkGreen,
                                  padding: const EdgeInsets.symmetric(
                                    vertical: 14,
                                  ),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                ),
                                child: _ordering
                                    ? const SizedBox(
                                        width: 18,
                                        height: 18,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: _darkGreen,
                                        ),
                                      )
                                    : const Text(
                                        'Place Order',
                                        style: TextStyle(
                                          fontWeight: FontWeight.w800,
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
              ),
            );
          },
        );
      },
    );

    // The route's closing animation still owns the form for a short time.
    await Future<void>.delayed(const Duration(milliseconds: 300));
    addressController.dispose();
    regionController.dispose();
    recipientController.dispose();
    phoneController.dispose();
    cityController.dispose();
    districtController.dispose();
    postalCodeController.dispose();
  }

  Widget _purchaseField({
    required TextEditingController controller,
    required String label,
    required String hint,
    TextInputType? keyboardType,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            color: _text,
            fontSize: 13,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 6),
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          decoration: InputDecoration(
            hintText: hint,
            filled: true,
            fillColor: Colors.white,
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 14,
              vertical: 12,
            ),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: _border),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: _border),
            ),
          ),
          validator: (value) => value == null || value.trim().isEmpty
              ? '$label is required'
              : null,
        ),
      ],
    );
  }

  Future<void> _submitOrder({
    required int gemId,
    required String shippingAddress,
    required String shippingRegion,
    required String shippingCountryCode,
    required Map<String, dynamic> deliveryDetails,
  }) async {
    setState(() => _ordering = true);

    try {
      final order = await _service.createOrder(
        gemId,
        shippingAddress: shippingAddress,
        shippingRegion: shippingRegion,
        shippingCountryCode: shippingCountryCode,
        deliveryDetails: deliveryDetails,
      );

      if (!mounted) return;

      setState(() => _ordering = false);

      final isInternational = order.shippingCountryCode != 'LK';

      await showDialog<void>(
        context: context,
        barrierDismissible: false,
        builder: (dialogContext) {
          return AlertDialog(
            backgroundColor: _cream,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(20),
            ),
            title: const Row(
              children: [
                Icon(Icons.check_circle, color: Color(0xFF2E7D32)),
                SizedBox(width: 8),
                Text(
                  'Order Created',
                  style: TextStyle(
                    color: _darkGreen,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Order ${order.orderNumber} created successfully.',
                  style: const TextStyle(
                    fontWeight: FontWeight.w700,
                    color: _text,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  'Total: ${order.currency} ${order.agreedPrice.toStringAsFixed(2)}',
                  style: const TextStyle(color: _muted),
                ),
                Text(
                  'Destination: ${order.shippingCountryCode ?? shippingCountryCode}',
                  style: const TextStyle(color: _muted),
                ),
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: isInternational
                        ? const Color(0xFFFFF8E1)
                        : const Color(0xFFE8F5E9),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    isInternational
                        ? 'Export clearance is required for this international order. The seller can now start export compliance (Component 4).'
                        : 'This is a domestic order within Sri Lanka. Export clearance is not required.',
                    style: TextStyle(
                      fontSize: 12,
                      color: isInternational
                          ? const Color(0xFF8D6E63)
                          : const Color(0xFF2E7D32),
                    ),
                  ),
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.pop(dialogContext);
                  Navigator.pop(context, true);
                },
                child: const Text('Close'),
              ),
              FilledButton(
                onPressed: () {
                  Navigator.pop(dialogContext);
                  Navigator.push(
                    context,
                    MaterialPageRoute(builder: (_) => const MyOrdersScreen()),
                  );
                },
                style: FilledButton.styleFrom(
                  backgroundColor: _darkGreen,
                  foregroundColor: Colors.white,
                ),
                child: const Text('View My Orders'),
              ),
            ],
          );
        },
      );
    } catch (e) {
      if (!mounted) return;
      final errorMessage = e.toString().replaceFirst('Exception: ', '');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(errorMessage),
          backgroundColor: Colors.red.shade800,
        ),
      );
    } finally {
      if (mounted) setState(() => _ordering = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _cream,
      appBar: AppBar(
        backgroundColor: _darkGreen,
        foregroundColor: Colors.white,
        title: const Text('Gemstone Details'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: _green))
          : _error != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(_error!, textAlign: TextAlign.center),
              ),
            )
          : _buildContent(),
    );
  }

  Widget _buildContent() {
    final gem = _gem!;

    final image = _imageUrl(gem.primaryImageUrl);

    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 18, 18, 50),
      children: [
        Container(
          height: 300,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(22),
            border: Border.all(color: _border),
          ),
          child: image == null
              ? const Center(
                  child: Icon(Icons.diamond_outlined, color: _gold, size: 80),
                )
              : Image.network(
                  image,
                  fit: BoxFit.cover,
                  loadingBuilder: (context, child, progress) {
                    if (progress == null) return child;
                    return const Center(
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: _gold,
                      ),
                    );
                  },
                  errorBuilder: (context, error, stackTrace) {
                    return const Center(
                      child: Icon(
                        Icons.diamond_outlined,
                        color: _gold,
                        size: 80,
                      ),
                    );
                  },
                ),
        ),
        const SizedBox(height: 18),
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: _darkGreen,
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.verified_rounded,
                    color: Color(0xFFE4BC74),
                    size: 18,
                  ),
                  SizedBox(width: 6),
                  Text(
                    'VERIFIED GEMSTONE',
                    style: TextStyle(
                      color: Color(0xFFE4BC74),
                      fontSize: 10,
                      letterSpacing: 1,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                gem.title,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 25,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                '${gem.gemType} • ${gem.caratWeight.toStringAsFixed(2)} ct',
                style: const TextStyle(color: Colors.white70),
              ),
              const SizedBox(height: 16),
              Text(
                '${gem.currency} ${gem.price.toStringAsFixed(2)}',
                style: const TextStyle(
                  color: _gold,
                  fontSize: 21,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 18),
        _detailsCard(
          title: 'Gemstone Information',
          children: [
            _detail('Type', gem.gemType),
            _detail('Color', gem.color),
            _detail('Clarity', gem.clarity),
            _detail('Cut', gem.cut),
            _detail('Weight', '${gem.caratWeight.toStringAsFixed(2)} ct'),
          ],
        ),
        const SizedBox(height: 18),
        _detailsCard(
          title: 'Seller',
          children: [_detail('Seller', gem.sellerName)],
        ),
        const SizedBox(height: 18),
        _detailsCard(
          title: 'Certification',
          children: [
            _detail('Certificate No.', gem.certificateNumber ?? 'Not listed'),
            _detail('Authority', gem.certificateAuthority ?? 'Not listed'),
          ],
        ),
        const SizedBox(height: 18),
        _detailsCard(
          title: 'Description',
          children: [
            Text(
              gem.description,
              style: const TextStyle(color: _muted, height: 1.6),
            ),
          ],
        ),
        const SizedBox(height: 22),
        SizedBox(
          height: 54,
          child: FilledButton.icon(
            onPressed: _ordering ? null : _buy,
            style: FilledButton.styleFrom(
              backgroundColor: _gold,
              foregroundColor: _darkGreen,
            ),
            icon: _ordering
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: _darkGreen,
                    ),
                  )
                : const Icon(Icons.shopping_bag_outlined),
            label: Text(
              _ordering
                  ? 'Processing...'
                  : (widget.isAuthenticated
                        ? 'Buy Gemstone'
                        : 'Sign In to Buy'),
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
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
              color: _text,
              fontSize: 16,
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
      padding: const EdgeInsets.only(bottom: 11),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(color: _muted, fontSize: 11),
            ),
          ),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: const TextStyle(
                color: _text,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

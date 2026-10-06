import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';

import 'admin_dashboard.dart';
import 'buyer_dashboard.dart';
import 'export_officer_dashboard.dart';
import 'gemologist_dashboard.dart';
import 'seller_dashboard.dart';

class RoleDashboard extends StatelessWidget {
  const RoleDashboard({super.key});

  static const Color _primary = Color(0xFF7356A3);

  static const Color _primaryDark = Color(0xFF513679);

  static const Color _background = Color(0xFFF7F7FA);

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();

    final user = authProvider.user;

    if (user == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    final role = _normalizeRole(user.role);

    return Scaffold(
      backgroundColor: _background,
      body: SafeArea(
        child: CustomScrollView(
          slivers: [
            // ==================================================
            // HEADER
            // ==================================================

            SliverToBoxAdapter(
              child: Container(
                padding: const EdgeInsets.fromLTRB(18, 18, 18, 30),
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [_primaryDark, _primary],
                  ),
                  borderRadius: BorderRadius.only(
                    bottomLeft: Radius.circular(32),
                    bottomRight: Radius.circular(32),
                  ),
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
                            color: Colors.white.withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: const Icon(
                            Icons.diamond_outlined,
                            color: Colors.white,
                            size: 27,
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
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                  letterSpacing: 1.6,
                                ),
                              ),
                              Text(
                                'Gemstone Marketplace',
                                style: TextStyle(
                                  color: Colors.white70,
                                  fontSize: 10,
                                ),
                              ),
                            ],
                          ),
                        ),
                        PopupMenuButton<String>(
                          tooltip: 'Account',
                          color: Colors.white,
                          onSelected: (value) async {
                            if (value == 'logout') {
                              await context.read<AuthProvider>().logout();

                              if (!context.mounted) {
                                return;
                              }

                              Navigator.of(context)
                                  .popUntil((route) => route.isFirst);
                            }
                          },
                          itemBuilder: (_) => const [
                            PopupMenuItem(
                              value: 'logout',
                              child: Row(
                                children: [
                                  Icon(Icons.logout_rounded),
                                  SizedBox(width: 10),
                                  Text('Logout'),
                                ],
                              ),
                            ),
                          ],
                          child: CircleAvatar(
                            radius: 21,
                            backgroundColor: Colors.white,
                            child: Text(
                              _initial(user.fullName),
                              style: const TextStyle(
                                color: _primary,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),

                    const SizedBox(height: 28),

                    const Text(
                      'Welcome back,',
                      style: TextStyle(color: Colors.white70, fontSize: 15),
                    ),

                    const SizedBox(height: 4),

                    Text(
                      user.fullName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 29,
                        fontWeight: FontWeight.w800,
                      ),
                    ),

                    const SizedBox(height: 7),

                    const Text(
                      'Discover trusted gems from Sri Lanka.',
                      style: TextStyle(color: Colors.white70, fontSize: 14),
                    ),

                    const SizedBox(height: 16),

                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 7,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(30),
                      ),
                      child: Text(
                        _roleDisplayName(role),
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // ==================================================
            // BODY
            // ==================================================
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(18, 23, 18, 35),
              sliver: SliverList(
                delegate: SliverChildListDelegate([
                  // SEARCH

                  Container(
                    height: 56,
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(18),
                    ),
                    child: const TextField(
                      readOnly: true,
                      decoration: InputDecoration(
                        hintText: 'Search gemstones...',
                        prefixIcon: Icon(Icons.search_rounded),
                        suffixIcon: Icon(Icons.tune_rounded),
                        border: InputBorder.none,
                        contentPadding: EdgeInsets.symmetric(vertical: 17),
                      ),
                    ),
                  ),

                  const SizedBox(height: 28),

                  const Text(
                    'Explore Gems',
                    style: TextStyle(
                      color: Color(0xFF24212A),
                      fontSize: 21,
                      fontWeight: FontWeight.w800,
                    ),
                  ),

                  const SizedBox(height: 5),

                  const Text(
                    'Browse gemstones by category',
                    style: TextStyle(color: Color(0xFF77727F), fontSize: 13),
                  ),

                  const SizedBox(height: 16),

                  const Row(
                    children: [
                      Expanded(
                        child: _CategoryCard(
                          icon: Icons.diamond_outlined,
                          title: 'Sapphire',
                        ),
                      ),
                      SizedBox(width: 9),
                      Expanded(
                        child: _CategoryCard(
                          icon: Icons.auto_awesome,
                          title: 'Ruby',
                        ),
                      ),
                      SizedBox(width: 9),
                      Expanded(
                        child: _CategoryCard(
                          icon: Icons.hexagon_outlined,
                          title: 'Emerald',
                        ),
                      ),
                      SizedBox(width: 9),
                      Expanded(
                        child: _CategoryCard(
                          icon: Icons.grid_view_rounded,
                          title: 'All',
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 30),

                  const Text(
                    'Featured Gems',
                    style: TextStyle(
                      color: Color(0xFF24212A),
                      fontSize: 21,
                      fontWeight: FontWeight.w800,
                    ),
                  ),

                  const SizedBox(height: 5),

                  const Text(
                    'Discover selected gemstones',
                    style: TextStyle(color: Color(0xFF77727F), fontSize: 13),
                  ),

                  const SizedBox(height: 16),

                  const SizedBox(
                    height: 205,
                    child: Row(
                      children: [
                        Expanded(
                          child: _GemCard(
                            name: 'Blue Sapphire',
                            origin: 'Ratnapura',
                            price: 'LKR 385,000',
                          ),
                        ),
                        SizedBox(width: 12),
                        Expanded(
                          child: _GemCard(
                            name: 'Ceylon Sapphire',
                            origin: 'Elahera',
                            price: 'LKR 295,000',
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 30),

                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: const Color(0xFF24212B),
                      borderRadius: BorderRadius.circular(22),
                    ),
                    child: const Row(
                      children: [
                        Icon(
                          Icons.verified_user_outlined,
                          color: Colors.white,
                          size: 33,
                        ),
                        SizedBox(width: 15),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Verified with confidence',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 17,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                              SizedBox(height: 6),
                              Text(
                                'Gemora combines AI-assisted analysis with professional human review.',
                                style: TextStyle(
                                  color: Colors.white70,
                                  fontSize: 12,
                                  height: 1.4,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 30),

                  // ==================================================
                  // ROLE WORKSPACE
                  // ==================================================
                  const Text(
                    'My Workspace',
                    style: TextStyle(
                      color: Color(0xFF24212A),
                      fontSize: 21,
                      fontWeight: FontWeight.w800,
                    ),
                  ),

                  const SizedBox(height: 6),

                  const Text(
                    'Continue to your role-specific Gemora tools.',
                    style: TextStyle(color: Color(0xFF77727F), fontSize: 13),
                  ),

                  const SizedBox(height: 16),

                  SizedBox(
                    width: double.infinity,
                    height: 58,
                    child: FilledButton.icon(
                      onPressed: () {
                        _openWorkspace(context, role);
                      },
                      style: FilledButton.styleFrom(
                        backgroundColor: _primary,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                      icon: Icon(_workspaceIcon(role)),
                      label: Text(
                        _workspaceLabel(role),
                        style: const TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                  ),
                ]),
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _normalizeRole(String role) {
    return role
        .trim()
        .toLowerCase()
        .replaceAll(' ', '')
        .replaceAll('_', '')
        .replaceAll('-', '');
  }

  void _openWorkspace(BuildContext context, String role) {
    final Widget screen;

    switch (role) {
      case 'buyer':
        screen = const BuyerDashboard();
        break;

      case 'seller':
        screen = const SellerDashboard();
        break;

      case 'gemologist':
        screen = const GemologistDashboard();
        break;

      case 'exportofficer':
        screen = const ExportOfficerDashboard();
        break;

      case 'admin':
        screen = const AdminDashboard();
        break;

      default:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Workspace is not available for this account.'),
          ),
        );
        return;
    }

    Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
  }

  String _workspaceLabel(String role) {
    switch (role) {
      case 'buyer':
        return 'Open Buyer Workspace';

      case 'seller':
        return 'Open Seller Workspace';

      case 'gemologist':
        return 'Open Gemologist Workspace';

      case 'exportofficer':
        return 'Open Export Workspace';

      case 'admin':
        return 'Open Admin Workspace';

      default:
        return 'Open My Workspace';
    }
  }

  String _roleDisplayName(String role) {
    switch (role) {
      case 'buyer':
        return 'Buyer Account';

      case 'seller':
        return 'Seller Account';

      case 'gemologist':
        return 'Gemologist Account';

      case 'exportofficer':
        return 'Export Officer Account';

      case 'admin':
        return 'Administrator Account';

      default:
        return 'Gemora Account';
    }
  }

  IconData _workspaceIcon(String role) {
    switch (role) {
      case 'buyer':
        return Icons.shopping_bag_outlined;

      case 'seller':
        return Icons.storefront_outlined;

      case 'gemologist':
        return Icons.verified_outlined;

      case 'exportofficer':
        return Icons.flight_takeoff_rounded;

      case 'admin':
        return Icons.admin_panel_settings_outlined;

      default:
        return Icons.dashboard_outlined;
    }
  }

  static String _initial(String name) {
    final value = name.trim();

    if (value.isEmpty) {
      return 'G';
    }

    return value[0].toUpperCase();
  }
}

// ============================================================
// CATEGORY CARD
// ============================================================

class _CategoryCard extends StatelessWidget {
  final IconData icon;
  final String title;

  const _CategoryCard({required this.icon, required this.title});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 15, horizontal: 3),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(17),
        border: Border.all(color: const Color(0xFFE7E3EB)),
      ),
      child: Column(
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: const BoxDecoration(
              color: Color(0xFFF1ECF7),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: Color(0xFF7356A3), size: 22),
          ),
          const SizedBox(height: 8),
          Text(
            title,
            textAlign: TextAlign.center,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w700),
          ),
        ],
      ),
    );
  }
}

// ============================================================
// GEM CARD
// ============================================================

class _GemCard extends StatelessWidget {
  final String name;
  final String origin;
  final String price;

  const _GemCard({
    required this.name,
    required this.origin,
    required this.price,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(19),
        border: Border.all(color: const Color(0xFFE7E3EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Container(
              width: double.infinity,
              decoration: const BoxDecoration(
                color: Color(0xFFF3EFF7),
                borderRadius: BorderRadius.vertical(top: Radius.circular(18)),
              ),
              child: const Center(
                child: Icon(Icons.diamond, color: Color(0xFF7356A3), size: 54),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  origin,
                  style: const TextStyle(
                    fontSize: 10,
                    color: Color(0xFF85818C),
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  price,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Color(0xFF7356A3),
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

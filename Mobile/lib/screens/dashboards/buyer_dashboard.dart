import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../export_compliance/my_export_requests_screen.dart';
import '../marketplace/marketplace_screen.dart';

class BuyerDashboard extends StatelessWidget {
  const BuyerDashboard({super.key});

  static const Color _darkGreen = Color(0xFF08251E);
  static const Color _green = Color(0xFF16483B);
  static const Color _gold = Color(0xFFC99242);
  static const Color _lightGold = Color(0xFFE4BC74);
  static const Color _cream = Color(0xFFFAF7F0);
  static const Color _text = Color(0xFF10241F);
  static const Color _muted = Color(0xFF697771);
  static const Color _border = Color(0xFFE7DFD2);

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final user = authProvider.user;

    return Scaffold(
      backgroundColor: _cream,
      appBar: AppBar(
        backgroundColor: _darkGreen,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'GEMORA',
              style: TextStyle(
                fontSize: 19,
                fontWeight: FontWeight.w800,
                letterSpacing: 1.8,
              ),
            ),
            Text(
              'Buyer Workspace',
              style: TextStyle(fontSize: 10, color: Colors.white60),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(22),
          children: [
            // ============================================================
            // WELCOME
            // ============================================================

            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(22),
                gradient: const LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [_darkGreen, _green],
                ),
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
                  const Text(
                    'BUYER WORKSPACE',
                    style: TextStyle(
                      color: _lightGold,
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 1.3,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Welcome, ${user?.fullName ?? 'Buyer'}',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 27,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Discover professionally verified gemstones and manage your Gemora buyer activity.',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: 14,
                      height: 1.5,
                    ),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 7,
                    ),
                    decoration: BoxDecoration(
                      color: _lightGold.withValues(alpha: 0.14),
                      borderRadius: BorderRadius.circular(99),
                      border: Border.all(
                        color: _lightGold.withValues(alpha: 0.28),
                      ),
                    ),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.verified_user_outlined,
                          color: _lightGold,
                          size: 15,
                        ),
                        SizedBox(width: 6),
                        Text(
                          'BUYER ACCOUNT',
                          style: TextStyle(
                            color: _lightGold,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.7,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 30),

            // ============================================================
            // BUYER SERVICES
            // ============================================================
            const Text(
              'Buyer Services',
              style: TextStyle(
                color: _text,
                fontSize: 21,
                fontWeight: FontWeight.w800,
              ),
            ),

            const SizedBox(height: 7),

            const Text(
              'Explore verified gemstones and manage services available for your buyer account.',
              style: TextStyle(color: _muted, fontSize: 13, height: 1.45),
            ),

            const SizedBox(height: 20),

            // ============================================================
            // VERIFIED GEM MARKETPLACE
            // ============================================================
            _BuyerActionCard(
              icon: Icons.diamond_outlined,
              title: 'Browse Verified Gems',
              description: 'Explore professionally approved gemstones available on the Gemora marketplace.',
              badge: 'MARKETPLACE',
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => MarketplaceScreen(
                      isAuthenticated: true,
                      displayName: user?.fullName,
                      role: 'Buyer',
                      onWorkspace: () {
                        Navigator.of(context).pop();
                      },
                      onProfile: () {
                        Navigator.of(context).pop();
                      },
                    ),
                  ),
                );
              },
            ),

            const SizedBox(height: 14),

            // ============================================================
            // EXPORT REQUESTS
            // ============================================================
            _BuyerActionCard(
              icon: Icons.flight_takeoff_rounded,
              title: 'Track My Exports',
              description: 'View your gemstone export requests and monitor their compliance status.',
              badge: 'EXPORT',
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => const MyExportRequestsScreen(),
                  ),
                );
              },
            ),

            const SizedBox(height: 30),

            // ============================================================
            // MARKETPLACE INFORMATION
            // ============================================================
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: const Color(0xFFF4EBDD),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: const Color(0xFFE6D3B4)),
              ),
              child: const Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.verified_rounded,
                    color: Color(0xFF247A50),
                    size: 23,
                  ),
                  SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Verified Marketplace',
                          style: TextStyle(
                            color: _text,
                            fontSize: 14,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        SizedBox(height: 5),
                        Text(
                          'Gemstones shown in the marketplace have completed the Gemora verification workflow before being approved for buyers.',
                          style: TextStyle(
                            color: _muted,
                            fontSize: 12,
                            height: 1.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 30),

            // ============================================================
            // ACCOUNT
            // ============================================================
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                border: Border.all(color: _border),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'ACCOUNT',
                    style: TextStyle(
                      color: _gold,
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 1.2,
                    ),
                  ),

                  const SizedBox(height: 12),

                  Row(
                    children: [
                      Container(
                        width: 46,
                        height: 46,
                        decoration: BoxDecoration(
                          color: const Color(0xFFF1E7D5),
                          borderRadius: BorderRadius.circular(13),
                        ),
                        child: const Icon(
                          Icons.person_outline_rounded,
                          color: _green,
                        ),
                      ),
                      const SizedBox(width: 13),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              user?.fullName ?? 'Gemora Buyer',
                              style: const TextStyle(
                                color: _text,
                                fontSize: 16,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            const SizedBox(height: 5),
                            Text(
                              user?.email ?? '',
                              style: const TextStyle(
                                color: _muted,
                                fontSize: 13,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

// ============================================================
// BUYER ACTION CARD
// ============================================================

class _BuyerActionCard extends StatelessWidget {
  final IconData icon;

  final String title;

  final String description;

  final String badge;

  final VoidCallback onTap;

  const _BuyerActionCard({
    required this.icon,
    required this.title,
    required this.description,
    required this.badge,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(18),
        child: Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            border: Border.all(color: const Color(0xFFE7DFD2)),
            borderRadius: BorderRadius.circular(18),
          ),
          child: Row(
            children: [
              Container(
                width: 54,
                height: 54,
                decoration: BoxDecoration(
                  color: const Color(0xFF102E27),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(icon, color: const Color(0xFFE4BC74), size: 27),
              ),

              const SizedBox(width: 16),

              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            title,
                            style: const TextStyle(
                              color: Color(0xFF10241F),
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 7,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: const Color(0xFFF1E7D5),
                            borderRadius: BorderRadius.circular(99),
                          ),
                          child: Text(
                            badge,
                            style: const TextStyle(
                              color: Color(0xFF9B6A2D),
                              fontSize: 7,
                              fontWeight: FontWeight.w900,
                              letterSpacing: 0.6,
                            ),
                          ),
                        ),
                      ],
                    ),

                    const SizedBox(height: 6),

                    Text(
                      description,
                      style: const TextStyle(
                        color: Color(0xFF697771),
                        fontSize: 12,
                        height: 1.45,
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(width: 10),

              const Icon(
                Icons.arrow_forward_ios_rounded,
                size: 16,
                color: Color(0xFFC99242),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

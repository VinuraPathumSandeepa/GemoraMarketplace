import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../export_compliance/my_export_requests_screen.dart';
import '../seller/create_gem_listing_screen.dart';
import '../seller/my_gem_listings_screen.dart';

class SellerDashboard extends StatelessWidget {
  const SellerDashboard({super.key});

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();

    final user = authProvider.user;

    return Scaffold(
      backgroundColor: const Color(0xFFFAF7F0),

      // ============================================================
      // APP BAR
      // ============================================================
      appBar: AppBar(
        backgroundColor: const Color(0xFF08251E),
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
              'Seller Workspace',
              style: TextStyle(fontSize: 10, color: Colors.white60),
            ),
          ],
        ),
      ),

      // ============================================================
      // BODY
      // ============================================================
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(22),
          children: [
            // ======================================================
            // WELCOME HERO
            // ======================================================
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(22),
                gradient: const LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [Color(0xFF08251E), Color(0xFF16483B)],
                ),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.08),
                    blurRadius: 22,
                    offset: const Offset(0, 10),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'SELLER WORKSPACE',
                    style: TextStyle(
                      color: Color(0xFFE4BC74),
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 1.3,
                    ),
                  ),

                  const SizedBox(height: 12),

                  Text(
                    'Welcome, ${user?.fullName ?? 'Seller'}',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 27,
                      fontWeight: FontWeight.w800,
                    ),
                  ),

                  const SizedBox(height: 8),

                  const Text(
                    'Create gemstone listings, prepare supporting evidence, and submit your gems for professional verification.',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: 14,
                      height: 1.55,
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 30),

            // ======================================================
            // GEM LISTING MANAGEMENT
            // ======================================================
            const Text(
              'Gem Listing Management',
              style: TextStyle(
                color: Color(0xFF10241F),
                fontSize: 21,
                fontWeight: FontWeight.w800,
              ),
            ),

            const SizedBox(height: 7),

            const Text(
              'Manage your gemstone inventory and verification workflow.',
              style: TextStyle(
                color: Color(0xFF697771),
                fontSize: 13,
                height: 1.4,
              ),
            ),

            const SizedBox(height: 20),

            // ======================================================
            // MY GEM LISTINGS
            // ======================================================
            _DashboardActionCard(
              icon: Icons.diamond_outlined,
              title: 'My Gem Listings',
              description: 'View Draft, Pending Verification, Approved, Changes Requested and Rejected listings.',
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => const MyGemListingsScreen(),
                  ),
                );
              },
            ),

            const SizedBox(height: 14),

            // ======================================================
            // CREATE GEM LISTING
            // ======================================================
            _DashboardActionCard(
              icon: Icons.add_circle_outline_rounded,
              title: 'Create Gem Listing',
              description: 'Create a new gemstone draft and prepare it for professional verification.',
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => const CreateGemListingScreen(),
                  ),
                );
              },
            ),

            const SizedBox(height: 30),

            // ======================================================
            // EXPORT COMPLIANCE
            // ======================================================
            const Text(
              'Export Compliance',
              style: TextStyle(
                color: Color(0xFF10241F),
                fontSize: 21,
                fontWeight: FontWeight.w800,
              ),
            ),

            const SizedBox(height: 7),

            const Text(
              'Track gemstone export requests and compliance activity.',
              style: TextStyle(
                color: Color(0xFF697771),
                fontSize: 13,
                height: 1.4,
              ),
            ),

            const SizedBox(height: 20),

            // ======================================================
            // MY EXPORT REQUESTS
            // ======================================================
            _DashboardActionCard(
              icon: Icons.flight_takeoff_rounded,
              title: 'My Export Requests',
              description: 'View and track your gemstone export requests and their compliance status.',
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => const MyExportRequestsScreen(),
                  ),
                );
              },
            ),

            const SizedBox(height: 30),

            // ======================================================
            // VERIFICATION INFORMATION
            // ======================================================
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: const Color(0xFFFFF9ED),
                border: Border.all(color: const Color(0xFFE6CE9F)),
                borderRadius: BorderRadius.circular(16),
              ),
              child: const Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.verified_user_outlined,
                    color: Color(0xFFC99242),
                    size: 26,
                  ),

                  SizedBox(width: 13),

                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Human-Controlled Verification',
                          style: TextStyle(
                            color: Color(0xFF18372E),
                            fontSize: 14,
                            fontWeight: FontWeight.w800,
                          ),
                        ),

                        SizedBox(height: 5),

                        Text(
                          'Gemora AI assists the gemstone verification workflow, but the final verification decision always remains with a human Gemologist.',
                          style: TextStyle(
                            color: Color(0xFF52615C),
                            fontSize: 12,
                            height: 1.55,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 30),

            // ======================================================
            // ACCOUNT INFORMATION
            // ======================================================
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                border: Border.all(color: const Color(0xFFE7DFD2)),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'ACCOUNT',
                    style: TextStyle(
                      color: Color(0xFFC99242),
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 1.2,
                    ),
                  ),

                  const SizedBox(height: 12),

                  _AccountRow(
                    icon: Icons.person_outline_rounded,
                    label: 'Seller',
                    value: user?.fullName ?? 'Gemora Seller',
                  ),

                  const Divider(height: 24),

                  _AccountRow(
                    icon: Icons.mail_outline_rounded,
                    label: 'Email',
                    value: user?.email ?? '',
                  ),

                  const Divider(height: 24),

                  const _AccountRow(
                    icon: Icons.shield_outlined,
                    label: 'Role',
                    value: 'Seller',
                  ),
                ],
              ),
            ),

            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }
}

// ============================================================
// DASHBOARD ACTION CARD
// ============================================================

class _DashboardActionCard extends StatelessWidget {
  final IconData icon;

  final String title;

  final String description;

  final VoidCallback onTap;

  const _DashboardActionCard({
    required this.icon,
    required this.title,
    required this.description,
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
                    Text(
                      title,
                      style: const TextStyle(
                        color: Color(0xFF10241F),
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                      ),
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

// ============================================================
// ACCOUNT ROW
// ============================================================

class _AccountRow extends StatelessWidget {
  final IconData icon;

  final String label;

  final String value;

  const _AccountRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: const Color(0xFFF4F0E7),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(icon, size: 19, color: const Color(0xFFC99242)),
        ),

        const SizedBox(width: 12),

        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(
                  color: Color(0xFF8A918E),
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                ),
              ),

              const SizedBox(height: 2),

              Text(
                value,
                style: const TextStyle(
                  color: Color(0xFF213A32),
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

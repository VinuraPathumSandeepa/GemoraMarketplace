import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../auth/login_screen.dart';
import '../auth/register_screen.dart';
import '../dashboards/role_dashboard.dart';

// ============================================================
// GEMORA PUBLIC THEME
// Matches the Web / Seller Workspace theme.
// ============================================================

const Color gemoraDarkGreen = Color(0xFF08251E);
const Color gemoraGreen = Color(0xFF16483B);
const Color gemoraGold = Color(0xFFE4BC74);
const Color gemoraDarkGold = Color(0xFFC99242);
const Color gemoraCream = Color(0xFFFAF7F0);
const Color gemoraText = Color(0xFF10241F);
const Color gemoraMuted = Color(0xFF697771);
const Color gemoraBorder = Color(0xFFE7DFD2);

// ============================================================
// PUBLIC HOME
//
// APP START
//    ↓
// Public Home
//    ├── Login
//    └── Create Account
//
// After successful login
//    ↓
// RoleDashboard
// ============================================================

class PublicHomeScreen extends StatelessWidget {
  const PublicHomeScreen({super.key});

  // ============================================================
  // LOGIN
  // ============================================================

  void _openLogin(BuildContext context) {
    Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => const _LoginGateway()));
  }

  // ============================================================
  // REGISTER
  // ============================================================

  void _openRegister(BuildContext context) {
    Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => const RegisterScreen()));
  }

  // ============================================================
  // BUILD
  // ============================================================

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: gemoraCream,
      body: SafeArea(
        child: CustomScrollView(
          slivers: [
            // ==================================================
            // TOP NAVIGATION
            // ==================================================

            SliverToBoxAdapter(
              child: _PublicHeader(
                onLogin: () {
                  _openLogin(context);
                },
                onRegister: () {
                  _openRegister(context);
                },
              ),
            ),

            // ==================================================
            // HERO
            // ==================================================
            SliverToBoxAdapter(
              child: _HeroSection(
                onLogin: () {
                  _openLogin(context);
                },
                onRegister: () {
                  _openRegister(context);
                },
              ),
            ),

            // ==================================================
            // TRUST FEATURES
            // ==================================================
            const SliverToBoxAdapter(child: _TrustBar()),

            // ==================================================
            // EXPLORE GEMORA
            // ==================================================
            const SliverToBoxAdapter(child: _ExploreSection()),

            // ==================================================
            // AI + HUMAN VERIFICATION
            // ==================================================
            const SliverToBoxAdapter(child: _VerificationSection()),

            // ==================================================
            // FINAL CALL TO ACTION
            // ==================================================
            SliverToBoxAdapter(
              child: _FinalCallToAction(
                onLogin: () {
                  _openLogin(context);
                },
                onRegister: () {
                  _openRegister(context);
                },
              ),
            ),

            // ==================================================
            // FOOTER
            // ==================================================
            const SliverToBoxAdapter(child: _Footer()),
          ],
        ),
      ),
    );
  }
}

// ============================================================
// HEADER
// ============================================================

class _PublicHeader extends StatelessWidget {
  final VoidCallback onLogin;
  final VoidCallback onRegister;

  const _PublicHeader({required this.onLogin, required this.onRegister});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: gemoraDarkGreen,
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      child: Row(
        children: [
          // LOGO

          Container(
            width: 45,
            height: 45,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(13),
            ),
            child: const Icon(
              Icons.diamond_outlined,
              color: gemoraGold,
              size: 27,
            ),
          ),

          const SizedBox(width: 11),

          // BRAND
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'GEMORA',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 19,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.8,
                  ),
                ),
                Text(
                  'Gemstone Marketplace',
                  style: TextStyle(color: Colors.white60, fontSize: 10),
                ),
              ],
            ),
          ),

          // LOGIN
          TextButton(
            onPressed: onLogin,
            child: const Text(
              'Login',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),

          const SizedBox(width: 4),

          // JOIN
          FilledButton(
            onPressed: onRegister,
            style: FilledButton.styleFrom(
              backgroundColor: gemoraGold,
              foregroundColor: gemoraDarkGreen,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            ),
            child: const Text(
              'Join',
              style: TextStyle(fontWeight: FontWeight.w800),
            ),
          ),
        ],
      ),
    );
  }
}

// ============================================================
// HERO
// ============================================================

class _HeroSection extends StatelessWidget {
  final VoidCallback onLogin;
  final VoidCallback onRegister;

  const _HeroSection({required this.onLogin, required this.onRegister});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(22, 42, 22, 46),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [gemoraDarkGreen, gemoraGreen],
        ),
        borderRadius: BorderRadius.only(
          bottomLeft: Radius.circular(30),
          bottomRight: Radius.circular(30),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // LABEL

          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(30),
            ),
            child: const Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.auto_awesome, color: gemoraGold, size: 15),
                SizedBox(width: 7),
                Text(
                  'SRI LANKAN GEMSTONE MARKETPLACE',
                  style: TextStyle(
                    color: gemoraGold,
                    fontSize: 9,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1,
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 24),

          // TITLE
          const Text(
            'Discover Sri Lanka\'s\nfinest gemstones.',
            style: TextStyle(
              color: Colors.white,
              fontSize: 36,
              height: 1.12,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.5,
            ),
          ),

          const SizedBox(height: 17),

          const Text(
            'A trusted marketplace connecting buyers, sellers and professional gemologists with AI-assisted verification.',
            style: TextStyle(color: Colors.white70, fontSize: 14, height: 1.6),
          ),

          const SizedBox(height: 28),

          // GEM VISUAL
          Center(
            child: Container(
              width: 165,
              height: 165,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.04),
                border: Border.all(color: gemoraGold.withValues(alpha: 0.30)),
              ),
              child: const Center(
                child: Icon(Icons.diamond, size: 86, color: gemoraGold),
              ),
            ),
          ),

          const SizedBox(height: 32),

          // LOGIN
          SizedBox(
            width: double.infinity,
            height: 53,
            child: FilledButton.icon(
              onPressed: onLogin,
              style: FilledButton.styleFrom(
                backgroundColor: gemoraGold,
                foregroundColor: gemoraDarkGreen,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
              icon: const Icon(Icons.login_rounded),
              label: const Text(
                'Login to Gemora',
                style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
              ),
            ),
          ),

          const SizedBox(height: 12),

          // REGISTER
          SizedBox(
            width: double.infinity,
            height: 53,
            child: OutlinedButton.icon(
              onPressed: onRegister,
              style: OutlinedButton.styleFrom(
                foregroundColor: Colors.white,
                side: BorderSide(color: Colors.white.withValues(alpha: 0.40)),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
              icon: const Icon(Icons.person_add_alt_1_rounded),
              label: const Text(
                'Create Account',
                style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ============================================================
// TRUST BAR
// ============================================================

class _TrustBar extends StatelessWidget {
  const _TrustBar();

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFFF2EADD),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 22),
      child: const Row(
        children: [
          Expanded(
            child: _TrustItem(icon: Icons.verified_outlined, title: 'Verified'),
          ),
          Expanded(
            child: _TrustItem(
              icon: Icons.psychology_outlined,
              title: 'AI Assisted',
            ),
          ),
          Expanded(
            child: _TrustItem(icon: Icons.security_outlined, title: 'Secure'),
          ),
        ],
      ),
    );
  }
}

class _TrustItem extends StatelessWidget {
  final IconData icon;
  final String title;

  const _TrustItem({required this.icon, required this.title});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Icon(icon, color: gemoraGreen, size: 27),
        const SizedBox(height: 7),
        Text(
          title,
          style: const TextStyle(
            color: gemoraText,
            fontSize: 11,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    );
  }
}

// ============================================================
// EXPLORE SECTION
// ============================================================

class _ExploreSection extends StatelessWidget {
  const _ExploreSection();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 38, 20, 38),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Explore Gemora',
            style: TextStyle(
              color: gemoraText,
              fontSize: 24,
              fontWeight: FontWeight.w800,
            ),
          ),

          const SizedBox(height: 6),

          const Text(
            'One trusted platform for the complete gemstone journey.',
            style: TextStyle(color: gemoraMuted, fontSize: 13, height: 1.45),
          ),

          const SizedBox(height: 22),

          // MARKETPLACE
          const _FeatureCard(
            icon: Icons.diamond_outlined,
            title: 'Gem Marketplace',
            description: 'Explore gemstone listings and discover trusted Sri Lankan gems.',
          ),

          const SizedBox(height: 13),

          // VERIFICATION
          const _FeatureCard(
            icon: Icons.verified_user_outlined,
            title: 'Professional Verification',
            description: 'Gemologists review evidence and make the final verification decision.',
          ),

          const SizedBox(height: 13),

          // AI
          const _FeatureCard(
            icon: Icons.psychology_outlined,
            title: 'AI-Assisted Analysis',
            description: 'Gemora AI supports verification by analyzing gemstone images and listing evidence.',
          ),

          const SizedBox(height: 13),

          // EXPORT
          const _FeatureCard(
            icon: Icons.flight_takeoff_outlined,
            title: 'Export Compliance',
            description: 'Support gemstone export requests and compliance workflows through one platform.',
          ),
        ],
      ),
    );
  }
}

// ============================================================
// FEATURE CARD
// ============================================================

class _FeatureCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final String description;

  const _FeatureCard({
    required this.icon,
    required this.title,
    required this.description,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: gemoraBorder),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 50,
            height: 50,
            decoration: BoxDecoration(
              color: const Color(0xFFF2EADD),
              borderRadius: BorderRadius.circular(14),
            ),
            child: Icon(icon, color: gemoraGreen, size: 26),
          ),

          const SizedBox(width: 15),

          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: gemoraText,
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  description,
                  style: const TextStyle(
                    color: gemoraMuted,
                    fontSize: 12,
                    height: 1.5,
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

// ============================================================
// VERIFICATION SECTION
// ============================================================

class _VerificationSection extends StatelessWidget {
  const _VerificationSection();

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 20),
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        color: gemoraDarkGreen,
        borderRadius: BorderRadius.circular(22),
      ),
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.verified_user_outlined, color: gemoraGold, size: 38),

          SizedBox(height: 17),

          Text(
            'AI assists.\nGemologists decide.',
            style: TextStyle(
              color: Colors.white,
              fontSize: 25,
              height: 1.15,
              fontWeight: FontWeight.w800,
            ),
          ),

          SizedBox(height: 12),

          Text(
            'Gemora combines AI-assisted gemstone analysis with professional human review. The final verification decision always remains with the Gemologist.',
            style: TextStyle(color: Colors.white70, fontSize: 13, height: 1.55),
          ),
        ],
      ),
    );
  }
}

// ============================================================
// FINAL CTA
// ============================================================

class _FinalCallToAction extends StatelessWidget {
  final VoidCallback onLogin;
  final VoidCallback onRegister;

  const _FinalCallToAction({required this.onLogin, required this.onRegister});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 38, 20, 44),
      child: Column(
        children: [
          const Text(
            'Ready to explore Gemora?',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: gemoraText,
              fontSize: 24,
              fontWeight: FontWeight.w800,
            ),
          ),

          const SizedBox(height: 9),

          const Text(
            'Create your account or login to continue.',
            textAlign: TextAlign.center,
            style: TextStyle(color: gemoraMuted, fontSize: 13),
          ),

          const SizedBox(height: 22),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: FilledButton(
              onPressed: onRegister,
              style: FilledButton.styleFrom(
                backgroundColor: gemoraDarkGreen,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
              child: const Text(
                'Create Account',
                style: TextStyle(fontWeight: FontWeight.w800),
              ),
            ),
          ),

          const SizedBox(height: 8),

          TextButton(
            onPressed: onLogin,
            child: const Text(
              'Already have an account? Login',
              style: TextStyle(color: gemoraGreen, fontWeight: FontWeight.w700),
            ),
          ),
        ],
      ),
    );
  }
}

// ============================================================
// FOOTER
// ============================================================

class _Footer extends StatelessWidget {
  const _Footer();

  @override
  Widget build(BuildContext context) {
    return Container(
      color: gemoraDarkGreen,
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 28),
      child: const Column(
        children: [
          Icon(Icons.diamond_outlined, color: gemoraGold, size: 30),

          SizedBox(height: 9),

          Text(
            'GEMORA',
            style: TextStyle(
              color: Colors.white,
              fontSize: 17,
              fontWeight: FontWeight.w800,
              letterSpacing: 1.8,
            ),
          ),

          SizedBox(height: 4),

          Text(
            'Sri Lankan Gemstone Marketplace',
            style: TextStyle(color: Colors.white54, fontSize: 10),
          ),
        ],
      ),
    );
  }
}

// ============================================================
// LOGIN GATEWAY
//
// LoginScreen remains unchanged.
//
// Before successful login:
// LoginScreen
//
// After authentication:
// RoleDashboard
// ============================================================

class _LoginGateway extends StatelessWidget {
  const _LoginGateway();

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();

    if (authProvider.isAuthenticated) {
      return const RoleDashboard();
    }

    return const LoginScreen();
  }
}

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/gemora_colors.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/buyer_glass.dart';

class BuyerOverviewScreen extends StatefulWidget {
  final VoidCallback? onExplore;
  final VoidCallback? onAskAi;

  const BuyerOverviewScreen({super.key, this.onExplore, this.onAskAi});

  @override
  State<BuyerOverviewScreen> createState() => _BuyerOverviewScreenState();
}

class _BuyerOverviewScreenState extends State<BuyerOverviewScreen>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 850),
    )..forward();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final firstName = user?.fullName.trim().isNotEmpty == true
        ? user!.fullName.trim().split(' ').first
        : 'Gem Explorer';

    return BuyerGradientBackground(
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 116),
        children: [
          _Entrance(
            animation: _controller,
            interval: const Interval(0, .55, curve: Curves.easeOutCubic),
            child: _Hero(
              firstName: firstName,
              onExplore: widget.onExplore,
              onAskAi: widget.onAskAi,
            ),
          ),
          const SizedBox(height: 24),
          _Entrance(
            animation: _controller,
            interval: const Interval(.18, .72, curve: Curves.easeOutCubic),
            child: const BuyerSectionHeading(
              eyebrow: 'Gemora assurance',
              title: 'Buy with clarity',
              description: 'Every step is visible—from gem verification to protected payment and delivery.',
            ),
          ),
          const SizedBox(height: 14),
          _Entrance(
            animation: _controller,
            interval: const Interval(.28, .82, curve: Curves.easeOutCubic),
            child: const Row(
              children: [
                Expanded(
                  child: _TrustCard(
                    icon: Icons.verified_outlined,
                    value: 'Verified',
                    label: 'Gemologist reviewed',
                  ),
                ),
                SizedBox(width: 10),
                Expanded(
                  child: _TrustCard(
                    icon: Icons.lock_outline_rounded,
                    value: 'Protected',
                    label: 'Secure card flow',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          _Entrance(
            animation: _controller,
            interval: const Interval(.38, .9, curve: Curves.easeOutCubic),
            child: const Row(
              children: [
                Expanded(
                  child: _TrustCard(
                    icon: Icons.public_rounded,
                    value: 'Compliant',
                    label: 'Export guidance',
                  ),
                ),
                SizedBox(width: 10),
                Expanded(
                  child: _TrustCard(
                    icon: Icons.local_shipping_outlined,
                    value: 'Tracked',
                    label: 'Courier milestones',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          _Entrance(
            animation: _controller,
            interval: const Interval(.5, 1, curve: Curves.easeOutCubic),
            child: BuyerGlassPanel(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: GemoraColors.gold.withValues(alpha: .2),
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: const Icon(
                      Icons.auto_awesome_rounded,
                      color: GemoraColors.darkGold,
                    ),
                  ),
                  const SizedBox(width: 14),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'A smarter gemstone journey',
                          style: TextStyle(
                            color: GemoraColors.darkGreen,
                            fontSize: 16,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        SizedBox(height: 6),
                        Text(
                          'Ask Gemora AI for verified gemstone recommendations, budget guidance and marketplace help at any time.',
                          style: TextStyle(
                            color: GemoraColors.mutedText,
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
          ),
        ],
      ),
    );
  }
}

class _Hero extends StatelessWidget {
  final String firstName;
  final VoidCallback? onExplore;
  final VoidCallback? onAskAi;

  const _Hero({required this.firstName, this.onExplore, this.onAskAi});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(22, 24, 22, 22),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF061F19), Color(0xFF0B3B30), Color(0xFF1B5948)],
        ),
        borderRadius: BorderRadius.circular(30),
        border: Border.all(color: Colors.white12),
        boxShadow: [
          BoxShadow(
            color: GemoraColors.darkGreen.withValues(alpha: .23),
            blurRadius: 34,
            offset: const Offset(0, 16),
          ),
        ],
      ),
      child: Stack(
        children: [
          Positioned(
            right: -55,
            top: -60,
            child: Container(
              width: 180,
              height: 180,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(
                  color: Colors.white.withValues(alpha: .07),
                  width: 24,
                ),
              ),
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.diamond_outlined,
                    color: GemoraColors.gold,
                    size: 18,
                  ),
                  SizedBox(width: 8),
                  Text(
                    'BUYER EXPERIENCE',
                    style: TextStyle(
                      color: GemoraColors.gold,
                      fontSize: 10,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.3,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 22),
              Text(
                'Hello, $firstName',
                style: const TextStyle(color: Colors.white70, fontSize: 14),
              ),
              const SizedBox(height: 5),
              const Text(
                'Find a gemstone\nworth remembering.',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 29,
                  height: 1.08,
                  fontWeight: FontWeight.w900,
                  letterSpacing: -.7,
                ),
              ),
              const SizedBox(height: 13),
              const Text(
                'Explore trusted listings, complete secure payments and follow delivery from one elegant workspace.',
                style: TextStyle(
                  color: Colors.white60,
                  fontSize: 12,
                  height: 1.5,
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: onExplore,
                      style: FilledButton.styleFrom(
                        backgroundColor: GemoraColors.gold,
                        foregroundColor: GemoraColors.darkGreen,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(15),
                        ),
                      ),
                      icon: const Icon(Icons.explore_outlined, size: 19),
                      label: const Text(
                        'Explore gems',
                        style: TextStyle(fontWeight: FontWeight.w900),
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  IconButton.filledTonal(
                    tooltip: 'Ask Gemora AI',
                    onPressed: onAskAi,
                    style: IconButton.styleFrom(
                      backgroundColor: Colors.white.withValues(alpha: .1),
                      foregroundColor: GemoraColors.gold,
                      side: BorderSide(
                        color: Colors.white.withValues(alpha: .12),
                      ),
                      padding: const EdgeInsets.all(14),
                    ),
                    icon: const Icon(Icons.chat_bubble_outline_rounded),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _TrustCard extends StatelessWidget {
  final IconData icon;
  final String value;
  final String label;

  const _TrustCard({
    required this.icon,
    required this.value,
    required this.label,
  });

  @override
  Widget build(BuildContext context) {
    return BuyerGlassPanel(
      padding: const EdgeInsets.all(15),
      borderRadius: BorderRadius.circular(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: GemoraColors.green.withValues(alpha: .1),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: GemoraColors.green, size: 19),
          ),
          const SizedBox(height: 12),
          Text(
            value,
            style: const TextStyle(
              color: GemoraColors.darkGreen,
              fontSize: 15,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            label,
            style: const TextStyle(color: GemoraColors.mutedText, fontSize: 10),
          ),
        ],
      ),
    );
  }
}

class _Entrance extends StatelessWidget {
  final Animation<double> animation;
  final Interval interval;
  final Widget child;

  const _Entrance({
    required this.animation,
    required this.interval,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    final curved = CurvedAnimation(parent: animation, curve: interval);
    return FadeTransition(
      opacity: curved,
      child: SlideTransition(
        position: Tween(
          begin: const Offset(0, .09),
          end: Offset.zero,
        ).animate(curved),
        child: child,
      ),
    );
  }
}

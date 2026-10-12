import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/gemora_colors.dart';
import '../../providers/auth_provider.dart';
import '../../services/export_compliance_service.dart';
import '../../widgets/notification_bell.dart';
import '../marketplace/ai_buyer_assistant_screen.dart';
import '../marketplace/marketplace_home_screen.dart';
import 'buyer_overview_screen.dart';
import 'buyer_profile_screen.dart';

class BuyerDashboard extends StatefulWidget {
  // Kept for compatibility with existing injected tracking tests/flows.
  final ExportComplianceService? exportComplianceService;

  const BuyerDashboard({super.key, this.exportComplianceService});

  @override
  State<BuyerDashboard> createState() => _BuyerDashboardState();
}

class _BuyerDashboardState extends State<BuyerDashboard> {
  static const _titles = ['Buyer Home', 'Explore Gems', 'Profile'];
  int _index = 0;

  void _select(int value) => setState(() => _index = value);

  Future<void> _openAssistant() async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Colors.transparent,
      builder: (context) => DraggableScrollableSheet(
        initialChildSize: .86,
        minChildSize: .58,
        maxChildSize: .96,
        expand: false,
        builder: (context, controller) => ClipRRect(
          borderRadius: const BorderRadius.vertical(top: Radius.circular(30)),
          child: Material(
            color: const Color(0xFFF7F3EA),
            child: Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(18, 11, 10, 4),
                  child: Row(
                    children: [
                      Container(
                        width: 42,
                        height: 4,
                        decoration: BoxDecoration(
                          color: GemoraColors.border,
                          borderRadius: BorderRadius.circular(99),
                        ),
                      ),
                      const Spacer(),
                      IconButton(
                        tooltip: 'Close assistant',
                        onPressed: () => Navigator.pop(context),
                        icon: const Icon(Icons.close_rounded),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: AiBuyerAssistantScreen(scrollController: controller),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final pages = [
      BuyerOverviewScreen(onExplore: () => _select(1), onAskAi: _openAssistant),
      const MarketplaceHomeScreen(),
      const BuyerProfileScreen(),
    ];

    return Scaffold(
      extendBody: true,
      backgroundColor: const Color(0xFFF7F3EA),
      appBar: AppBar(
        elevation: 0,
        scrolledUnderElevation: 0,
        backgroundColor: GemoraColors.darkGreen,
        foregroundColor: Colors.white,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'GEMORA',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w900,
                letterSpacing: 1.8,
              ),
            ),
            Text(
              _titles[_index],
              style: const TextStyle(color: Colors.white60, fontSize: 9),
            ),
          ],
        ),
        actions: [
          const NotificationBell(),
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: CircleAvatar(
              radius: 17,
              backgroundColor: GemoraColors.gold,
              child: Text(
                (user?.fullName.isNotEmpty == true
                        ? user!.fullName.substring(0, 1)
                        : 'B')
                    .toUpperCase(),
                style: const TextStyle(
                  color: GemoraColors.darkGreen,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
          ),
        ],
      ),
      body: IndexedStack(index: _index, children: pages),
      floatingActionButton: _index == 0
          ? _GemoraAiPill(onPressed: _openAssistant)
          : null,
      floatingActionButtonLocation: FloatingActionButtonLocation.endFloat,
      bottomNavigationBar: _GlassNavigationDock(
        selectedIndex: _index,
        onSelected: _select,
      ),
    );
  }
}

class _GemoraAiPill extends StatefulWidget {
  final VoidCallback onPressed;

  const _GemoraAiPill({required this.onPressed});

  @override
  State<_GemoraAiPill> createState() => _GemoraAiPillState();
}

class _GemoraAiPillState extends State<_GemoraAiPill>
    with SingleTickerProviderStateMixin {
  late final AnimationController _pulse;

  @override
  void initState() {
    super.initState();
    _pulse = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1600),
      lowerBound: 0,
      upperBound: 1,
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _pulse.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 78),
      child: AnimatedBuilder(
        animation: _pulse,
        builder: (context, child) => Transform.translate(
          offset: Offset(0, -2 * _pulse.value),
          child: child,
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            key: const ValueKey('ask-gemora-ai-pill'),
            onTap: widget.onPressed,
            borderRadius: BorderRadius.circular(99),
            child: Container(
              padding: const EdgeInsets.fromLTRB(15, 11, 17, 11),
              decoration: BoxDecoration(
                color: const Color(0xFF031F1A),
                borderRadius: BorderRadius.circular(99),
                border: Border.all(color: GemoraColors.darkGold),
                boxShadow: [
                  BoxShadow(
                    color: GemoraColors.darkGreen.withValues(alpha: .24),
                    blurRadius: 22,
                    offset: const Offset(0, 10),
                  ),
                ],
              ),
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.chat_bubble_outline_rounded,
                    color: GemoraColors.gold,
                    size: 25,
                  ),
                  SizedBox(width: 10),
                  Text(
                    'Ask Gemora AI',
                    style: TextStyle(
                      color: GemoraColors.gold,
                      fontSize: 15,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _GlassNavigationDock extends StatelessWidget {
  final int selectedIndex;
  final ValueChanged<int> onSelected;

  const _GlassNavigationDock({
    required this.selectedIndex,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      minimum: const EdgeInsets.fromLTRB(14, 0, 14, 12),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(26),
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 24, sigmaY: 24),
          child: Container(
            decoration: BoxDecoration(
              color: GemoraColors.darkGreen.withValues(alpha: .91),
              borderRadius: BorderRadius.circular(26),
              border: Border.all(color: Colors.white.withValues(alpha: .13)),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: .2),
                  blurRadius: 26,
                  offset: const Offset(0, 12),
                ),
              ],
            ),
            child: NavigationBarTheme(
              data: NavigationBarThemeData(
                labelTextStyle: WidgetStateProperty.resolveWith((states) {
                  final selected = states.contains(WidgetState.selected);
                  return TextStyle(
                    color: selected ? GemoraColors.gold : Colors.white60,
                    fontSize: 10,
                    fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
                  );
                }),
              ),
              child: NavigationBar(
                height: 68,
                selectedIndex: selectedIndex,
                onDestinationSelected: onSelected,
                backgroundColor: Colors.transparent,
                elevation: 0,
                indicatorColor: GemoraColors.gold.withValues(alpha: .2),
                labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
                destinations: const [
                  NavigationDestination(
                    icon: Icon(Icons.home_outlined, color: Colors.white60),
                    selectedIcon: Icon(
                      Icons.home_rounded,
                      color: GemoraColors.gold,
                    ),
                    label: 'Home',
                  ),
                  NavigationDestination(
                    icon: Icon(Icons.explore_outlined, color: Colors.white60),
                    selectedIcon: Icon(
                      Icons.explore_rounded,
                      color: GemoraColors.gold,
                    ),
                    label: 'Explore',
                  ),
                  NavigationDestination(
                    icon: Icon(
                      Icons.person_outline_rounded,
                      color: Colors.white60,
                    ),
                    selectedIcon: Icon(
                      Icons.person_rounded,
                      color: GemoraColors.gold,
                    ),
                    label: 'Profile',
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

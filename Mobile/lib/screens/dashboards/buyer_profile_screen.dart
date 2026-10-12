import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/gemora_colors.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/buyer_glass.dart';
import '../export_compliance/my_export_requests_screen.dart';
import '../marketplace/buyer_export_tracking_screen.dart';
import '../marketplace/my_orders_screen.dart';
import '../shipments/seller_shipment_list.dart';

class BuyerProfileScreen extends StatelessWidget {
  const BuyerProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final name = user?.fullName.trim().isNotEmpty == true
        ? user!.fullName
        : 'Gemora Buyer';
    final initial = name.substring(0, 1).toUpperCase();

    return BuyerGradientBackground(
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 20, 16, 42),
        children: [
          BuyerGlassPanel(
            tint: GemoraColors.darkGreen.withValues(alpha: .96),
            child: Column(
              children: [
                Container(
                  width: 82,
                  height: 82,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [GemoraColors.gold, GemoraColors.darkGold],
                    ),
                    borderRadius: BorderRadius.circular(28),
                    border: Border.all(color: Colors.white24),
                  ),
                  child: Text(
                    initial,
                    style: const TextStyle(
                      color: GemoraColors.darkGreen,
                      fontSize: 32,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                const SizedBox(height: 15),
                Text(
                  name,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  user?.email ?? '',
                  style: const TextStyle(color: Colors.white60, fontSize: 12),
                ),
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: GemoraColors.gold.withValues(alpha: .14),
                    borderRadius: BorderRadius.circular(99),
                    border: Border.all(
                      color: GemoraColors.gold.withValues(alpha: .32),
                    ),
                  ),
                  child: const Text(
                    'VERIFIED BUYER',
                    style: TextStyle(
                      color: GemoraColors.gold,
                      fontSize: 9,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.1,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 22),
          const BuyerSectionHeading(
            eyebrow: 'Account centre',
            title: 'Your Gemora profile',
            description: 'Orders and buyer services are organized here to keep the main navigation simple.',
          ),
          const SizedBox(height: 16),
          BuyerGlassPanel(
            padding: EdgeInsets.zero,
            child: Column(
              children: [
                ExpansionTile(
                  key: const ValueKey('buyer-profile-orders'),
                  shape: const Border(),
                  leading: const _MenuIcon(icon: Icons.receipt_long_rounded),
                  title: const Text(
                    'Orders',
                    style: TextStyle(fontWeight: FontWeight.w900),
                  ),
                  subtitle: const Text(
                    'Payments, delivery and export tracking',
                    style: TextStyle(fontSize: 11),
                  ),
                  children: [
                    _ProfileLink(
                      icon: Icons.shopping_bag_outlined,
                      title: 'My Orders',
                      subtitle: 'Review, pay and track every purchase',
                      screen: MyOrdersScreen(),
                    ),
                    _ProfileLink(
                      icon: Icons.flight_takeoff_rounded,
                      title: 'Export Tracking',
                      subtitle: 'Follow international clearance progress',
                      screen: BuyerExportTrackingScreen(),
                    ),
                    _ProfileLink(
                      icon: Icons.local_shipping_outlined,
                      title: 'My Deliveries',
                      subtitle: 'Courier handover and shipment updates',
                      screen: SellerShipmentList(buyerView: true),
                    ),
                  ],
                ),
                Divider(
                  height: 1,
                  color: GemoraColors.darkGreen.withValues(alpha: .08),
                ),
                ExpansionTile(
                  key: const ValueKey('buyer-profile-services'),
                  shape: const Border(),
                  leading: const _MenuIcon(icon: Icons.widgets_outlined),
                  title: const Text(
                    'Services',
                    style: TextStyle(fontWeight: FontWeight.w900),
                  ),
                  subtitle: const Text(
                    'Buyer support and compliance services',
                    style: TextStyle(fontSize: 11),
                  ),
                  children: const [
                    _ProfileLink(
                      icon: Icons.public_rounded,
                      title: 'Export Requests',
                      subtitle: 'Manage gemstone export documentation',
                      screen: MyExportRequestsScreen(),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          BuyerGlassPanel(
            padding: EdgeInsets.zero,
            child: ListTile(
              onTap: () => context.read<AuthProvider>().logout(),
              leading: const Icon(
                Icons.logout_rounded,
                color: Color(0xFF9E3631),
              ),
              title: const Text(
                'Sign out',
                style: TextStyle(
                  color: Color(0xFF9E3631),
                  fontWeight: FontWeight.w800,
                ),
              ),
              subtitle: const Text('Securely end this Gemora session.'),
              trailing: const Icon(Icons.chevron_right_rounded),
            ),
          ),
        ],
      ),
    );
  }
}

class _MenuIcon extends StatelessWidget {
  final IconData icon;

  const _MenuIcon({required this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 44,
      height: 44,
      decoration: BoxDecoration(
        color: GemoraColors.darkGreen,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Icon(icon, color: GemoraColors.gold, size: 21),
    );
  }
}

class _ProfileLink extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final Widget screen;

  const _ProfileLink({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.screen,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: const EdgeInsets.fromLTRB(24, 2, 16, 2),
      leading: Icon(icon, color: GemoraColors.green, size: 21),
      title: Text(
        title,
        style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w800),
      ),
      subtitle: Text(subtitle, style: const TextStyle(fontSize: 10)),
      trailing: const Icon(Icons.arrow_forward_ios_rounded, size: 13),
      onTap: () =>
          Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen)),
    );
  }
}

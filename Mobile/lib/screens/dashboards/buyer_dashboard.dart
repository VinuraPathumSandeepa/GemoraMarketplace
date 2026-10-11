import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../../widgets/notification_bell.dart';
import 'buyer_overview_screen.dart';
import '../marketplace/ai_buyer_assistant_screen.dart';
import '../marketplace/buyer_export_tracking_screen.dart';
import '../marketplace/marketplace_home_screen.dart';
import '../marketplace/my_orders_screen.dart';

class BuyerDashboard extends StatefulWidget {
  const BuyerDashboard({super.key});

  @override
  State<BuyerDashboard> createState() => _BuyerDashboardState();
}

class _BuyerDashboardState extends State<BuyerDashboard> {
  int _index = 0;
  final _pages = const [
    MarketplaceHomeScreen(),
    AiBuyerAssistantScreen(),
    MyOrdersScreen(),
    BuyerOverviewScreen(),
  ];
  final _titles = const [
    'Marketplace',
    'AI Buyer Assistant',
    'My Orders',
    'Buyer Workspace',
  ];

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;

    return Scaffold(
      appBar: _index == 3
          ? null
          : AppBar(
              title: Text(_titles[_index]),
              actions: [
                const NotificationBell(),
                IconButton(
                  tooltip: user?.fullName != null
                      ? 'Track My Exports (${user!.fullName})'
                      : 'Track My Exports',
                  onPressed: () {
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const BuyerExportTrackingScreen(),
                      ),
                    );
                  },
                  icon: const Icon(Icons.flight_takeoff),
                ),
                IconButton(
                  tooltip: 'Logout',
                  onPressed: () => context.read<AuthProvider>().logout(),
                  icon: const Icon(Icons.logout),
                ),
              ],
            ),
      body: IndexedStack(index: _index, children: _pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (value) => setState(() => _index = value),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.storefront_outlined),
            selectedIcon: Icon(Icons.storefront),
            label: 'Market',
          ),
          NavigationDestination(
            icon: Icon(Icons.auto_awesome_outlined),
            selectedIcon: Icon(Icons.auto_awesome),
            label: 'Assistant',
          ),
          NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            selectedIcon: Icon(Icons.receipt_long),
            label: 'Orders',
          ),
          NavigationDestination(
            icon: Icon(Icons.dashboard_outlined),
            selectedIcon: Icon(Icons.dashboard),
            label: 'Services',
          ),
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../../services/export_compliance_service.dart';
import '../marketplace/ai_buyer_assistant_screen.dart';
import '../marketplace/buyer_export_tracking_screen.dart';
import '../marketplace/marketplace_home_screen.dart';
import '../marketplace/my_orders_screen.dart';

class BuyerDashboard extends StatefulWidget {
  final ExportComplianceService? exportComplianceService;

  const BuyerDashboard({
    super.key,
    this.exportComplianceService,
  });

  @override
  State<BuyerDashboard> createState() => _BuyerDashboardState();
}

class _BuyerDashboardState extends State<BuyerDashboard> {
  int _index = 0;
  late final List<Widget> _pages;
  final _titles = const [
    'Marketplace',
    'AI Buyer Assistant',
    'My Orders',
    'Track My Exports',
  ];

  @override
  void initState() {
    super.initState();
    _pages = [
      const MarketplaceHomeScreen(),
      const AiBuyerAssistantScreen(),
      const MyOrdersScreen(),
      BuyerExportTrackingScreen(
        showAppBar: false,
        service: widget.exportComplianceService,
      ),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_titles[_index]),
        actions: [
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
        backgroundColor: Colors.white,
        indicatorColor: const Color(0xFFF1E7D5),
        height: 68,
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
            icon: Icon(Icons.flight_takeoff),
            selectedIcon: Icon(Icons.flight_takeoff),
            label: 'Exports',
          ),
        ],
      ),
    );
  }
}

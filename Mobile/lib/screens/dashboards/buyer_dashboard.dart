import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../marketplace/ai_buyer_assistant_screen.dart';
import '../marketplace/marketplace_home_screen.dart';
import '../marketplace/my_orders_screen.dart';

class BuyerDashboard extends StatefulWidget {
  const BuyerDashboard({super.key});
  @override
  State<BuyerDashboard> createState() => _BuyerDashboardState();
}

class _BuyerDashboardState extends State<BuyerDashboard> {
  int _index = 0;
  final _pages = const [MarketplaceHomeScreen(), AiBuyerAssistantScreen(), MyOrdersScreen()];
  final _titles = const ['Marketplace', 'AI Buyer Assistant', 'My Orders'];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_titles[_index]),
        actions: [IconButton(tooltip: 'Logout', onPressed: () => context.read<AuthProvider>().logout(), icon: const Icon(Icons.logout))],
      ),
      body: IndexedStack(index: _index, children: _pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (value) => setState(() => _index = value),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.storefront_outlined), selectedIcon: Icon(Icons.storefront), label: 'Market'),
          NavigationDestination(icon: Icon(Icons.auto_awesome_outlined), selectedIcon: Icon(Icons.auto_awesome), label: 'Assistant'),
          NavigationDestination(icon: Icon(Icons.receipt_long_outlined), selectedIcon: Icon(Icons.receipt_long), label: 'Orders'),
        ],
      ),
    );
  }
}

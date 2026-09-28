import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../export_compliance/my_export_requests_screen.dart';

class BuyerDashboard extends StatelessWidget {
  const BuyerDashboard({super.key});

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final user = authProvider.user;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Gemora - Buyer'),
        actions: [
          IconButton(
            tooltip: 'Logout',
            onPressed: () async {
              await context.read<AuthProvider>().logout();
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(
                Icons.shopping_bag_outlined,
                size: 72,
              ),
              const SizedBox(height: 20),
              Text(
                'Welcome, ${user?.fullName ?? 'Buyer'}',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 12),
              Text(
                'Email: ${user?.email ?? ''}',
              ),
              const SizedBox(height: 6),
              const Text('Role: Buyer'),
              const SizedBox(height: 30),
              const Text(
                'Buyer Dashboard',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => const MyExportRequestsScreen(),
                    ),
                  );
                },
                icon: const Icon(Icons.flight_takeoff),
                label: const Text('Export Compliance'),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 14,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
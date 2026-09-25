import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import 'admin_dashboard.dart';
import 'buyer_dashboard.dart';
import 'export_officer_dashboard.dart';
import 'gemologist_dashboard.dart';
import 'seller_dashboard.dart';

class RoleDashboard extends StatelessWidget {
  const RoleDashboard({super.key});

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final user = authProvider.user;

    if (user == null) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(),
        ),
      );
    }

    switch (user.role.trim().toLowerCase()) {
      case 'buyer':
        return const BuyerDashboard();

      case 'seller':
        return const SellerDashboard();

      case 'gemologist':
        return const GemologistDashboard();

      case 'exportofficer':
        return const ExportOfficerDashboard();

      case 'admin':
        return const AdminDashboard();

      default:
        return Scaffold(
          appBar: AppBar(
            title: const Text('Gemora'),
          ),
          body: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(
                    Icons.warning_amber_outlined,
                    size: 70,
                  ),
                  const SizedBox(height: 20),
                  const Text(
                    'Unknown User Role',
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 10),
                  Text(
                    'Role: ${user.role}',
                  ),
                  const SizedBox(height: 24),
                  FilledButton.icon(
                    onPressed: () async {
                      await context
                          .read<AuthProvider>()
                          .logout();
                    },
                    icon: const Icon(Icons.logout),
                    label: const Text('Logout'),
                  ),
                ],
              ),
            ),
          ),
        );
    }
  }
}
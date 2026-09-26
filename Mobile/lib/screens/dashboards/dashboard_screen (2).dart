import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';

class DashboardScreen
    extends StatelessWidget {
  const DashboardScreen({
    super.key,
  });

  @override
  Widget build(BuildContext context) {
    final authProvider =
        context.watch<AuthProvider>();

    final user =
        authProvider.user;

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Gemora',
        ),

        actions: [
          IconButton(
            tooltip: 'Logout',
            icon: const Icon(
              Icons.logout,
            ),

            onPressed: () async {
              await context
                  .read<AuthProvider>()
                  .logout();
            },
          ),
        ],
      ),

      body: Center(
        child: Padding(
          padding:
              const EdgeInsets.all(24),

          child: Column(
            mainAxisAlignment:
                MainAxisAlignment.center,

            children: [
              const Icon(
                Icons.diamond_outlined,
                size: 72,
              ),

              const SizedBox(
                height: 20,
              ),

              Text(
                'Welcome, ${user?.fullName ?? ''}',
                style: const TextStyle(
                  fontSize: 24,
                  fontWeight:
                      FontWeight.bold,
                ),
                textAlign:
                    TextAlign.center,
              ),

              const SizedBox(
                height: 12,
              ),

              Text(
                'Email: ${user?.email ?? ''}',
              ),

              const SizedBox(
                height: 8,
              ),

              Text(
                'Role: ${user?.role ?? ''}',
              ),
            ],
          ),
        ),
      ),
    );
  }
}
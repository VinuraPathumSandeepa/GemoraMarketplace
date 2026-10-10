import 'package:flutter/material.dart';

class HomeScreen extends StatelessWidget {
  final String userName;
  final Future<void> Function() onLogout;

  const HomeScreen({super.key, required this.userName, required this.onLogout});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF7F7FA),
      appBar: AppBar(
        title: const Text('Gemora'),
        actions: [
          IconButton(
            tooltip: 'Logout',
            onPressed: () async {
              await onLogout();
            },
            icon: const Icon(Icons.logout_rounded),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            'Welcome, $userName',
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
          ),
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';

import '../auth/login_screen.dart';
import '../auth/register_screen.dart';
import '../marketplace/marketplace_screen.dart';

class PublicHomeScreen extends StatelessWidget {
  const PublicHomeScreen({super.key});

  Future<void> _openLogin(BuildContext context) async {
    await Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => const LoginScreen()));
  }

  Future<void> _openRegister(BuildContext context) async {
    await Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => const RegisterScreen()));
  }

  @override
  Widget build(BuildContext context) {
    return MarketplaceScreen(
      isAuthenticated: false,
      onLogin: () {
        _openLogin(context);
      },
      onRegister: () {
        _openRegister(context);
      },
    );
  }
}

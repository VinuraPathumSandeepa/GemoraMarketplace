import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'constants/gemora_colors.dart';
import 'providers/auth_provider.dart';
import 'providers/notification_provider.dart';
import 'screens/auth/login_screen.dart';
import 'screens/dashboards/role_dashboard.dart';
import 'screens/home/public_home_screen.dart';
import 'screens/onboarding/welcome_screen.dart';
import 'widgets/gemora_mark.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  runApp(
    ChangeNotifierProvider(
      create: (_) => AuthProvider()..initialize(),
      child: const GemoraApp(),
    ),
  );
}

class GemoraApp extends StatelessWidget {
  const GemoraApp({super.key});

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;

    // Dashboard and notification routes are pushed onto MaterialApp's
    // Navigator, so they must share a provider above that Navigator.
    return ChangeNotifierProvider(
      key: ValueKey(user?.id ?? user?.email ?? 'guest'),
      create: (_) => NotificationProvider(),
      child: MaterialApp(
        title: 'Gemora Marketplace',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: GemoraColors.green),
          scaffoldBackgroundColor: GemoraColors.cream,
          useMaterial3: true,
        ),
        home: const AuthGate(),
      ),
    );
  }
}

class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  static const _welcomeKey = 'gemora_welcome_completed_v1';
  bool? _welcomeCompleted;

  @override
  void initState() {
    super.initState();
    _loadWelcomePreference();
  }

  Future<void> _loadWelcomePreference() async {
    try {
      final preferences = await SharedPreferences.getInstance();
      if (!mounted) return;
      setState(() {
        _welcomeCompleted = preferences.getBool(_welcomeKey) ?? false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _welcomeCompleted = false);
    }
  }

  Future<void> _completeWelcome(
    WelcomeAction action,
    bool isAuthenticated,
  ) async {
    try {
      final preferences = await SharedPreferences.getInstance();
      await preferences.setBool(_welcomeKey, true);
    } catch (_) {
      // Let the user proceed even if local preferences are unavailable.
    }
    if (!mounted) return;
    setState(() => _welcomeCompleted = true);

    if (action == WelcomeAction.signIn && !isAuthenticated) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted) return;
        Navigator.of(context)
            .push(MaterialPageRoute<void>(builder: (_) => const LoginScreen()));
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();

    if (authProvider.isLoading || _welcomeCompleted == null) {
      return const _StartupScreen();
    }

    if (!_welcomeCompleted!) {
      return WelcomeScreen(
        canSignIn: !authProvider.isAuthenticated,
        onFinished: (action) =>
            _completeWelcome(action, authProvider.isAuthenticated),
      );
    }

    if (!authProvider.isAuthenticated) {
      return const PublicHomeScreen();
    }

    return const RoleDashboard();
  }
}

class _StartupScreen extends StatelessWidget {
  const _StartupScreen();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: GemoraColors.darkGreen,
      body: SafeArea(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              GemoraMark(size: 80),
              SizedBox(height: 18),
              Text(
                'GEMORA',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 25,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2.2,
                ),
              ),
              SizedBox(height: 6),
              Text(
                'Gemstone Marketplace',
                style: TextStyle(color: Colors.white70, fontSize: 12),
              ),
              SizedBox(height: 26),
              SizedBox(
                width: 26,
                height: 26,
                child: CircularProgressIndicator(
                  strokeWidth: 2.5,
                  color: Colors.white,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

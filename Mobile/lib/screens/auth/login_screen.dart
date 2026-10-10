import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import 'register_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  static const Color _darkGreen = Color(0xFF08251E);

  static const Color _green = Color(0xFF16483B);

  static const Color _gold = Color(0xFFE4BC74);

  static const Color _darkGold = Color(0xFFC99242);

  static const Color _cream = Color(0xFFFAF7F0);

  static const Color _white = Color(0xFFFFFFFF);

  static const Color _mutedText = Color(0xFF697771);

  static const Color _border = Color(0xFFE7DFD2);

  final _formKey = GlobalKey<FormState>();

  final _emailController = TextEditingController();

  final _passwordController = TextEditingController();

  bool _hidePassword = true;

  // ==========================================
  // LOGIN
  // ==========================================

  Future<void> _login() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final authProvider = context.read<AuthProvider>();

    final success = await authProvider.login(
      _emailController.text,
      _passwordController.text,
    );

    if (!mounted) {
      return;
    }

    if (!success) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: Colors.red.shade700,
          content: Text(authProvider.errorMessage ?? 'Login failed.'),
        ),
      );

      return;
    }

    // Login succeeded.
    // Return to the root route.
    // The root app listens to AuthProvider and
    // shows the authenticated Gemora home.

    Navigator.of(context).popUntil((route) => route.isFirst);
  }

  // ==========================================
  // DISPOSE
  // ==========================================

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();

    super.dispose();
  }

  // ==========================================
  // INPUT DECORATION
  // ==========================================

  InputDecoration _inputDecoration({
    required String hintText,
    required IconData prefixIcon,
    Widget? suffixIcon,
  }) {
    return InputDecoration(
      hintText: hintText,
      hintStyle: const TextStyle(color: _mutedText, fontSize: 14),
      prefixIcon: Icon(prefixIcon, color: _green, size: 21),
      suffixIcon: suffixIcon,
      filled: true,
      fillColor: _white,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 17),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: _border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: _border),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: _darkGold, width: 1.5),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: Colors.red),
      ),
      focusedErrorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: Colors.red, width: 1.5),
      ),
    );
  }

  Widget _fieldLabel(String label) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(
        label.toUpperCase(),
        style: const TextStyle(
          color: _darkGreen,
          fontSize: 12,
          fontWeight: FontWeight.w800,
          letterSpacing: 0.9,
        ),
      ),
    );
  }

  // ==========================================
  // UI
  // ==========================================

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();

    return Scaffold(
      backgroundColor: _darkGreen,

      appBar: AppBar(
        backgroundColor: _darkGreen,
        foregroundColor: _white,
        elevation: 0,
        centerTitle: true,
        title: const Text(
          'Sign In',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
      ),

      body: SafeArea(
        bottom: false,

        child: Container(
          width: double.infinity,

          decoration: const BoxDecoration(
            color: _cream,

            borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
          ),

          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(22, 30, 22, 36),

            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 500),

                child: Form(
                  key: _formKey,

                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,

                    children: [
                      // ==================================
                      // GEMORA BRAND
                      // ==================================

                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,

                        children: [
                          Container(
                            width: 50,
                            height: 50,

                            decoration: BoxDecoration(
                              color: _green,

                              borderRadius: BorderRadius.circular(13),
                            ),

                            child: const Icon(
                              Icons.diamond_outlined,
                              color: _gold,
                              size: 29,
                            ),
                          ),

                          const SizedBox(width: 12),

                          const Column(
                            crossAxisAlignment: CrossAxisAlignment.start,

                            children: [
                              Text(
                                'GEMORA',

                                style: TextStyle(
                                  color: _darkGreen,

                                  fontSize: 22,

                                  fontWeight: FontWeight.w900,

                                  letterSpacing: 2,
                                ),
                              ),

                              Text(
                                'Gemstone Marketplace',

                                style: TextStyle(
                                  color: _mutedText,

                                  fontSize: 10,

                                  letterSpacing: 0.5,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),

                      const SizedBox(height: 26),

                      Container(height: 1, color: _border),

                      const SizedBox(height: 30),

                      // ==================================
                      // TITLE
                      // ==================================
                      const Text(
                        'WELCOME BACK',

                        textAlign: TextAlign.center,

                        style: TextStyle(
                          color: _darkGold,

                          fontSize: 12,

                          fontWeight: FontWeight.w800,

                          letterSpacing: 1.7,
                        ),
                      ),

                      const SizedBox(height: 8),

                      const Text(
                        'Sign In to Gemora',

                        textAlign: TextAlign.center,

                        style: TextStyle(
                          color: _darkGreen,

                          fontSize: 30,

                          fontWeight: FontWeight.w900,
                        ),
                      ),

                      const SizedBox(height: 10),

                      const Text(
                        'Access your trusted Sri Lankan gemstone marketplace.',

                        textAlign: TextAlign.center,

                        style: TextStyle(
                          color: _mutedText,

                          fontSize: 14,

                          height: 1.5,
                        ),
                      ),

                      const SizedBox(height: 34),

                      // ==================================
                      // EMAIL
                      // ==================================
                      _fieldLabel('Email'),

                      TextFormField(
                        controller: _emailController,

                        keyboardType: TextInputType.emailAddress,

                        textInputAction: TextInputAction.next,

                        autocorrect: false,

                        decoration: _inputDecoration(
                          hintText: 'you@example.com',

                          prefixIcon: Icons.email_outlined,
                        ),

                        validator: (value) {
                          final email = value?.trim() ?? '';

                          if (email.isEmpty) {
                            return 'Email is required.';
                          }

                          final emailPattern = RegExp(
                            r'^[^@\s]+@[^@\s]+\.[^@\s]+$',
                          );

                          if (!emailPattern.hasMatch(email)) {
                            return 'Enter a valid email address.';
                          }

                          return null;
                        },
                      ),

                      const SizedBox(height: 20),

                      // ==================================
                      // PASSWORD
                      // ==================================
                      _fieldLabel('Password'),

                      TextFormField(
                        controller: _passwordController,

                        obscureText: _hidePassword,

                        textInputAction: TextInputAction.done,

                        decoration: _inputDecoration(
                          hintText: 'Enter your password',

                          prefixIcon: Icons.lock_outline,

                          suffixIcon: IconButton(
                            onPressed: () {
                              setState(() {
                                _hidePassword = !_hidePassword;
                              });
                            },

                            icon: Icon(
                              _hidePassword
                                  ? Icons.visibility_outlined
                                  : Icons.visibility_off_outlined,

                              color: _mutedText,
                            ),
                          ),
                        ),

                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Password is required.';
                          }

                          return null;
                        },

                        onFieldSubmitted: (_) {
                          if (!authProvider.isLoading) {
                            _login();
                          }
                        },
                      ),

                      const SizedBox(height: 28),

                      // ==================================
                      // SIGN IN BUTTON
                      // ==================================
                      SizedBox(
                        height: 56,

                        child: FilledButton(
                          onPressed: authProvider.isLoading ? null : _login,

                          style: FilledButton.styleFrom(
                            backgroundColor: _gold,

                            foregroundColor: _darkGreen,

                            disabledBackgroundColor: _gold.withValues(
                              alpha: 0.6,
                            ),

                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(13),
                            ),
                          ),

                          child: authProvider.isLoading
                              ? const SizedBox(
                                  width: 22,

                                  height: 22,

                                  child: CircularProgressIndicator(
                                    strokeWidth: 2.3,

                                    color: _darkGreen,
                                  ),
                                )
                              : const Row(
                                  mainAxisAlignment: MainAxisAlignment.center,

                                  children: [
                                    Icon(Icons.login_rounded, size: 19),

                                    SizedBox(width: 8),

                                    Text(
                                      'Sign In',

                                      style: TextStyle(
                                        fontSize: 15,

                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                  ],
                                ),
                        ),
                      ),

                      const SizedBox(height: 26),

                      Container(height: 1, color: _border),

                      const SizedBox(height: 18),

                      // ==================================
                      // REGISTER
                      // ==================================
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,

                        children: [
                          const Flexible(
                            child: Text(
                              'New to Gemora?',

                              style: TextStyle(color: _mutedText, fontSize: 13),
                            ),
                          ),

                          TextButton(
                            onPressed: authProvider.isLoading
                                ? null
                                : () {
                                    Navigator.push(
                                      context,

                                      MaterialPageRoute(
                                        builder: (_) => const RegisterScreen(),
                                      ),
                                    );
                                  },

                            style: TextButton.styleFrom(
                              foregroundColor: _darkGold,
                            ),

                            child: const Text(
                              'Create Account',

                              style: TextStyle(fontWeight: FontWeight.w800),
                            ),
                          ),
                        ],
                      ),

                      const SizedBox(height: 16),

                      // ==================================
                      // SECURITY INFO
                      // ==================================
                      Container(
                        padding: const EdgeInsets.all(16),

                        decoration: BoxDecoration(
                          color: _green.withValues(alpha: 0.06),

                          borderRadius: BorderRadius.circular(13),

                          border: Border.all(
                            color: _green.withValues(alpha: 0.12),
                          ),
                        ),

                        child: const Row(
                          children: [
                            Icon(
                              Icons.verified_user_outlined,

                              color: _green,

                              size: 22,
                            ),

                            SizedBox(width: 12),

                            Expanded(
                              child: Text(
                                'Your account is protected with secure authentication and verified email access.',

                                style: TextStyle(
                                  color: _mutedText,

                                  fontSize: 12,

                                  height: 1.4,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

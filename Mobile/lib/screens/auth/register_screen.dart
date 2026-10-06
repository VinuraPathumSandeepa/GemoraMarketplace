import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  static const Color _darkGreen = Color(0xFF08251E);

  static const Color _green = Color(0xFF16483B);

  static const Color _gold = Color(0xFFE4BC74);

  static const Color _darkGold = Color(0xFFC99242);

  static const Color _cream = Color(0xFFFAF7F0);

  static const Color _white = Color(0xFFFFFFFF);

  static const Color _darkText = Color(0xFF10241F);

  static const Color _mutedText = Color(0xFF697771);

  static const Color _border = Color(0xFFE7DFD2);

  final _formKey = GlobalKey<FormState>();

  final _fullNameController = TextEditingController();

  final _emailController = TextEditingController();

  final _phoneController = TextEditingController();

  final _regionController = TextEditingController();

  final _passwordController = TextEditingController();

  final _confirmPasswordController = TextEditingController();

  String _selectedRole = 'Buyer';

  bool _hidePassword = true;

  bool _hideConfirmPassword = true;

  final List<_CountryOption> _countries = const [
    _CountryOption(name: 'Sri Lanka', isoCode: 'LK', dialCode: '+94'),
    _CountryOption(name: 'United States', isoCode: 'US', dialCode: '+1'),
    _CountryOption(name: 'United Kingdom', isoCode: 'GB', dialCode: '+44'),
    _CountryOption(name: 'Australia', isoCode: 'AU', dialCode: '+61'),
    _CountryOption(name: 'India', isoCode: 'IN', dialCode: '+91'),
    _CountryOption(name: 'Singapore', isoCode: 'SG', dialCode: '+65'),
    _CountryOption(
      name: 'United Arab Emirates',
      isoCode: 'AE',
      dialCode: '+971',
    ),
  ];

  late _CountryOption _selectedCountry;

  @override
  void initState() {
    super.initState();

    _selectedCountry = _countries.first;
  }

  // ==========================================
  // PHONE NUMBER
  // ==========================================

  String _buildPhoneNumber([String? input]) {
    final raw = (input ?? _phoneController.text).trim();

    if (raw.isEmpty) {
      return '';
    }

    final digits = raw.replaceAll(RegExp(r'[^0-9]'), '');

    if (raw.startsWith('+')) {
      return '+$digits';
    }

    final localNumber = digits.replaceFirst(RegExp(r'^0+'), '');

    return '${_selectedCountry.dialCode}$localNumber';
  }

  // ==========================================
  // REGISTER
  // ==========================================

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final authProvider = context.read<AuthProvider>();

    final success = await authProvider.register(
      fullName: _fullNameController.text,
      email: _emailController.text,
      phoneNumber: _buildPhoneNumber(),
      countryCode: _selectedCountry.isoCode,
      region: _regionController.text,
      password: _passwordController.text,
      role: _selectedRole,
    );

    if (!mounted) {
      return;
    }

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: _green,
          content: Text(
            'Registration successful. Please verify your email, then sign in.',
          ),
        ),
      );

      Navigator.pop(context);

      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        backgroundColor: Colors.red.shade700,
        content: Text(authProvider.errorMessage ?? 'Registration failed.'),
      ),
    );
  }

  // ==========================================
  // DISPOSE
  // ==========================================

  @override
  void dispose() {
    _fullNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _regionController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();

    super.dispose();
  }

  // ==========================================
  // INPUT DECORATION
  // ==========================================

  InputDecoration _inputDecoration({
    required String hintText,
    IconData? prefixIcon,
    Widget? suffixIcon,
    String? prefixText,
  }) {
    return InputDecoration(
      hintText: hintText,
      hintStyle: const TextStyle(color: _mutedText, fontSize: 14),
      prefixText: prefixText,
      prefixStyle: const TextStyle(
        color: _darkText,
        fontWeight: FontWeight.w700,
      ),
      prefixIcon: prefixIcon == null
          ? null
          : Icon(prefixIcon, color: _green, size: 21),
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

  Widget _fieldSpacing() {
    return const SizedBox(height: 18);
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
        title: const Text(
          'Create Account',
          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
        ),
        centerTitle: true,
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
            padding: const EdgeInsets.fromLTRB(22, 28, 22, 36),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 520),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // --------------------------
                      // BRAND
                      // --------------------------

                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Container(
                            width: 48,
                            height: 48,
                            decoration: BoxDecoration(
                              color: _green,
                              borderRadius: BorderRadius.circular(13),
                            ),
                            child: const Icon(
                              Icons.diamond_outlined,
                              color: _gold,
                              size: 28,
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
                                  letterSpacing: 0.6,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),

                      const SizedBox(height: 26),

                      Container(height: 1, color: _border),

                      const SizedBox(height: 26),

                      const Text(
                        'CREATE YOUR ACCOUNT',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: _darkGold,
                          fontWeight: FontWeight.w800,
                          fontSize: 12,
                          letterSpacing: 1.7,
                        ),
                      ),

                      const SizedBox(height: 8),

                      const Text(
                        'Join Gemora',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: _darkGreen,
                          fontSize: 30,
                          fontWeight: FontWeight.w900,
                        ),
                      ),

                      const SizedBox(height: 10),

                      const Text(
                        'Create a Buyer or Seller account and verify your email to continue.',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: _mutedText,
                          height: 1.5,
                          fontSize: 14,
                        ),
                      ),

                      const SizedBox(height: 32),

                      // --------------------------
                      // FULL NAME
                      // --------------------------
                      _fieldLabel('Full Name'),

                      TextFormField(
                        controller: _fullNameController,
                        textInputAction: TextInputAction.next,
                        textCapitalization: TextCapitalization.words,
                        decoration: _inputDecoration(
                          hintText: 'Enter your full name',
                          prefixIcon: Icons.person_outline,
                        ),
                        validator: (value) {
                          final name = value?.trim() ?? '';

                          if (name.isEmpty) {
                            return 'Full name is required.';
                          }

                          if (name.length < 2) {
                            return 'Full name must contain at least 2 characters.';
                          }

                          if (name.length > 100) {
                            return 'Full name cannot exceed 100 characters.';
                          }

                          return null;
                        },
                      ),

                      _fieldSpacing(),

                      // --------------------------
                      // EMAIL
                      // --------------------------
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

                      _fieldSpacing(),

                      // --------------------------
                      // COUNTRY
                      // --------------------------
                      _fieldLabel('Country'),

                      DropdownButtonFormField<_CountryOption>(
                        key: ValueKey(_selectedCountry.isoCode),
                        initialValue: _selectedCountry,
                        isExpanded: true,
                        decoration: _inputDecoration(
                          hintText: 'Select your country',
                          prefixIcon: Icons.public_outlined,
                        ),
                        items: _countries.map((country) {
                          return DropdownMenuItem<_CountryOption>(
                            value: country,
                            child: Text(
                              '${country.name} (${country.isoCode})',
                              overflow: TextOverflow.ellipsis,
                            ),
                          );
                        }).toList(),
                        onChanged: (value) {
                          if (value == null) {
                            return;
                          }

                          setState(() {
                            _selectedCountry = value;
                          });
                        },
                      ),

                      _fieldSpacing(),

                      // --------------------------
                      // MOBILE NUMBER
                      // --------------------------
                      _fieldLabel('Mobile Number'),

                      TextFormField(
                        controller: _phoneController,
                        keyboardType: TextInputType.phone,
                        textInputAction: TextInputAction.next,
                        decoration: _inputDecoration(
                          hintText: '77 123 4567',
                          prefixText: '${_selectedCountry.dialCode} ',
                        ),
                        validator: (value) {
                          final raw = value?.trim() ?? '';

                          if (raw.isEmpty) {
                            return 'Phone number is required.';
                          }

                          final phone = _buildPhoneNumber(raw);

                          if (!RegExp(r'^\+[1-9]\d{6,14}$').hasMatch(phone)) {
                            return 'Enter a valid mobile number.';
                          }

                          return null;
                        },
                      ),

                      const SizedBox(height: 6),

                      Text(
                        'International number: ${_selectedCountry.dialCode} + your mobile number',
                        style: const TextStyle(color: _mutedText, fontSize: 11),
                      ),

                      _fieldSpacing(),

                      // --------------------------
                      // REGION
                      // --------------------------
                      _fieldLabel('Province / State / Region'),

                      TextFormField(
                        controller: _regionController,
                        textInputAction: TextInputAction.next,
                        textCapitalization: TextCapitalization.words,
                        decoration: _inputDecoration(
                          hintText: 'Example: Western Province',
                          prefixIcon: Icons.location_on_outlined,
                        ),
                        validator: (value) {
                          final region = value?.trim() ?? '';

                          if (region.isEmpty) {
                            return 'Region is required.';
                          }

                          if (region.length < 2) {
                            return 'Region must contain at least 2 characters.';
                          }

                          if (region.length > 100) {
                            return 'Region cannot exceed 100 characters.';
                          }

                          return null;
                        },
                      ),

                      _fieldSpacing(),

                      // --------------------------
                      // PASSWORD
                      // --------------------------
                      _fieldLabel('Password'),

                      TextFormField(
                        controller: _passwordController,
                        obscureText: _hidePassword,
                        textInputAction: TextInputAction.next,
                        decoration: _inputDecoration(
                          hintText: 'Minimum 6 characters',
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

                          if (value.length < 6) {
                            return 'Password must contain at least 6 characters.';
                          }

                          if (value.length > 100) {
                            return 'Password cannot exceed 100 characters.';
                          }

                          return null;
                        },
                      ),

                      _fieldSpacing(),

                      // --------------------------
                      // CONFIRM PASSWORD
                      // --------------------------
                      _fieldLabel('Confirm Password'),

                      TextFormField(
                        controller: _confirmPasswordController,
                        obscureText: _hideConfirmPassword,
                        textInputAction: TextInputAction.next,
                        decoration: _inputDecoration(
                          hintText: 'Re-enter your password',
                          prefixIcon: Icons.lock_outline,
                          suffixIcon: IconButton(
                            onPressed: () {
                              setState(() {
                                _hideConfirmPassword = !_hideConfirmPassword;
                              });
                            },
                            icon: Icon(
                              _hideConfirmPassword
                                  ? Icons.visibility_outlined
                                  : Icons.visibility_off_outlined,
                              color: _mutedText,
                            ),
                          ),
                        ),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Confirm your password.';
                          }

                          if (value != _passwordController.text) {
                            return 'Passwords do not match.';
                          }

                          return null;
                        },
                      ),

                      _fieldSpacing(),

                      // --------------------------
                      // ACCOUNT TYPE
                      // --------------------------
                      _fieldLabel('Account Type'),

                      DropdownButtonFormField<String>(
                        initialValue: _selectedRole,
                        decoration: _inputDecoration(
                          hintText: 'Select account type',
                          prefixIcon: Icons.badge_outlined,
                        ),
                        items: const [
                          DropdownMenuItem(
                            value: 'Buyer',
                            child: Text('Buyer'),
                          ),
                          DropdownMenuItem(
                            value: 'Seller',
                            child: Text('Seller'),
                          ),
                        ],
                        onChanged: (value) {
                          if (value == null) {
                            return;
                          }

                          setState(() {
                            _selectedRole = value;
                          });
                        },
                      ),

                      const SizedBox(height: 28),

                      // --------------------------
                      // REGISTER BUTTON
                      // --------------------------
                      SizedBox(
                        height: 56,
                        child: FilledButton(
                          onPressed: authProvider.isLoading ? null : _register,
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
                                    Text(
                                      'Create Account',
                                      style: TextStyle(
                                        fontWeight: FontWeight.w800,
                                        fontSize: 15,
                                      ),
                                    ),
                                    SizedBox(width: 7),
                                    Icon(Icons.arrow_forward, size: 18),
                                  ],
                                ),
                        ),
                      ),

                      const SizedBox(height: 24),

                      Container(height: 1, color: _border),

                      const SizedBox(height: 18),

                      // --------------------------
                      // SIGN IN
                      // --------------------------
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Flexible(
                            child: Text(
                              'Already have an account?',
                              style: TextStyle(color: _mutedText, fontSize: 13),
                            ),
                          ),
                          TextButton(
                            onPressed: authProvider.isLoading
                                ? null
                                : () {
                                    Navigator.pop(context);
                                  },
                            style: TextButton.styleFrom(
                              foregroundColor: _darkGold,
                            ),
                            child: const Text(
                              'Sign In',
                              style: TextStyle(fontWeight: FontWeight.w800),
                            ),
                          ),
                        ],
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

class _CountryOption {
  final String name;
  final String isoCode;
  final String dialCode;

  const _CountryOption({
    required this.name,
    required this.isoCode,
    required this.dialCode,
  });
}

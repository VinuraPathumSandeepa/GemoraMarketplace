import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import 'login_screen.dart';

class VerifyEmailScreen extends StatefulWidget {
  final String email;

  const VerifyEmailScreen({super.key, required this.email});

  @override
  State<VerifyEmailScreen> createState() => _VerifyEmailScreenState();
}

class _VerifyEmailScreenState extends State<VerifyEmailScreen> {
  static const Color _darkGreen = Color(0xFF08251E);
  static const Color _green = Color(0xFF16483B);
  static const Color _gold = Color(0xFFE4BC74);
  static const Color _darkGold = Color(0xFFC99242);
  static const Color _cream = Color(0xFFFAF7F0);
  static const Color _white = Color(0xFFFFFFFF);
  static const Color _mutedText = Color(0xFF697771);
  static const Color _border = Color(0xFFE7DFD2);

  final _formKey = GlobalKey<FormState>();

  final _codeController = TextEditingController();

  Future<void> _verifyEmail() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final authProvider = context.read<AuthProvider>();

    final success = await authProvider.verifyEmail(
      email: widget.email,
      code: _codeController.text,
    );

    if (!mounted) {
      return;
    }

    if (!success) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: Colors.red.shade700,
          content: Text(
            authProvider.errorMessage ?? 'Email verification failed.',
          ),
        ),
      );

      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        backgroundColor: _green,
        content: Text('Email verified successfully. Please sign in.'),
      ),
    );

    Navigator.of(
      context,
    ).pushReplacement(MaterialPageRoute(builder: (_) => const LoginScreen()));
  }

  Future<void> _resendCode() async {
    final authProvider = context.read<AuthProvider>();

    final success = await authProvider.resendVerificationCode(
      email: widget.email,
    );

    if (!mounted) {
      return;
    }

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          backgroundColor: _green,
          content: Text('A new verification code has been sent.'),
        ),
      );

      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        backgroundColor: Colors.red.shade700,
        content: Text(
          authProvider.errorMessage ??
              'Unable to resend the verification code.',
        ),
      ),
    );
  }

  @override
  void dispose() {
    _codeController.dispose();

    super.dispose();
  }

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
          'Verify Email',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
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
            padding: const EdgeInsets.fromLTRB(24, 36, 24, 36),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 500),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Container(
                            width: 52,
                            height: 52,
                            decoration: BoxDecoration(
                              color: _green,
                              borderRadius: BorderRadius.circular(14),
                            ),
                            child: const Icon(
                              Icons.diamond_outlined,
                              color: _gold,
                              size: 30,
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
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),

                      const SizedBox(height: 28),

                      Container(height: 1, color: _border),

                      const SizedBox(height: 32),

                      Container(
                        width: 82,
                        height: 82,
                        decoration: BoxDecoration(
                          color: _green.withValues(alpha: 0.08),
                          shape: BoxShape.circle,
                          border: Border.all(color: _gold),
                        ),
                        child: const Icon(
                          Icons.mark_email_read_outlined,
                          color: _green,
                          size: 38,
                        ),
                      ),

                      const SizedBox(height: 22),

                      const Text(
                        'VERIFY YOUR EMAIL',
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
                        'Enter Verification Code',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: _darkGreen,
                          fontSize: 27,
                          fontWeight: FontWeight.w900,
                        ),
                      ),

                      const SizedBox(height: 12),

                      const Text(
                        'We sent a 6-digit verification code to',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: _mutedText, height: 1.5),
                      ),

                      const SizedBox(height: 4),

                      Text(
                        widget.email,
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          color: _darkGreen,
                          fontWeight: FontWeight.w800,
                        ),
                      ),

                      const SizedBox(height: 34),

                      const Text(
                        'VERIFICATION CODE',
                        style: TextStyle(
                          color: _darkGreen,
                          fontSize: 12,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 0.9,
                        ),
                      ),

                      const SizedBox(height: 9),

                      TextFormField(
                        controller: _codeController,
                        autofocus: true,
                        keyboardType: TextInputType.number,
                        textInputAction: TextInputAction.done,
                        autofillHints: const [AutofillHints.oneTimeCode],
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          color: _darkGreen,
                          fontSize: 25,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 9,
                        ),
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                          LengthLimitingTextInputFormatter(6),
                        ],
                        decoration: InputDecoration(
                          hintText: '000000',
                          counterText: '',
                          hintStyle: const TextStyle(
                            color: _mutedText,
                            letterSpacing: 9,
                          ),
                          filled: true,
                          fillColor: _white,
                          contentPadding: const EdgeInsets.symmetric(
                            vertical: 20,
                          ),
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(13),
                            borderSide: const BorderSide(color: _border),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(13),
                            borderSide: const BorderSide(color: _border),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(13),
                            borderSide: const BorderSide(
                              color: _darkGold,
                              width: 1.5,
                            ),
                          ),
                        ),
                        validator: (value) {
                          final code = value?.trim() ?? '';

                          if (code.isEmpty) {
                            return 'Verification code is required.';
                          }

                          if (!RegExp(r'^\d{6}$').hasMatch(code)) {
                            return 'Enter the 6-digit verification code.';
                          }

                          return null;
                        },
                        onFieldSubmitted: (_) {
                          if (!authProvider.isLoading) {
                            _verifyEmail();
                          }
                        },
                      ),

                      const SizedBox(height: 26),

                      SizedBox(
                        height: 56,
                        child: FilledButton(
                          onPressed: authProvider.isLoading
                              ? null
                              : _verifyEmail,
                          style: FilledButton.styleFrom(
                            backgroundColor: _gold,
                            foregroundColor: _darkGreen,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(13),
                            ),
                          ),
                          child: authProvider.isLoading
                              ? const SizedBox(
                                  width: 22,
                                  height: 22,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2.2,
                                    color: _darkGreen,
                                  ),
                                )
                              : const Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.verified_outlined, size: 19),
                                    SizedBox(width: 8),
                                    Text(
                                      'Verify Email',
                                      style: TextStyle(
                                        fontSize: 15,
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                  ],
                                ),
                        ),
                      ),

                      const SizedBox(height: 24),

                      Container(height: 1, color: _border),

                      const SizedBox(height: 18),

                      const Text(
                        'Didn\'t receive the code?',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: _mutedText, fontSize: 13),
                      ),

                      const SizedBox(height: 4),

                      TextButton.icon(
                        onPressed: authProvider.isLoading ? null : _resendCode,
                        icon: const Icon(Icons.refresh_rounded, size: 18),
                        label: const Text(
                          'Resend Code',
                          style: TextStyle(fontWeight: FontWeight.w800),
                        ),
                        style: TextButton.styleFrom(foregroundColor: _darkGold),
                      ),

                      const SizedBox(height: 8),

                      const Text(
                        'For your security, verification codes may expire after a short period.',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: _mutedText,
                          fontSize: 11,
                          height: 1.5,
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

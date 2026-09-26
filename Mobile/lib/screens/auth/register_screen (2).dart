import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({
    super.key,
  });

  @override
  State<RegisterScreen> createState() =>
      _RegisterScreenState();
}

class _RegisterScreenState
    extends State<RegisterScreen> {
  final _formKey =
      GlobalKey<FormState>();

  final _fullNameController =
      TextEditingController();

  final _emailController =
      TextEditingController();

  final _passwordController =
      TextEditingController();

  final _confirmPasswordController =
      TextEditingController();

  String _selectedRole = 'Buyer';

  bool _hidePassword = true;
  bool _hideConfirmPassword = true;

  // ==========================================
  // REGISTER
  // ==========================================

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final authProvider =
        context.read<AuthProvider>();

    final success =
        await authProvider.register(
      fullName:
          _fullNameController.text,
      email:
          _emailController.text,
      password:
          _passwordController.text,
      role:
          _selectedRole,
    );

    if (!mounted) {
      return;
    }

    if (success) {
      ScaffoldMessenger.of(context)
          .showSnackBar(
        const SnackBar(
          content: Text(
            'Registration successful. Please sign in.',
          ),
        ),
      );

      Navigator.pop(context);

      return;
    }

    ScaffoldMessenger.of(context)
        .showSnackBar(
      SnackBar(
        content: Text(
          authProvider.errorMessage ??
              'Registration failed.',
        ),
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
    _passwordController.dispose();
    _confirmPasswordController.dispose();

    super.dispose();
  }

  // ==========================================
  // UI
  // ==========================================

  @override
  Widget build(BuildContext context) {
    final authProvider =
        context.watch<AuthProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Create Account',
        ),
        centerTitle: true,
      ),

      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding:
                const EdgeInsets.all(24),

            child: ConstrainedBox(
              constraints:
                  const BoxConstraints(
                maxWidth: 450,
              ),

              child: Form(
                key: _formKey,

                child: Column(
                  crossAxisAlignment:
                      CrossAxisAlignment
                          .stretch,

                  children: [
                    const Icon(
                      Icons.diamond_outlined,
                      size: 70,
                    ),

                    const SizedBox(
                      height: 16,
                    ),

                    const Text(
                      'Gemora Marketplace',
                      textAlign:
                          TextAlign.center,
                      style: TextStyle(
                        fontSize: 28,
                        fontWeight:
                            FontWeight.bold,
                      ),
                    ),

                    const SizedBox(
                      height: 8,
                    ),

                    const Text(
                      'Create your account',
                      textAlign:
                          TextAlign.center,
                    ),

                    const SizedBox(
                      height: 30,
                    ),

                    // FULL NAME

                    TextFormField(
                      controller:
                          _fullNameController,

                      textInputAction:
                          TextInputAction.next,

                      decoration:
                          const InputDecoration(
                        labelText:
                            'Full Name',
                        border:
                            OutlineInputBorder(),
                        prefixIcon:
                            Icon(
                          Icons.person_outline,
                        ),
                      ),

                      validator: (value) {
                        if (value == null ||
                            value
                                .trim()
                                .isEmpty) {
                          return 'Full name is required.';
                        }

                        if (value
                                .trim()
                                .length <
                            2) {
                          return 'Full name must contain at least 2 characters.';
                        }

                        return null;
                      },
                    ),

                    const SizedBox(
                      height: 16,
                    ),

                    // EMAIL

                    TextFormField(
                      controller:
                          _emailController,

                      keyboardType:
                          TextInputType
                              .emailAddress,

                      textInputAction:
                          TextInputAction.next,

                      decoration:
                          const InputDecoration(
                        labelText: 'Email',
                        border:
                            OutlineInputBorder(),
                        prefixIcon:
                            Icon(
                          Icons.email_outlined,
                        ),
                      ),

                      validator: (value) {
                        if (value == null ||
                            value
                                .trim()
                                .isEmpty) {
                          return 'Email is required.';
                        }

                        final email =
                            value.trim();

                        if (!email
                                .contains('@') ||
                            !email
                                .contains('.')) {
                          return 'Enter a valid email address.';
                        }

                        return null;
                      },
                    ),

                    const SizedBox(
                      height: 16,
                    ),

                    // ROLE

                    DropdownButtonFormField<
                        String>(
                      initialValue:
                          _selectedRole,

                      decoration:
                          const InputDecoration(
                        labelText:
                            'Account Type',
                        border:
                            OutlineInputBorder(),
                        prefixIcon:
                            Icon(
                          Icons.badge_outlined,
                        ),
                      ),

                      items: const [
                        DropdownMenuItem(
                          value: 'Buyer',
                          child:
                              Text('Buyer'),
                        ),

                        DropdownMenuItem(
                          value: 'Seller',
                          child:
                              Text('Seller'),
                        ),
                      ],

                      onChanged: (value) {
                        if (value == null) {
                          return;
                        }

                        setState(() {
                          _selectedRole =
                              value;
                        });
                      },
                    ),

                    const SizedBox(
                      height: 16,
                    ),

                    // PASSWORD

                    TextFormField(
                      controller:
                          _passwordController,

                      obscureText:
                          _hidePassword,

                      textInputAction:
                          TextInputAction.next,

                      decoration:
                          InputDecoration(
                        labelText:
                            'Password',
                        border:
                            const OutlineInputBorder(),
                        prefixIcon:
                            const Icon(
                          Icons.lock_outline,
                        ),

                        suffixIcon:
                            IconButton(
                          onPressed: () {
                            setState(() {
                              _hidePassword =
                                  !_hidePassword;
                            });
                          },

                          icon: Icon(
                            _hidePassword
                                ? Icons
                                    .visibility_outlined
                                : Icons
                                    .visibility_off_outlined,
                          ),
                        ),
                      ),

                      validator: (value) {
                        if (value == null ||
                            value.isEmpty) {
                          return 'Password is required.';
                        }

                        if (value.length < 6) {
                          return 'Password must contain at least 6 characters.';
                        }

                        return null;
                      },
                    ),

                    const SizedBox(
                      height: 16,
                    ),

                    // CONFIRM PASSWORD

                    TextFormField(
                      controller:
                          _confirmPasswordController,

                      obscureText:
                          _hideConfirmPassword,

                      textInputAction:
                          TextInputAction.done,

                      decoration:
                          InputDecoration(
                        labelText:
                            'Confirm Password',
                        border:
                            const OutlineInputBorder(),
                        prefixIcon:
                            const Icon(
                          Icons.lock_outline,
                        ),

                        suffixIcon:
                            IconButton(
                          onPressed: () {
                            setState(() {
                              _hideConfirmPassword =
                                  !_hideConfirmPassword;
                            });
                          },

                          icon: Icon(
                            _hideConfirmPassword
                                ? Icons
                                    .visibility_outlined
                                : Icons
                                    .visibility_off_outlined,
                          ),
                        ),
                      ),

                      validator: (value) {
                        if (value == null ||
                            value.isEmpty) {
                          return 'Confirm your password.';
                        }

                        if (value !=
                            _passwordController
                                .text) {
                          return 'Passwords do not match.';
                        }

                        return null;
                      },

                      onFieldSubmitted: (_) {
                        if (!authProvider
                            .isLoading) {
                          _register();
                        }
                      },
                    ),

                    const SizedBox(
                      height: 24,
                    ),

                    // REGISTER BUTTON

                    FilledButton(
                      onPressed:
                          authProvider.isLoading
                              ? null
                              : _register,

                      child:
                          authProvider.isLoading
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child:
                                      CircularProgressIndicator(
                                    strokeWidth:
                                        2,
                                  ),
                                )
                              : const Text(
                                  'Create Account',
                                ),
                    ),

                    const SizedBox(
                      height: 12,
                    ),

                    // BACK TO LOGIN

                    TextButton(
                      onPressed:
                          authProvider.isLoading
                              ? null
                              : () {
                                  Navigator.pop(
                                    context,
                                  );
                                },
                      child: const Text(
                        'Already have an account? Sign In',
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
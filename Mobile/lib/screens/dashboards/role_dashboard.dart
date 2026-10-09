import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import '../../config/api_config.dart';
import '../../providers/auth_provider.dart';
import '../../services/token_storage_service.dart';
import '../marketplace/marketplace_screen.dart';
import 'admin_dashboard.dart';
import 'buyer_dashboard.dart';
import 'export_officer_dashboard.dart';
import 'gemologist_dashboard.dart';
import 'seller_dashboard.dart';

class RoleDashboard extends StatefulWidget {
  const RoleDashboard({super.key});

  @override
  State<RoleDashboard> createState() => _RoleDashboardState();
}

class _RoleDashboardState extends State<RoleDashboard> {
  final TokenStorageService _tokenStorageService = TokenStorageService();

  bool _loading = true;
  bool _loggingOut = false;

  String? _error;

  String _fullName = '';
  String _role = '';

  static const Color _darkGreen = Color(0xFF08251E);

  static const Color _green = Color(0xFF16483B);

  static const Color _gold = Color(0xFFC99242);

  static const Color _cream = Color(0xFFFAF7F0);

  static const Color _text = Color(0xFF10241F);

  static const Color _muted = Color(0xFF697771);

  static const Color _border = Color(0xFFE7DFD2);

  static const Color _danger = Color(0xFFA33B3B);

  @override
  void initState() {
    super.initState();

    _loadProfile();
  }

  Future<void> _loadProfile() async {
    if (mounted) {
      setState(() {
        _loading = true;
        _error = null;
      });
    }

    try {
      final token = await _tokenStorageService.getToken();

      if (token == null || token.trim().isEmpty) {
        throw Exception('Your session has expired. Please sign in again.');
      }

      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/Auth/me'),
        headers: {
          'Accept': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw Exception(
          _readErrorMessage(response, 'We could not load your profile.'),
        );
      }

      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception('The server returned an unexpected profile response.');
      }

      final fullName = decoded['fullName']?.toString().trim() ?? '';

      final role = decoded['role']?.toString().trim() ?? '';

      if (role.isEmpty) {
        throw Exception('Your account role could not be determined.');
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _fullName = fullName.isEmpty ? 'Gemora User' : fullName;

        _role = role;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _error = error.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _loading = false;
        });
      }
    }
  }

  String get _normalizedRole {
    return _role
        .trim()
        .toLowerCase()
        .replaceAll(' ', '')
        .replaceAll('_', '')
        .replaceAll('-', '');
  }

  String get _cleanName {
    if (_fullName.trim().isEmpty) {
      return 'Gemora User';
    }

    return _fullName.trim();
  }

  String get _cleanRole {
    if (_role.trim().isEmpty) {
      return 'Member';
    }

    return _role.trim();
  }

  String get _initial {
    return _cleanName.substring(0, 1).toUpperCase();
  }

  Future<void> _openWorkspace() async {
    final Widget workspace;

    switch (_normalizedRole) {
      case 'seller':
        workspace = const SellerDashboard();
        break;

      case 'buyer':
        workspace = const BuyerDashboard();
        break;

      case 'gemologist':
        workspace = const GemologistDashboard();
        break;

      case 'exportofficer':
        workspace = const ExportOfficerDashboard();
        break;

      case 'admin':
        workspace = const AdminDashboard();
        break;

      default:
        _showUnsupportedRole();
        return;
    }

    await Navigator.of(context)
        .push(MaterialPageRoute(builder: (_) => workspace));
  }

  Future<void> _openProfile() async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (BuildContext sheetContext) {
        return Container(
          padding: const EdgeInsets.fromLTRB(20, 10, 20, 26),
          decoration: const BoxDecoration(
            color: _cream,
            borderRadius: BorderRadius.vertical(top: Radius.circular(30)),
          ),
          child: SafeArea(
            top: false,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Center(
                    child: Container(
                      width: 42,
                      height: 4,
                      margin: const EdgeInsets.only(bottom: 22),
                      decoration: BoxDecoration(
                        color: const Color(0xFFD6D0C6),
                        borderRadius: BorderRadius.circular(99),
                      ),
                    ),
                  ),

                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(22),
                      border: Border.all(color: _border),
                    ),
                    child: Row(
                      children: [
                        Container(
                          width: 66,
                          height: 66,
                          decoration: const BoxDecoration(
                            color: _gold,
                            shape: BoxShape.circle,
                          ),
                          alignment: Alignment.center,
                          child: Text(
                            _initial,
                            style: const TextStyle(
                              color: _darkGreen,
                              fontSize: 27,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        ),

                        const SizedBox(width: 16),

                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _cleanName,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  color: _text,
                                  fontSize: 19,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),

                              const SizedBox(height: 5),

                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 10,
                                  vertical: 5,
                                ),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFF3E8D5),
                                  borderRadius: BorderRadius.circular(99),
                                ),
                                child: Text(
                                  '$_cleanRole Account',
                                  style: const TextStyle(
                                    color: _green,
                                    fontSize: 10,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),

                        const Icon(Icons.verified_user_outlined, color: _green),
                      ],
                    ),
                  ),

                  const SizedBox(height: 22),

                  const Text(
                    'ACCOUNT',
                    style: TextStyle(
                      color: _gold,
                      fontSize: 10,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.4,
                    ),
                  ),

                  const SizedBox(height: 10),

                  _ProfileActionTile(
                    icon: Icons.home_outlined,
                    title: 'Marketplace Home',
                    subtitle: 'Return to your Gemora marketplace.',
                    onTap: () {
                      Navigator.pop(sheetContext);
                    },
                  ),

                  const SizedBox(height: 8),

                  _ProfileActionTile(
                    icon: Icons.work_outline_rounded,
                    title: '$_cleanRole Workspace',
                    subtitle: _workspaceSubtitle(),
                    onTap: () {
                      Navigator.pop(sheetContext);

                      _openWorkspace();
                    },
                  ),

                  const SizedBox(height: 8),

                  _ProfileActionTile(
                    icon: Icons.refresh_rounded,
                    title: 'Refresh Account',
                    subtitle: 'Reload your latest profile information.',
                    onTap: () {
                      Navigator.pop(sheetContext);

                      _loadProfile();
                    },
                  ),

                  const SizedBox(height: 22),

                  const Divider(color: _border, height: 1),

                  const SizedBox(height: 16),

                  Container(
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFF5F4),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: const Color(0xFFF0D2CF)),
                    ),
                    child: ListTile(
                      onTap: _loggingOut
                          ? null
                          : () {
                              Navigator.pop(sheetContext);

                              _confirmLogout();
                            },
                      leading: const Icon(Icons.logout_rounded, color: _danger),
                      title: const Text(
                        'Log Out',
                        style: TextStyle(
                          color: _danger,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      subtitle: const Text(
                        'End this session and continue as a guest.',
                        style: TextStyle(color: _muted, fontSize: 11),
                      ),
                      trailing: const Icon(
                        Icons.chevron_right_rounded,
                        color: _danger,
                      ),
                    ),
                  ),

                  const SizedBox(height: 12),

                  const Text(
                    'You can still browse verified gemstones after logging out.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: _muted, fontSize: 10, height: 1.5),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  String _workspaceSubtitle() {
    switch (_normalizedRole) {
      case 'seller':
        return 'Manage listings, verification and export activity.';

      case 'buyer':
        return 'Manage purchases and your buyer activities.';

      case 'gemologist':
        return 'Review gemstone verification requests.';

      case 'exportofficer':
        return 'Manage export compliance requests.';

      case 'admin':
        return 'Open administrative controls.';

      default:
        return 'Open your role-specific workspace.';
    }
  }

  Future<void> _confirmLogout() async {
    if (_loggingOut) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (BuildContext dialogContext) {
        return AlertDialog(
          backgroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(22),
          ),
          titlePadding: const EdgeInsets.fromLTRB(24, 24, 24, 0),
          contentPadding: const EdgeInsets.fromLTRB(24, 14, 24, 8),
          actionsPadding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
          title: Row(
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: const BoxDecoration(
                  color: Color(0xFFFFECEA),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.logout_rounded, color: _danger),
              ),

              const SizedBox(width: 12),

              const Expanded(
                child: Text(
                  'Log out of Gemora?',
                  style: TextStyle(
                    color: _text,
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          ),
          content: const Text(
            'You will need to sign in again to buy, sell, or access your workspace. You can continue browsing verified gemstones as a guest.',
            style: TextStyle(color: _muted, height: 1.5),
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(dialogContext, false);
              },
              child: const Text(
                'Cancel',
                style: TextStyle(color: _muted, fontWeight: FontWeight.w700),
              ),
            ),

            FilledButton.icon(
              onPressed: () {
                Navigator.pop(dialogContext, true);
              },
              style: FilledButton.styleFrom(
                backgroundColor: _danger,
                foregroundColor: Colors.white,
              ),
              icon: const Icon(Icons.logout, size: 18),
              label: const Text('Log Out'),
            ),
          ],
        );
      },
    );

    if (confirmed != true || !mounted) {
      return;
    }

    await _logout();
  }

  Future<void> _logout() async {
    if (_loggingOut) {
      return;
    }

    setState(() {
      _loggingOut = true;
    });

    try {
      // IMPORTANT:
      // Always log out through AuthProvider.
      //
      // AuthProvider will:
      // 1. Delete the saved JWT.
      // 2. Set the current user to null.
      // 3. Notify the root application.
      //
      // The root authentication listener then automatically
      // changes RoleDashboard back to PublicHomeScreen.
      await context.read<AuthProvider>().logout();

      if (!mounted) {
        return;
      }

      // Remove any pushed workspace/detail routes while
      // preserving the real application root route.
      Navigator.of(context).popUntil((Route<dynamic> route) => route.isFirst);
    } catch (_) {
      if (!mounted) {
        return;
      }

      setState(() {
        _loggingOut = false;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('We could not log you out. Please try again.'),
        ),
      );
    }
  }

  void _showUnsupportedRole() {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('No workspace is configured for the role "$_role".'),
      ),
    );
  }

  String _readErrorMessage(http.Response response, String fallback) {
    try {
      if (response.body.trim().isNotEmpty) {
        final decoded = jsonDecode(response.body);

        if (decoded is Map<String, dynamic>) {
          final message = decoded['message']?.toString().trim();

          if (message != null && message.isNotEmpty) {
            return message;
          }
        }
      }
    } catch (_) {}

    if (response.statusCode == 401) {
      return 'Your session has expired. Please sign in again.';
    }

    return fallback;
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(
        backgroundColor: _cream,
        body: Center(child: CircularProgressIndicator(color: _green)),
      );
    }

    if (_error != null) {
      return Scaffold(
        backgroundColor: _cream,
        body: SafeArea(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(28),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.person_off_outlined, color: _gold, size: 58),

                  const SizedBox(height: 18),

                  const Text(
                    'Unable to load account',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: _darkGreen,
                      fontSize: 21,
                      fontWeight: FontWeight.w800,
                    ),
                  ),

                  const SizedBox(height: 10),

                  Text(
                    _error!,
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: _muted, height: 1.5),
                  ),

                  const SizedBox(height: 22),

                  FilledButton.icon(
                    onPressed: _loadProfile,
                    style: FilledButton.styleFrom(backgroundColor: _green),
                    icon: const Icon(Icons.refresh),
                    label: const Text('Try Again'),
                  ),

                  const SizedBox(height: 10),

                  TextButton(
                    onPressed: _confirmLogout,
                    child: const Text('Log Out'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    return Stack(
      children: [
        MarketplaceScreen(
          isAuthenticated: true,
          displayName: _fullName,
          role: _role,
          onWorkspace: _openWorkspace,
          onProfile: _openProfile,
        ),

        if (_loggingOut)
          Container(
            color: Colors.black.withValues(alpha: 0.18),
            alignment: Alignment.center,
            child: Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18),
              ),
              child: const Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircularProgressIndicator(color: _green),

                  SizedBox(height: 14),

                  Text(
                    'Logging out...',
                    style: TextStyle(
                      color: _darkGreen,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }
}

class _ProfileActionTile extends StatelessWidget {
  final IconData icon;

  final String title;

  final String subtitle;

  final VoidCallback onTap;

  const _ProfileActionTile({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: const Color(0xFFE7DFD2)),
          ),
          child: Row(
            children: [
              Container(
                width: 42,
                height: 42,
                decoration: BoxDecoration(
                  color: const Color(0xFFF4EEE3),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: const Color(0xFF16483B), size: 21),
              ),

              const SizedBox(width: 13),

              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        color: Color(0xFF10241F),
                        fontSize: 13,
                        fontWeight: FontWeight.w800,
                      ),
                    ),

                    const SizedBox(height: 3),

                    Text(
                      subtitle,
                      style: const TextStyle(
                        color: Color(0xFF697771),
                        fontSize: 10,
                        height: 1.35,
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(width: 8),

              const Icon(Icons.chevron_right_rounded, color: Color(0xFF9A9F9B)),
            ],
          ),
        ),
      ),
    );
  }
}

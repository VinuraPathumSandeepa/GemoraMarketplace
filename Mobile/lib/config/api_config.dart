import 'package:flutter/foundation.dart';

class ApiConfig {
  // ============================================================
  // SERVER CONFIGURATION
  // ============================================================

  // Override at build time with, for example:
  // `--dart-define=API_SERVER_URL=https://api.example.com`.
  static const String _configuredServerUrl = String.fromEnvironment(
    'API_SERVER_URL',
  );

  // Android emulators reach the host through 10.0.2.2. Browsers and desktop
  // builds run on the host, so they use localhost by default.
  static String get serverUrl {
    final configuredUrl = _configuredServerUrl.trim();
    if (configuredUrl.isNotEmpty) {
      return configuredUrl.replaceFirst(RegExp(r'/+$'), '');
    }

    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) {
      return 'http://localhost:5198';
    }

    return 'http://10.0.2.2:5198';
  }

  static String get baseUrl => '$serverUrl/api';

  // ============================================================
  // AUTH ENDPOINTS
  // ============================================================

  static String get login => '$baseUrl/Auth/login';

  static String get register => '$baseUrl/Auth/register';

  static String get verifyEmail => '$baseUrl/Auth/verify-email';

  static String get resendVerificationCode =>
      '$baseUrl/Auth/resend-verification-code';

  static String get currentUser => '$baseUrl/Auth/me';

  // ============================================================
  // EXPORT COMPLIANCE ENDPOINTS
  // ============================================================

  static String get exportRequests => '$baseUrl/ExportRequests';

  static String get myExportRequests => '$baseUrl/ExportRequests/my';

  static String exportRequestFromOrder(dynamic orderId) =>
      '$exportRequests/from-order/$orderId';

  static String orderExportTracking(dynamic orderId) =>
      '$baseUrl/orders/$orderId/export-tracking';

  static String get myExportTracking => '$baseUrl/export-tracking/my';

  static String get sellerOrders => '$baseUrl/seller/orders';

  static String? resolveImageUrl(String? value) {
    final imageUrl = value?.trim();
    if (imageUrl == null || imageUrl.isEmpty) {
      return null;
    }

    final parsedUrl = Uri.tryParse(imageUrl);
    if (parsedUrl != null && parsedUrl.hasScheme) {
      return imageUrl;
    }

    final relativePath = imageUrl.replaceFirst(RegExp(r'^/+'), '');
    return '$serverUrl/$relativePath';
  }

  // ============================================================
  // GEM LISTING ENDPOINTS
  // ============================================================

  static String get myGemListings => '$baseUrl/GemListings/my';

  static String get createGemListing => '$baseUrl/GemListings';

  static String gemListing(int id) => '$baseUrl/GemListings/$id';

  static String updateGemListing(int id) => '$baseUrl/GemListings/$id';

  static String deleteGemListing(int id) => '$baseUrl/GemListings/$id';

  static String submitGemListing(int id) =>
      '$baseUrl/GemListings/$id/submit-verification';

  static String uploadGemImage(int id) => '$baseUrl/GemListings/$id/image';

  static String uploadGemCertificate(int id) =>
      '$baseUrl/GemListings/$id/certificate';

  // ============================================================
  // GEM VERIFICATION ENDPOINTS
  // ============================================================

  static String get pendingGemVerifications =>
      '$baseUrl/GemVerifications/pending';

  static String gemVerification(int verificationId) =>
      '$baseUrl/GemVerifications/$verificationId';

  static String gemVerificationAiAnalysis(int verificationId) =>
      '$baseUrl/GemVerifications/$verificationId/ai-analysis';

  static String reviewGemVerification(int verificationId) =>
      '$baseUrl/GemVerifications/$verificationId/review';

  // ============================================================
  // PROTECTED CERTIFICATE ENDPOINTS
  // ============================================================

  static String gemCertificateAccess(int listingId) =>
      '$baseUrl/GemCertificates/listings/$listingId/access';

  static String gemLegacyCertificate(int listingId) =>
      '$baseUrl/GemCertificates/listings/$listingId/legacy';
}

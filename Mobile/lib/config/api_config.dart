import 'package:flutter/foundation.dart';

class ApiConfig {
  // ============================================================
  // SERVER CONFIGURATION
  // ============================================================

  // Android emulators reach the host through 10.0.2.2. Browsers and desktop
  // builds run on the host, so they must use localhost instead.
  static String get serverUrl {
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

class ApiConfig {
  // ============================================================
  // SERVER CONFIGURATION
  // ============================================================

  // Override at build time with, for example:
  // `--dart-define=API_SERVER_URL=https://api.example.com`.
  static const String _configuredServerUrl = String.fromEnvironment(
    'API_SERVER_URL',
  );

  // Production is the safe default. Local development can still override this
  // with --dart-define=API_SERVER_URL=http://10.0.2.2:5198.
  static String get serverUrl {
    final configuredUrl = _configuredServerUrl.trim();
    if (configuredUrl.isNotEmpty) {
      return configuredUrl.replaceFirst(RegExp(r'/+$'), '');
    }

    return 'https://gemora-api.onrender.com';
  }

  static String get baseUrl => '$serverUrl/api';

  // ============================================================
  // IMAGE URL RESOLVER
  // ============================================================

  static String? resolveImageUrl(String? value) {
    if (value == null || value.trim().isEmpty) {
      return null;
    }

    final url = value.trim();

    if (url.startsWith('https://') || url.startsWith('http://')) {
      return url;
    }

    final slash = url.startsWith('/') ? '' : '/';
    return '$serverUrl$slash$url';
  }

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
  // NOTIFICATION ENDPOINTS
  // ============================================================

  static String get notifications => '$baseUrl/Notifications';

  static String get notificationUnreadCount =>
      '$baseUrl/Notifications/unread-count';

  static String markNotificationRead(String notificationId) =>
      '$baseUrl/Notifications/$notificationId/read';

  static String get markAllNotificationsRead =>
      '$baseUrl/Notifications/read-all';

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

  static String get myOrders => '$baseUrl/orders/my';

  static String get sellerOrders => '$baseUrl/seller/orders';

  // ============================================================
  // GEM LISTING ENDPOINTS
  // ============================================================

  static String get myGemListings => '$baseUrl/GemListings/my';

  static String get adminGemListings => '$baseUrl/admin/gem-listings';

  static String adminGemListing(int id) => '$adminGemListings/$id';

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

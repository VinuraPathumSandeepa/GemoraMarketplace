class ApiConfig {
  // ============================================================
  // SERVER CONFIGURATION
  // ============================================================

  // Local development backend URL.
  // Defaults to standard Android emulator loopback (10.0.2.2).
  // For ChromeOS ARC / adb-reverse, pass `--dart-define=API_SERVER_URL=http://127.0.0.1:5198`.
  static const String serverUrl = String.fromEnvironment(
    'API_SERVER_URL',
    defaultValue: 'http://10.0.2.2:5198',
  );

  static const String baseUrl = '$serverUrl/api';

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

  static const String login = '$baseUrl/Auth/login';

  static const String register = '$baseUrl/Auth/register';

  static const String verifyEmail = '$baseUrl/Auth/verify-email';

  static const String resendVerificationCode =
      '$baseUrl/Auth/resend-verification-code';

  static const String currentUser = '$baseUrl/Auth/me';

  // ============================================================
  // NOTIFICATION ENDPOINTS
  // ============================================================

  static const String notifications = '$baseUrl/Notifications';

  static const String notificationUnreadCount =
      '$baseUrl/Notifications/unread-count';

  static String markNotificationRead(String notificationId) =>
      '$baseUrl/Notifications/$notificationId/read';

  static const String markAllNotificationsRead =
      '$baseUrl/Notifications/read-all';

  // ============================================================
  // EXPORT COMPLIANCE ENDPOINTS
  // ============================================================

  static const String exportRequests = '$baseUrl/ExportRequests';

  static const String myExportRequests = '$baseUrl/ExportRequests/my';

  static String exportRequestFromOrder(dynamic orderId) =>
      '$exportRequests/from-order/$orderId';

  static String orderExportTracking(dynamic orderId) =>
      '$baseUrl/orders/$orderId/export-tracking';

  static const String myExportTracking = '$baseUrl/export-tracking/my';

  static const String sellerOrders = '$baseUrl/seller/orders';

  // ============================================================
  // GEM LISTING ENDPOINTS
  // ============================================================

  static const String myGemListings = '$baseUrl/GemListings/my';

  static const String createGemListing = '$baseUrl/GemListings';

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

  static const String pendingGemVerifications =
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

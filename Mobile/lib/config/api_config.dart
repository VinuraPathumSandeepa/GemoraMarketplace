class ApiConfig {
  // ============================================================
  // SERVER CONFIGURATION
  // ============================================================

  // Android Emulator -> Windows host machine
  static const String serverUrl = 'http://10.0.2.2:5198';

  static const String baseUrl = '$serverUrl/api';

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

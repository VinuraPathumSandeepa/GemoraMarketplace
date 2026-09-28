class ApiConfig {
  // Android Emulator → Windows host machine
  static const String baseUrl = 'http://10.0.2.2:5198/api';

  // API endpoints
  static const String login = '$baseUrl/Auth/login';
  static const String register = '$baseUrl/Auth/register';
  static const String currentUser = '$baseUrl/Auth/me';

  // Export Compliance endpoints
  static const String exportRequests = '$baseUrl/ExportRequests';
  static const String myExportRequests = '$baseUrl/ExportRequests/my';
}
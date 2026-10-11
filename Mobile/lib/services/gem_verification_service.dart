import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/certificate_access_model.dart';
import '../models/certificate_document_model.dart';
import '../models/gem_ai_analysis_model.dart';
import '../models/gem_verification_model.dart';
import 'token_storage_service.dart';

class GemVerificationService {
  final TokenStorageService _tokenStorage = TokenStorageService();

  // ============================================================
  // PENDING VERIFICATIONS
  // ============================================================

  Future<List<GemVerificationModel>> getPendingVerifications() async {
    final token = await _requiredToken();

    final response = await http
        .get(
          Uri.parse(ApiConfig.pendingGemVerifications),
          headers: _headers(token),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode != 200) {
      throw Exception(
        _errorMessage(response, 'Unable to load pending verifications.'),
      );
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! List) {
      throw Exception('The verification queue response was invalid.');
    }

    return decoded.map((item) {
      if (item is! Map<String, dynamic>) {
        throw Exception('A verification record was invalid.');
      }

      return GemVerificationModel.fromJson(item);
    }).toList();
  }

  // ============================================================
  // VERIFICATION DETAILS
  // ============================================================

  Future<GemVerificationModel> getVerification(int verificationId) async {
    final token = await _requiredToken();

    final response = await http
        .get(
          Uri.parse(ApiConfig.gemVerification(verificationId)),
          headers: _headers(token),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode != 200) {
      throw Exception(
        _errorMessage(response, 'Unable to load the verification.'),
      );
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The verification response was invalid.');
    }

    return GemVerificationModel.fromJson(decoded);
  }

  // ============================================================
  // AI ANALYSIS
  // ============================================================

  Future<GemAiAnalysisModel> runAiAnalysis(int verificationId) async {
    final token = await _requiredToken();

    final response = await http
        .post(
          Uri.parse(ApiConfig.gemVerificationAiAnalysis(verificationId)),
          headers: _headers(token),
        )
        .timeout(const Duration(minutes: 2));

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        _errorMessage(response, 'Unable to run AI-assisted analysis.'),
      );
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The AI analysis response was invalid.');
    }

    return GemAiAnalysisModel.fromJson(decoded);
  }

  // ============================================================
  // GEMOLOGIST REVIEW
  // ============================================================

  Future<GemVerificationModel> reviewVerification({
    required int verificationId,
    required String decision,
    String? reviewNotes,
  }) async {
    final token = await _requiredToken();

    final response = await http
        .put(
          Uri.parse(ApiConfig.reviewGemVerification(verificationId)),
          headers: {..._headers(token), 'Content-Type': 'application/json'},
          body: jsonEncode({
            'decision': decision,
            'reviewNotes': reviewNotes?.trim().isEmpty == true
                ? null
                : reviewNotes?.trim(),
          }),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        _errorMessage(response, 'Unable to save the Gemologist review.'),
      );
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The review response was invalid.');
    }

    return GemVerificationModel.fromJson(decoded);
  }

  // ============================================================
  // CERTIFICATE ACCESS
  // ============================================================

  Future<CertificateAccessModel> getCertificateAccess(int listingId) async {
    final token = await _requiredToken();

    final response = await http
        .get(
          Uri.parse(ApiConfig.gemCertificateAccess(listingId)),
          headers: _headers(token),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode != 200) {
      throw Exception(
        _errorMessage(response, 'Unable to access the gemstone certificate.'),
      );
    }

    final decoded = jsonDecode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The certificate access response was invalid.');
    }

    return CertificateAccessModel.fromJson(decoded);
  }

  // ============================================================
  // LOAD PROTECTED CERTIFICATE INTO MEMORY
  //
  // The temporary signed URL stays inside the service.
  // The UI receives certificate bytes only.
  // ============================================================

  Future<CertificateDocumentModel> loadProtectedCertificate(
    int listingId,
  ) async {
    final access = await getCertificateAccess(listingId);

    if (access.url.trim().isEmpty) {
      throw Exception('The protected certificate location was invalid.');
    }

    late final http.Response response;

    if (access.isSigned) {
      response = await http
          .get(
            Uri.parse(access.url),
            headers: const {'Accept': 'application/pdf,image/jpeg,image/png'},
          )
          .timeout(const Duration(seconds: 30));
    } else if (access.isLegacy) {
      final token = await _requiredToken();

      response = await http
          .get(
            Uri.parse(_resolveApiUrl(access.url)),
            headers: {
              ..._headers(token),
              'Accept': 'application/pdf,image/jpeg,image/png',
            },
          )
          .timeout(const Duration(seconds: 30));
    } else {
      throw Exception('The certificate access type is not supported.');
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        _errorMessage(response, 'Unable to load the protected certificate.'),
      );
    }

    final bytes = response.bodyBytes;

    if (bytes.isEmpty) {
      throw Exception('The protected certificate was empty.');
    }

    const maximumCertificateSize = 12 * 1024 * 1024;

    if (bytes.length > maximumCertificateSize) {
      throw Exception('The certificate is too large to preview safely.');
    }

    final contentType = _detectContentType(
      bytes,
      response.headers['content-type'],
    );

    return CertificateDocumentModel(bytes: bytes, contentType: contentType);
  }

  // ============================================================
  // AUTH HELPERS
  // ============================================================

  Future<String> _requiredToken() async {
    final token = await _tokenStorage.getToken();

    if (token == null || token.trim().isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }

    return token.trim();
  }

  Map<String, String> _headers(String token) {
    return {'Accept': 'application/json', 'Authorization': 'Bearer $token'};
  }

  // ============================================================
  // URL HELPERS
  // ============================================================

  String _resolveApiUrl(String value) {
    final trimmed = value.trim();

    if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
      return trimmed;
    }

    return '${ApiConfig.serverUrl}'
        '${trimmed.startsWith('/') ? '' : '/'}'
        '$trimmed';
  }

  // ============================================================
  // CERTIFICATE TYPE DETECTION
  // ============================================================

  String _detectContentType(Uint8List bytes, String? header) {
    if (header != null && header.trim().isNotEmpty) {
      final normalized = header.split(';').first.trim().toLowerCase();

      if (normalized == 'application/pdf' ||
          normalized == 'image/jpeg' ||
          normalized == 'image/jpg' ||
          normalized == 'image/png') {
        return normalized;
      }
    }

    if (_startsWith(bytes, const [0x25, 0x50, 0x44, 0x46])) {
      return 'application/pdf';
    }

    if (_startsWith(bytes, const [0x89, 0x50, 0x4E, 0x47])) {
      return 'image/png';
    }

    if (_startsWith(bytes, const [0xFF, 0xD8, 0xFF])) {
      return 'image/jpeg';
    }

    return 'application/octet-stream';
  }

  bool _startsWith(Uint8List bytes, List<int> signature) {
    if (bytes.length < signature.length) {
      return false;
    }

    for (var index = 0; index < signature.length; index++) {
      if (bytes[index] != signature[index]) {
        return false;
      }
    }

    return true;
  }

  // ============================================================
  // ERROR HANDLING
  // ============================================================

  String _errorMessage(http.Response response, String fallback) {
    try {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        final message = decoded['message'];

        if (message != null && message.toString().trim().isNotEmpty) {
          return message.toString().trim();
        }

        final title = decoded['title'];

        if (title != null && title.toString().trim().isNotEmpty) {
          return title.toString().trim();
        }

        final errors = decoded['errors'];

        if (errors is Map) {
          final messages = <String>[];

          for (final value in errors.values) {
            if (value is List) {
              messages.addAll(value.map((item) => item.toString()));
            }
          }

          if (messages.isNotEmpty) {
            return messages.join('\n');
          }
        }
      }
    } catch (_) {
      // The response may be PDF/image bytes instead of JSON.
    }

    return fallback;
  }
}

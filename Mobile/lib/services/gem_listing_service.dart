import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

import '../config/api_config.dart';
import '../models/gem_listing_model.dart';
import 'token_storage_service.dart';

class GemListingService {
  final TokenStorageService _tokenStorageService = TokenStorageService();

  // ============================================================
  // AUTH
  // ============================================================

  Future<String> _getToken() async {
    final token = await _tokenStorageService.getToken();

    if (token == null || token.trim().isEmpty) {
      throw Exception('Your session has expired. Please sign in again.');
    }

    return token;
  }

  Future<Map<String, String>> _jsonHeaders() async {
    final token = await _getToken();

    return {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };
  }

  Future<Map<String, String>> _authHeaders() async {
    final token = await _getToken();

    return {'Accept': 'application/json', 'Authorization': 'Bearer $token'};
  }

  bool _isSuccess(int statusCode) {
    return statusCode >= 200 && statusCode < 300;
  }

  // ============================================================
  // GET MY LISTINGS
  // ============================================================

  Future<List<GemListingModel>> getMyListings() async {
    final response = await http.get(
      Uri.parse(ApiConfig.myGemListings),
      headers: await _jsonHeaders(),
    );

    if (_isSuccess(response.statusCode)) {
      final decoded = jsonDecode(response.body);

      if (decoded is! List) {
        throw Exception('The server returned an unexpected listings response.');
      }

      return decoded
          .whereType<Map<String, dynamic>>()
          .map(GemListingModel.fromJson)
          .toList();
    }

    throw Exception(
      _readErrorMessage(response, 'We could not load your gem listings.'),
    );
  }

  // ============================================================
  // GET ONE LISTING
  // ============================================================

  Future<GemListingModel> getListing(int id) async {
    final response = await http.get(
      Uri.parse(ApiConfig.gemListing(id)),
      headers: await _jsonHeaders(),
    );

    if (_isSuccess(response.statusCode)) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception(
          'The server returned an unexpected gem listing response.',
        );
      }

      return GemListingModel.fromJson(decoded);
    }

    throw Exception(
      _readErrorMessage(response, 'We could not load this gem listing.'),
    );
  }

  // ============================================================
  // CREATE LISTING
  // ============================================================

  Future<GemListingModel> createListing({
    required String title,
    required String gemType,
    required String description,
    required double caratWeight,
    required String color,
    required String clarity,
    required String cut,
    required double price,
    String currency = 'LKR',
    String? certificateNumber,
    String? certificateAuthority,
  }) async {
    final body = <String, dynamic>{
      'title': title.trim(),
      'gemType': gemType.trim(),
      'description': description.trim(),
      'caratWeight': caratWeight,
      'color': color.trim(),
      'clarity': clarity.trim(),
      'cut': cut.trim(),
      'price': price,
      'currency': currency.trim().toUpperCase(),
      'certificateNumber': _cleanOptional(certificateNumber),
      'certificateAuthority': _cleanOptional(certificateAuthority),
    };

    final response = await http.post(
      Uri.parse(ApiConfig.createGemListing),
      headers: await _jsonHeaders(),
      body: jsonEncode(body),
    );

    if (_isSuccess(response.statusCode)) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception(
          'The server returned an unexpected create listing response.',
        );
      }

      return GemListingModel.fromJson(decoded);
    }

    throw Exception(
      _readErrorMessage(response, 'We could not create the gem listing.'),
    );
  }

  // ============================================================
  // UPDATE LISTING
  // ============================================================

  Future<void> updateListing({
    required int id,
    required String title,
    required String gemType,
    required String description,
    required double caratWeight,
    required String color,
    required String clarity,
    required String cut,
    required double price,
    String currency = 'LKR',
    String? certificateNumber,
    String? certificateAuthority,
  }) async {
    final body = <String, dynamic>{
      'title': title.trim(),
      'gemType': gemType.trim(),
      'description': description.trim(),
      'caratWeight': caratWeight,
      'color': color.trim(),
      'clarity': clarity.trim(),
      'cut': cut.trim(),
      'price': price,
      'currency': currency.trim().toUpperCase(),
      'certificateNumber': _cleanOptional(certificateNumber),
      'certificateAuthority': _cleanOptional(certificateAuthority),
    };

    final response = await http.put(
      Uri.parse(ApiConfig.updateGemListing(id)),
      headers: await _jsonHeaders(),
      body: jsonEncode(body),
    );

    if (_isSuccess(response.statusCode)) {
      return;
    }

    throw Exception(
      _readErrorMessage(response, 'We could not update the gem listing.'),
    );
  }

  // ============================================================
  // DELETE LISTING
  // ============================================================

  Future<void> deleteListing(int id) async {
    final response = await http.delete(
      Uri.parse(ApiConfig.deleteGemListing(id)),
      headers: await _jsonHeaders(),
    );

    if (_isSuccess(response.statusCode)) {
      return;
    }

    throw Exception(
      _readErrorMessage(response, 'We could not delete the gem listing.'),
    );
  }

  // ============================================================
  // GEM IMAGE UPLOAD
  // ============================================================

  Future<GemListingModel> uploadGemImage({
    required int id,
    required String filePath,
  }) async {
    final prepared = await _prepareUpload(
      filePath: filePath,
      type: _UploadType.gemImage,
    );

    final request = http.MultipartRequest(
      'POST',
      Uri.parse(ApiConfig.uploadGemImage(id)),
    );

    request.headers.addAll(await _authHeaders());

    request.files.add(
      http.MultipartFile.fromBytes(
        'file',
        prepared.bytes,
        filename:
            'gem_${DateTime.now().millisecondsSinceEpoch}.${prepared.extension}',
        contentType: MediaType.parse(prepared.mimeType),
      ),
    );

    final streamed = await request.send();

    final response = await http.Response.fromStream(streamed);

    if (_isSuccess(response.statusCode)) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception(
          'The server returned an unexpected image upload response.',
        );
      }

      return GemListingModel.fromJson(decoded);
    }

    throw Exception(
      _readErrorMessage(response, 'We could not upload the gemstone image.'),
    );
  }

  // ============================================================
  // CERTIFICATE UPLOAD
  // ============================================================

  Future<GemListingModel> uploadCertificate({
    required int id,
    required String filePath,
  }) async {
    final prepared = await _prepareUpload(
      filePath: filePath,
      type: _UploadType.certificate,
    );

    final request = http.MultipartRequest(
      'POST',
      Uri.parse(ApiConfig.uploadGemCertificate(id)),
    );

    request.headers.addAll(await _authHeaders());

    request.files.add(
      http.MultipartFile.fromBytes(
        'file',
        prepared.bytes,
        filename:
            'certificate_${DateTime.now().millisecondsSinceEpoch}.${prepared.extension}',
        contentType: MediaType.parse(prepared.mimeType),
      ),
    );

    final streamed = await request.send();

    final response = await http.Response.fromStream(streamed);

    if (_isSuccess(response.statusCode)) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception(
          'The server returned an unexpected certificate upload response.',
        );
      }

      return GemListingModel.fromJson(decoded);
    }

    throw Exception(
      _readErrorMessage(response, 'We could not upload the certificate.'),
    );
  }

  // ============================================================
  // SUBMIT FOR VERIFICATION
  // ============================================================

  Future<GemListingModel> submitForVerification(int id) async {
    final response = await http.post(
      Uri.parse(ApiConfig.submitGemListing(id)),
      headers: await _jsonHeaders(),
    );

    if (_isSuccess(response.statusCode)) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map<String, dynamic>) {
        throw Exception(
          'The server returned an unexpected verification response.',
        );
      }

      return GemListingModel.fromJson(decoded);
    }

    throw Exception(
      _readErrorMessage(
        response,
        'We could not submit this listing for verification.',
      ),
    );
  }

  // ============================================================
  // PREPARE UPLOAD
  //
  // Detect the REAL format from the file bytes.
  // We do not trust Android's temporary filename.
  // ============================================================

  Future<_PreparedUpload> _prepareUpload({
    required String filePath,
    required _UploadType type,
  }) async {
    final file = File(filePath);

    if (!await file.exists()) {
      throw Exception(
        'The selected file could not be found. Please select it again.',
      );
    }

    final bytes = await file.readAsBytes();

    if (bytes.isEmpty) {
      throw Exception('The selected file is empty.');
    }

    if (type == _UploadType.gemImage && bytes.length > 5 * 1024 * 1024) {
      throw Exception('Gemstone image must be 5 MB or smaller.');
    }

    if (type == _UploadType.certificate && bytes.length > 10 * 1024 * 1024) {
      throw Exception('Certificate must be 10 MB or smaller.');
    }

    // PNG
    if (_isPng(bytes)) {
      return _PreparedUpload(
        bytes: bytes,
        extension: 'png',
        mimeType: 'image/png',
      );
    }

    // JPEG
    if (_isJpeg(bytes)) {
      return _PreparedUpload(
        bytes: bytes,
        extension: 'jpg',
        mimeType: 'image/jpeg',
      );
    }

    // WEBP - only allowed for gem images
    if (type == _UploadType.gemImage && _isWebp(bytes)) {
      return _PreparedUpload(
        bytes: bytes,
        extension: 'webp',
        mimeType: 'image/webp',
      );
    }

    // PDF - only allowed for certificates
    if (type == _UploadType.certificate && _isPdf(bytes)) {
      return _PreparedUpload(
        bytes: bytes,
        extension: 'pdf',
        mimeType: 'application/pdf',
      );
    }

    if (type == _UploadType.gemImage) {
      throw Exception(
        'Unsupported gemstone image. Please select a valid JPG, JPEG, PNG or WEBP image.',
      );
    }

    throw Exception(
      'Unsupported certificate. Please select a valid PDF, JPG, JPEG or PNG file.',
    );
  }

  // ============================================================
  // FILE SIGNATURES
  // ============================================================

  bool _isPng(List<int> bytes) {
    return bytes.length >= 8 &&
        bytes[0] == 0x89 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x4E &&
        bytes[3] == 0x47 &&
        bytes[4] == 0x0D &&
        bytes[5] == 0x0A &&
        bytes[6] == 0x1A &&
        bytes[7] == 0x0A;
  }

  bool _isJpeg(List<int> bytes) {
    return bytes.length >= 3 &&
        bytes[0] == 0xFF &&
        bytes[1] == 0xD8 &&
        bytes[2] == 0xFF;
  }

  bool _isWebp(List<int> bytes) {
    return bytes.length >= 12 &&
        bytes[0] == 0x52 &&
        bytes[1] == 0x49 &&
        bytes[2] == 0x46 &&
        bytes[3] == 0x46 &&
        bytes[8] == 0x57 &&
        bytes[9] == 0x45 &&
        bytes[10] == 0x42 &&
        bytes[11] == 0x50;
  }

  bool _isPdf(List<int> bytes) {
    return bytes.length >= 4 &&
        bytes[0] == 0x25 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x44 &&
        bytes[3] == 0x46;
  }

  // ============================================================
  // OPTIONAL STRING
  // ============================================================

  String? _cleanOptional(String? value) {
    final cleaned = value?.trim();

    if (cleaned == null || cleaned.isEmpty) {
      return null;
    }

    return cleaned;
  }

  // ============================================================
  // API ERROR
  // ============================================================

  String _readErrorMessage(http.Response response, String fallback) {
    try {
      if (response.body.trim().isNotEmpty) {
        final decoded = jsonDecode(response.body);

        if (decoded is Map<String, dynamic>) {
          final message = decoded['message']?.toString().trim();

          if (message != null && message.isNotEmpty) {
            return message;
          }

          final errors = decoded['errors'];

          if (errors is Map<String, dynamic>) {
            final messages = <String>[];

            for (final value in errors.values) {
              if (value is List) {
                for (final item in value) {
                  final text = item.toString().trim();

                  if (text.isNotEmpty) {
                    messages.add(text);
                  }
                }
              } else if (value != null) {
                final text = value.toString().trim();

                if (text.isNotEmpty) {
                  messages.add(text);
                }
              }
            }

            if (messages.isNotEmpty) {
              return messages.join('\n');
            }
          }
        }
      }
    } catch (_) {
      // Use HTTP fallback below.
    }

    switch (response.statusCode) {
      case 400:
        return fallback;

      case 401:
        return 'Your session has expired. Please sign in again.';

      case 403:
        return 'Only Seller accounts can access gem listings.';

      case 404:
        return 'The requested gem listing was not found.';

      case 413:
        return 'The selected file is too large.';

      case 500:
        return 'The server could not complete this request. Please try again.';

      default:
        return fallback;
    }
  }
}

// ============================================================
// INTERNAL UPLOAD TYPES
// ============================================================

enum _UploadType { gemImage, certificate }

class _PreparedUpload {
  final List<int> bytes;
  final String extension;
  final String mimeType;

  const _PreparedUpload({
    required this.bytes,
    required this.extension,
    required this.mimeType,
  });
}

import 'dart:typed_data';

class CertificateDocumentModel {
  final Uint8List bytes;
  final String contentType;

  const CertificateDocumentModel({
    required this.bytes,
    required this.contentType,
  });

  bool get isPdf {
    return contentType.toLowerCase() == 'application/pdf';
  }

  bool get isImage {
    final type = contentType.toLowerCase();

    return type == 'image/jpeg' || type == 'image/jpg' || type == 'image/png';
  }
}

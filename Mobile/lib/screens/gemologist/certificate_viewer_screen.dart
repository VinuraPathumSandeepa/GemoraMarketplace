import 'package:flutter/material.dart';
import 'package:pdfrx/pdfrx.dart';

import '../../models/certificate_document_model.dart';

class CertificateViewerScreen extends StatelessWidget {
  final CertificateDocumentModel document;
  final String? certificateNumber;
  final String? certificateAuthority;

  const CertificateViewerScreen({
    super.key,
    required this.document,
    this.certificateNumber,
    this.certificateAuthority,
  });

  static const Color _darkEmerald = Color(0xFF08251E);
  static const Color _emerald = Color(0xFF16483B);
  static const Color _gold = Color(0xFFC99242);
  static const Color _ivory = Color(0xFFFAF7F0);
  static const Color _muted = Color(0xFF697771);
  static const Color _border = Color(0xFFE7DFD2);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        backgroundColor: _darkEmerald,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Protected Certificate',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            Text(
              'Secure Gemora Viewer',
              style: TextStyle(fontSize: 11, color: Colors.white60),
            ),
          ],
        ),
      ),
      body: Column(
        children: [
          Container(
            width: double.infinity,
            margin: const EdgeInsets.fromLTRB(14, 14, 14, 10),
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: _border),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 42,
                  height: 42,
                  decoration: BoxDecoration(
                    color: _emerald.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(
                    Icons.verified_user_outlined,
                    color: _emerald,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Secure certificate access',
                        style: TextStyle(
                          color: _darkEmerald,
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 4),
                      const Text(
                        'This protected document is being viewed inside '
                        'Gemora. The temporary storage URL is not exposed.',
                        style: TextStyle(
                          color: _muted,
                          fontSize: 11,
                          height: 1.45,
                        ),
                      ),
                      if (certificateNumber != null &&
                          certificateNumber!.trim().isNotEmpty) ...[
                        const SizedBox(height: 8),
                        Text(
                          'Certificate: $certificateNumber',
                          style: const TextStyle(
                            color: _darkEmerald,
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                      if (certificateAuthority != null &&
                          certificateAuthority!.trim().isNotEmpty)
                        Text(
                          'Authority: $certificateAuthority',
                          style: const TextStyle(color: _muted, fontSize: 11),
                        ),
                    ],
                  ),
                ),
                const Icon(Icons.lock_rounded, color: _gold, size: 20),
              ],
            ),
          ),
          Expanded(
            child: Container(
              margin: const EdgeInsets.fromLTRB(14, 0, 14, 14),
              clipBehavior: Clip.antiAlias,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: _border),
              ),
              child: _buildDocumentViewer(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDocumentViewer() {
    if (document.isPdf) {
      return PdfViewer.data(
        document.bytes,
        sourceName: 'gemora-certificate.pdf',
      );
    }

    if (document.isImage) {
      return Container(
        color: const Color(0xFF171A19),
        child: InteractiveViewer(
          minScale: 0.5,
          maxScale: 6,
          boundaryMargin: const EdgeInsets.all(80),
          child: Center(
            child: Image.memory(
              document.bytes,
              fit: BoxFit.contain,
              errorBuilder: (context, error, stackTrace) {
                return const _DocumentError(
                  message: 'The certificate image could not be displayed.',
                );
              },
            ),
          ),
        ),
      );
    }

    return const _DocumentError(
      message: 'This certificate format cannot be previewed inside Gemora.',
    );
  }
}

class _DocumentError extends StatelessWidget {
  final String message;

  const _DocumentError({required this.message});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.description_outlined,
              size: 58,
              color: Color(0xFFC99242),
            ),
            const SizedBox(height: 16),
            Text(
              message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Color(0xFF697771), height: 1.5),
            ),
          ],
        ),
      ),
    );
  }
}

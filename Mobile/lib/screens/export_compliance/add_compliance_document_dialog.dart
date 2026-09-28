import 'dart:typed_data';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../../models/compliance_document_model.dart';
import '../../services/export_compliance_service.dart';

class AddComplianceDocumentDialog extends StatefulWidget {
  final String exportRequestId;

  const AddComplianceDocumentDialog({
    super.key,
    required this.exportRequestId,
  });

  @override
  State<AddComplianceDocumentDialog> createState() =>
      _AddComplianceDocumentDialogState();
}

class _AddComplianceDocumentDialogState
    extends State<AddComplianceDocumentDialog> {
  final _formKey = GlobalKey<FormState>();
  final _exportService = ExportComplianceService();

  final _typeController = TextEditingController();
  final _numberController = TextEditingController();
  final _issuerController = TextEditingController();

  DateTime? _issueDate;
  DateTime? _expiryDate;

  String? _selectedFileName;
  Uint8List? _selectedFileBytes;
  int? _selectedFileSize;

  bool _isSaving = false;
  String? _statusText;
  String? _errorMessage;

  @override
  void dispose() {
    _typeController.dispose();
    _numberController.dispose();
    _issuerController.dispose();
    super.dispose();
  }

  Future<void> _pickFile() async {
    setState(() {
      _errorMessage = null;
    });

    try {
      final files = await FilePicker.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
      );

      if (files.isNotEmpty) {
        final file = files.first;
        final bytes = await file.readAsBytes();

        if (bytes.isEmpty) {
          setState(() {
            _errorMessage = 'The selected file is empty.';
          });
          return;
        }

        // Validate max size 10MB
        if (bytes.length > 10 * 1024 * 1024) {
          setState(() {
            _errorMessage =
                'The selected file is too large. Maximum file size is 10 MB.';
          });
          return;
        }

        setState(() {
          _selectedFileName = file.name;
          _selectedFileBytes = bytes;
          _selectedFileSize = bytes.length;
        });
      }
    } catch (err) {
      setState(() {
        _errorMessage = 'Failed to select file: $err';
      });
    }
  }

  Future<void> _selectIssueDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _issueDate ?? DateTime.now(),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (picked != null) {
      setState(() {
        _issueDate = picked;
      });
    }
  }

  Future<void> _selectExpiryDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _expiryDate ?? DateTime.now().add(const Duration(days: 365)),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (picked != null) {
      setState(() {
        _expiryDate = picked;
      });
    }
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate() || _isSaving) return;

    if (_issueDate != null && _expiryDate != null && _expiryDate!.isBefore(_issueDate!)) {
      setState(() {
        _errorMessage = 'Expiry date cannot be earlier than issue date.';
      });
      return;
    }

    setState(() {
      _isSaving = true;
      _errorMessage = null;
      _statusText = 'Saving document details...';
    });

    ComplianceDocumentModel? createdDoc;

    // STEP 1: Add Document Metadata
    try {
      createdDoc = await _exportService.addComplianceDocument(
        widget.exportRequestId,
        documentType: _typeController.text,
        documentNumber: _numberController.text,
        issuer: _issuerController.text,
        issueDate: _issueDate,
        expiryDate: _expiryDate,
      );
    } catch (err) {
      setState(() {
        _errorMessage = err.toString().replaceAll('Exception: ', '');
        _isSaving = false;
        _statusText = null;
      });
      return;
    }

    // STEP 2: Upload File if Selected
    if (_selectedFileBytes != null && _selectedFileName != null) {
      setState(() {
        _statusText = 'Uploading document file...';
      });

      try {
        await _exportService.uploadComplianceDocumentFile(
          widget.exportRequestId,
          createdDoc.id,
          _selectedFileBytes!,
          _selectedFileName!,
        );
      } catch (uploadErr) {
        if (!mounted) return;
        final uploadErrMsg = uploadErr.toString().replaceAll('Exception: ', '');
        
        // Partial Failure Handling: metadata created, file upload failed
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Document details were saved, but the file upload failed. ($uploadErrMsg)',
            ),
            backgroundColor: Colors.orange.shade800,
            duration: const Duration(seconds: 5),
          ),
        );

        Navigator.of(context).pop(true);
        return;
      }
    }

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Compliance document added successfully.'),
        backgroundColor: Colors.green,
      ),
    );

    Navigator.of(context).pop(true);
  }

  String _formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Add Document',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: _isSaving ? null : () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const Divider(height: 20),

              if (_errorMessage != null) ...[
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.red.shade300),
                  ),
                  child: Text(
                    _errorMessage!,
                    style: TextStyle(color: Colors.red.shade800, fontSize: 13),
                  ),
                ),
                const SizedBox(height: 12),
              ],

              // DOCUMENT TYPE
              TextFormField(
                controller: _typeController,
                decoration: const InputDecoration(
                  labelText: 'Document Type *',
                  hintText: 'e.g. GemologyCertificate, ExportPermit',
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Document type is required.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),

              // DOCUMENT NUMBER
              TextFormField(
                controller: _numberController,
                decoration: const InputDecoration(
                  labelText: 'Document Number (optional)',
                  hintText: 'e.g. GEM-998877',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),

              // ISSUER
              TextFormField(
                controller: _issuerController,
                decoration: const InputDecoration(
                  labelText: 'Issuer (optional)',
                  hintText: 'e.g. NGJA',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),

              // DATE PICKERS ROW
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _isSaving ? null : _selectIssueDate,
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text(
                        _issueDate == null
                            ? 'Issue Date'
                            : 'Issue: ${_issueDate!.toString().split(' ')[0]}',
                        style: const TextStyle(fontSize: 12),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _isSaving ? null : _selectExpiryDate,
                      icon: const Icon(Icons.event, size: 16),
                      label: Text(
                        _expiryDate == null
                            ? 'Expiry Date'
                            : 'Expiry: ${_expiryDate!.toString().split(' ')[0]}',
                        style: const TextStyle(fontSize: 12),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // FILE SELECTION
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.grey.shade100,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey.shade300),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        ElevatedButton.icon(
                          onPressed: _isSaving ? null : _pickFile,
                          icon: const Icon(Icons.attach_file),
                          label: const Text('Select File'),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            _selectedFileName ?? 'No file selected',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: _selectedFileName != null
                                  ? FontWeight.bold
                                  : FontWeight.normal,
                              color: _selectedFileName != null
                                  ? Colors.black87
                                  : Colors.grey.shade600,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                    if (_selectedFileSize != null) ...[
                      const SizedBox(height: 4),
                      Text(
                        'Size: ${_formatFileSize(_selectedFileSize!)}',
                        style: TextStyle(fontSize: 12, color: Colors.grey.shade700),
                      ),
                    ],
                    const SizedBox(height: 6),
                    Text(
                      'Allowed formats: PDF, JPG, JPEG, PNG | Max size: 10 MB',
                      style: TextStyle(fontSize: 11, color: Colors.grey.shade600),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // SAVE BUTTON
              FilledButton(
                onPressed: _isSaving ? null : _submitForm,
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
                child: _isSaving
                    ? Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          ),
                          const SizedBox(width: 10),
                          Text(_statusText ?? 'Saving...'),
                        ],
                      )
                    : const Text(
                        'Save & Upload',
                        style: TextStyle(fontSize: 16),
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

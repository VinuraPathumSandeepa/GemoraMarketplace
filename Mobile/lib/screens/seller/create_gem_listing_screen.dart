import 'dart:io';

import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';

import '../../models/gem_listing_model.dart';
import '../../services/gem_listing_service.dart';

class CreateGemListingScreen extends StatefulWidget {
  const CreateGemListingScreen({super.key});

  @override
  State<CreateGemListingScreen> createState() => _CreateGemListingScreenState();
}

class _CreateGemListingScreenState extends State<CreateGemListingScreen> {
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();

  final GemListingService _listingService = GemListingService();

  final TextEditingController _titleController = TextEditingController();
  final TextEditingController _gemTypeController = TextEditingController();
  final TextEditingController _descriptionController = TextEditingController();
  final TextEditingController _caratController = TextEditingController();
  final TextEditingController _colorController = TextEditingController();
  final TextEditingController _clarityController = TextEditingController();
  final TextEditingController _cutController = TextEditingController();
  final TextEditingController _priceController = TextEditingController();
  final TextEditingController _certificateNumberController =
      TextEditingController();
  final TextEditingController _certificateAuthorityController =
      TextEditingController();

  String _currency = 'LKR';

  XFile? _selectedGemImage;
  int? _selectedGemImageSize;

  XFile? _selectedCertificate;
  int? _selectedCertificateSize;

  bool _saving = false;

  static const Color _darkEmerald = Color(0xFF08251E);
  static const Color _emerald = Color(0xFF16483B);
  static const Color _gold = Color(0xFFC99242);
  static const Color _lightGold = Color(0xFFE4BC74);
  static const Color _ivory = Color(0xFFFAF7F0);
  static const Color _border = Color(0xFFE7DFD2);
  static const Color _muted = Color(0xFF697771);
  static const Color _danger = Color(0xFFA5433A);
  static const Color _success = Color(0xFF247A50);

  @override
  void dispose() {
    _titleController.dispose();
    _gemTypeController.dispose();
    _descriptionController.dispose();
    _caratController.dispose();
    _colorController.dispose();
    _clarityController.dispose();
    _cutController.dispose();
    _priceController.dispose();
    _certificateNumberController.dispose();
    _certificateAuthorityController.dispose();

    super.dispose();
  }

  String? _requiredText(String? value, String name, {int minLength = 1}) {
    final String text = value?.trim() ?? '';

    if (text.isEmpty) {
      return '$name is required.';
    }

    if (text.length < minLength) {
      return '$name must contain at least $minLength characters.';
    }

    return null;
  }

  String? _positiveNumber(String? value, String name) {
    final String text = value?.trim() ?? '';

    if (text.isEmpty) {
      return '$name is required.';
    }

    final double? number = double.tryParse(text);

    if (number == null) {
      return 'Enter a valid $name.';
    }

    if (number <= 0) {
      return '$name must be greater than 0.';
    }

    return null;
  }

  Future<void> _pickGemImage() async {
    try {
      const XTypeGroup imageTypes = XTypeGroup(
        label: 'Gemstone images',
        extensions: <String>['jpg', 'jpeg', 'png', 'webp'],
      );

      final XFile? file = await openFile(
        acceptedTypeGroups: <XTypeGroup>[imageTypes],
      );

      if (file == null) {
        return;
      }

      final int fileSize = await file.length();

      const int maxImageSize = 5 * 1024 * 1024;

      if (fileSize > maxImageSize) {
        _showError('Gemstone image must be 5 MB or smaller.');
        return;
      }

      final String extension = _extensionOf(file.name);

      const Set<String> allowedExtensions = <String>{
        'jpg',
        'jpeg',
        'png',
        'webp',
      };

      if (!allowedExtensions.contains(extension)) {
        _showError('Please select a JPG, JPEG, PNG or WEBP image.');
        return;
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _selectedGemImage = file;
        _selectedGemImageSize = fileSize;
      });
    } catch (_) {
      _showError('Could not select the gemstone image.');
    }
  }

  Future<void> _pickCertificate() async {
    try {
      const XTypeGroup certificateTypes = XTypeGroup(
        label: 'Gem certificates',
        extensions: <String>['pdf', 'jpg', 'jpeg', 'png'],
      );

      final XFile? file = await openFile(
        acceptedTypeGroups: <XTypeGroup>[certificateTypes],
      );

      if (file == null) {
        return;
      }

      final int fileSize = await file.length();

      const int maxCertificateSize = 10 * 1024 * 1024;

      if (fileSize > maxCertificateSize) {
        _showError('Certificate must be 10 MB or smaller.');
        return;
      }

      final String extension = _extensionOf(file.name);

      const Set<String> allowedExtensions = <String>{
        'pdf',
        'jpg',
        'jpeg',
        'png',
      };

      if (!allowedExtensions.contains(extension)) {
        _showError('Please select a PDF, JPG, JPEG or PNG certificate.');
        return;
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _selectedCertificate = file;
        _selectedCertificateSize = fileSize;
      });
    } catch (_) {
      _showError('Could not select the certificate.');
    }
  }

  String _extensionOf(String fileName) {
    final List<String> parts = fileName.toLowerCase().split('.');

    if (parts.length < 2) {
      return '';
    }

    return parts.last;
  }

  void _removeGemImage() {
    setState(() {
      _selectedGemImage = null;
      _selectedGemImageSize = null;
    });
  }

  void _removeCertificate() {
    setState(() {
      _selectedCertificate = null;
      _selectedCertificateSize = null;
    });
  }

  Future<void> _saveListing() async {
    if (_saving) {
      return;
    }

    FocusScope.of(context).unfocus();

    if (!_formKey.currentState!.validate()) {
      return;
    }

    if (_selectedGemImage == null) {
      _showError('Please select a gemstone image.');
      return;
    }

    setState(() {
      _saving = true;
    });

    try {
      GemListingModel listing = await _listingService.createListing(
        title: _titleController.text,
        gemType: _gemTypeController.text,
        description: _descriptionController.text,
        caratWeight: double.parse(_caratController.text.trim()),
        color: _colorController.text,
        clarity: _clarityController.text,
        cut: _cutController.text,
        price: double.parse(_priceController.text.trim()),
        currency: _currency,
        certificateNumber: _certificateNumberController.text,
        certificateAuthority: _certificateAuthorityController.text,
      );

      if (_selectedGemImage != null) {
        listing = await _listingService.uploadGemImage(
          id: listing.id,
          filePath: _selectedGemImage!.path,
        );
      }

      if (_selectedCertificate != null) {
        listing = await _listingService.uploadCertificate(
          id: listing.id,
          filePath: _selectedCertificate!.path,
        );
      }

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Gem listing created successfully.'),
          backgroundColor: _success,
        ),
      );

      Navigator.of(context).pop(listing);
    } catch (error) {
      if (!mounted) {
        return;
      }

      final String message = error.toString().replaceFirst('Exception: ', '');

      _showError(message);
    } finally {
      if (mounted) {
        setState(() {
          _saving = false;
        });
      }
    }
  }

  void _showError(String message) {
    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message), backgroundColor: _danger));
  }

  String _formatFileSize(int? bytes) {
    if (bytes == null) {
      return '';
    }

    if (bytes < 1024) {
      return '$bytes B';
    }

    final double kb = bytes / 1024;

    if (kb < 1024) {
      return '${kb.toStringAsFixed(1)} KB';
    }

    final double mb = kb / 1024;

    return '${mb.toStringAsFixed(1)} MB';
  }

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
          children: <Widget>[
            Text(
              'Create Gem Listing',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            Text(
              'Seller Workspace',
              style: TextStyle(fontSize: 11, color: Colors.white60),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(18, 20, 18, 120),
            children: <Widget>[
              _buildHeader(),

              const SizedBox(height: 20),

              _buildSection(
                title: 'Gemstone Information',
                subtitle: 'Enter the main details buyers and gemologists need.',
                children: <Widget>[
                  _field(
                    controller: _titleController,
                    label: 'Listing Title',
                    hint: 'Example: Premium Hessonite Garnet',
                    icon: Icons.title_rounded,
                    validator: (String? value) =>
                        _requiredText(value, 'Title', minLength: 3),
                  ),

                  _field(
                    controller: _gemTypeController,
                    label: 'Gem Type',
                    hint: 'Example: Hessonite Garnet',
                    icon: Icons.diamond_outlined,
                    validator: (String? value) =>
                        _requiredText(value, 'Gem type'),
                  ),

                  _field(
                    controller: _descriptionController,
                    label: 'Description',
                    hint: 'Describe the gemstone, appearance and notable characteristics.',
                    icon: Icons.description_outlined,
                    maxLines: 5,
                    validator: (String? value) =>
                        _requiredText(value, 'Description', minLength: 10),
                  ),
                ],
              ),

              const SizedBox(height: 18),

              _buildSection(
                title: 'Gem Characteristics',
                subtitle: 'Add measurable and visual gemstone characteristics.',
                children: <Widget>[
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Expanded(
                        child: _field(
                          controller: _caratController,
                          label: 'Carat Weight',
                          hint: '4.85',
                          icon: Icons.scale_outlined,
                          keyboardType: const TextInputType.numberWithOptions(
                            decimal: true,
                          ),
                          validator: (String? value) =>
                              _positiveNumber(value, 'carat weight'),
                        ),
                      ),

                      const SizedBox(width: 12),

                      Expanded(
                        child: _field(
                          controller: _colorController,
                          label: 'Color',
                          hint: 'Honey Orange',
                          icon: Icons.palette_outlined,
                          validator: (String? value) =>
                              _requiredText(value, 'Color'),
                        ),
                      ),
                    ],
                  ),

                  _field(
                    controller: _clarityController,
                    label: 'Clarity',
                    hint: 'Transparent to Slightly Translucent',
                    icon: Icons.visibility_outlined,
                    validator: (String? value) =>
                        _requiredText(value, 'Clarity'),
                  ),

                  _field(
                    controller: _cutController,
                    label: 'Cut',
                    hint: 'Oval Mixed Cut',
                    icon: Icons.auto_awesome_outlined,
                    validator: (String? value) => _requiredText(value, 'Cut'),
                  ),
                ],
              ),

              const SizedBox(height: 18),

              _buildSection(
                title: 'Pricing',
                subtitle: 'Set the asking price for this listing.',
                children: <Widget>[
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Expanded(
                        flex: 2,
                        child: _field(
                          controller: _priceController,
                          label: 'Price',
                          hint: '185000',
                          icon: Icons.payments_outlined,
                          keyboardType: const TextInputType.numberWithOptions(
                            decimal: true,
                          ),
                          validator: (String? value) =>
                              _positiveNumber(value, 'price'),
                        ),
                      ),

                      const SizedBox(width: 12),

                      Expanded(child: _currencyField()),
                    ],
                  ),
                ],
              ),

              const SizedBox(height: 18),

              _buildSection(
                title: 'Gemstone Image',
                subtitle: 'Upload a clear image. JPG, JPEG, PNG or WEBP. Maximum 5 MB.',
                children: <Widget>[_buildGemImagePicker()],
              ),

              const SizedBox(height: 18),

              _buildSection(
                title: 'Certificate',
                subtitle: 'Enter certificate details and upload the supporting document.',
                children: <Widget>[
                  _field(
                    controller: _certificateNumberController,
                    label: 'Certificate Number',
                    hint: 'Example: GM-HG-2026-001',
                    icon: Icons.verified_outlined,
                  ),

                  _field(
                    controller: _certificateAuthorityController,
                    label: 'Certificate Authority',
                    hint: 'Example: Gemora Sample Lab',
                    icon: Icons.account_balance_outlined,
                  ),

                  _buildCertificatePicker(),
                ],
              ),

              const SizedBox(height: 22),

              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: const Color(0xFFF2EBDD),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: _border),
                ),
                child: const Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Icon(Icons.info_outline_rounded, color: _gold),

                    SizedBox(width: 10),

                    Expanded(
                      child: Text(
                        'The listing is created as Draft first. Gemora then uploads the selected gemstone image and certificate.',
                        style: TextStyle(
                          color: _darkEmerald,
                          height: 1.5,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 24),

              SizedBox(
                height: 54,
                child: FilledButton.icon(
                  onPressed: _saving ? null : _saveListing,
                  style: FilledButton.styleFrom(
                    backgroundColor: _gold,
                    foregroundColor: _darkEmerald,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                  ),
                  icon: _saving
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: _darkEmerald,
                          ),
                        )
                      : const Icon(Icons.add_circle_outline),
                  label: Text(
                    _saving ? 'Creating & Uploading...' : 'Create Gem Listing',
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        color: _darkEmerald,
        borderRadius: BorderRadius.circular(20),
      ),
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(
            'NEW GEMSTONE',
            style: TextStyle(
              color: _lightGold,
              fontSize: 11,
              letterSpacing: 1.2,
              fontWeight: FontWeight.w800,
            ),
          ),

          SizedBox(height: 8),

          Text(
            'Create a trusted gemstone listing.',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              height: 1.2,
              fontWeight: FontWeight.w800,
            ),
          ),

          SizedBox(height: 8),

          Text(
            'Add gemstone details, a clear image and certification evidence for professional verification.',
            style: TextStyle(color: Colors.white70, fontSize: 12, height: 1.5),
          ),
        ],
      ),
    );
  }

  Widget _buildGemImagePicker() {
    final XFile? image = _selectedGemImage;

    if (image == null) {
      return _emptyUploadBox(
        icon: Icons.add_photo_alternate_outlined,
        title: 'Choose Gemstone Image',
        subtitle: 'JPG, JPEG, PNG, WEBP • Maximum 5 MB',
        onTap: _pickGemImage,
      );
    }

    return Column(
      children: <Widget>[
        Container(
          width: double.infinity,
          height: 220,
          decoration: BoxDecoration(
            color: const Color(0xFFF1EEE7),
            borderRadius: BorderRadius.circular(16),
          ),
          clipBehavior: Clip.antiAlias,
          child: Image.file(
            File(image.path),
            fit: BoxFit.contain,
            errorBuilder:
                (BuildContext context, Object error, StackTrace? stackTrace) {
                  return const Center(
                    child: Icon(Icons.diamond_outlined, size: 60, color: _gold),
                  );
                },
          ),
        ),

        const SizedBox(height: 12),

        _selectedFileCard(
          icon: Icons.image_outlined,
          fileName: image.name,
          fileSize: _formatFileSize(_selectedGemImageSize),
          onReplace: _pickGemImage,
          onRemove: _removeGemImage,
        ),
      ],
    );
  }

  Widget _buildCertificatePicker() {
    final XFile? certificate = _selectedCertificate;

    if (certificate == null) {
      return _emptyUploadBox(
        icon: Icons.upload_file_outlined,
        title: 'Choose Certificate',
        subtitle: 'PDF, JPG, JPEG, PNG • Maximum 10 MB',
        onTap: _pickCertificate,
      );
    }

    final String extension = _extensionOf(certificate.name);

    return _selectedFileCard(
      icon: extension == 'pdf'
          ? Icons.picture_as_pdf_outlined
          : Icons.description_outlined,
      fileName: certificate.name,
      fileSize: _formatFileSize(_selectedCertificateSize),
      onReplace: _pickCertificate,
      onRemove: _removeCertificate,
    );
  }

  Widget _emptyUploadBox({
    required IconData icon,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 18),
        decoration: BoxDecoration(
          color: const Color(0xFFFCFBF8),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: _gold.withValues(alpha: 0.65)),
        ),
        child: Column(
          children: <Widget>[
            Container(
              width: 56,
              height: 56,
              decoration: const BoxDecoration(
                color: Color(0xFFF2EBDD),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, color: _gold, size: 29),
            ),

            const SizedBox(height: 13),

            Text(
              title,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: _darkEmerald,
                fontSize: 14,
                fontWeight: FontWeight.w800,
              ),
            ),

            const SizedBox(height: 6),

            Text(
              subtitle,
              textAlign: TextAlign.center,
              style: const TextStyle(color: _muted, fontSize: 11),
            ),

            const SizedBox(height: 14),

            OutlinedButton.icon(
              onPressed: onTap,
              style: OutlinedButton.styleFrom(
                foregroundColor: _emerald,
                side: const BorderSide(color: _emerald),
              ),
              icon: const Icon(Icons.folder_open_outlined, size: 18),
              label: const Text('Choose File'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _selectedFileCard({
    required IconData icon,
    required String fileName,
    required String fileSize,
    required VoidCallback onReplace,
    required VoidCallback onRemove,
  }) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFF7F5EF),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _border),
      ),
      child: Row(
        children: <Widget>[
          Container(
            width: 46,
            height: 46,
            decoration: BoxDecoration(
              color: _emerald.withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: _emerald),
          ),

          const SizedBox(width: 12),

          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  fileName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _darkEmerald,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),

                const SizedBox(height: 4),

                Row(
                  children: <Widget>[
                    const Icon(
                      Icons.check_circle_outline,
                      color: _success,
                      size: 14,
                    ),

                    const SizedBox(width: 4),

                    Text(
                      fileSize,
                      style: const TextStyle(color: _muted, fontSize: 10),
                    ),
                  ],
                ),
              ],
            ),
          ),

          PopupMenuButton<String>(
            onSelected: (String value) {
              if (value == 'replace') {
                onReplace();
              }

              if (value == 'remove') {
                onRemove();
              }
            },
            itemBuilder: (BuildContext context) =>
                const <PopupMenuEntry<String>>[
                  PopupMenuItem<String>(
                    value: 'replace',
                    child: Row(
                      children: <Widget>[
                        Icon(Icons.refresh_rounded),
                        SizedBox(width: 10),
                        Text('Replace'),
                      ],
                    ),
                  ),

                  PopupMenuItem<String>(
                    value: 'remove',
                    child: Row(
                      children: <Widget>[
                        Icon(Icons.delete_outline, color: _danger),
                        SizedBox(width: 10),
                        Text('Remove'),
                      ],
                    ),
                  ),
                ],
          ),
        ],
      ),
    );
  }

  Widget _buildSection({
    required String title,
    required String subtitle,
    required List<Widget> children,
  }) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(
            title,
            style: const TextStyle(
              color: _darkEmerald,
              fontSize: 17,
              fontWeight: FontWeight.w800,
            ),
          ),

          const SizedBox(height: 5),

          Text(
            subtitle,
            style: const TextStyle(color: _muted, fontSize: 11, height: 1.5),
          ),

          const SizedBox(height: 18),

          ...children,
        ],
      ),
    );
  }

  Widget _field({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    String? Function(String?)? validator,
    TextInputType? keyboardType,
    int maxLines = 1,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 15),
      child: TextFormField(
        controller: controller,
        validator: validator,
        keyboardType: keyboardType,
        maxLines: maxLines,
        textInputAction: maxLines > 1
            ? TextInputAction.newline
            : TextInputAction.next,
        decoration: InputDecoration(
          labelText: label,
          hintText: hint,
          prefixIcon: Icon(icon, color: _emerald),
          filled: true,
          fillColor: const Color(0xFFFCFBF8),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(13),
            borderSide: const BorderSide(color: _border),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(13),
            borderSide: const BorderSide(color: _border),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(13),
            borderSide: const BorderSide(color: _emerald, width: 1.5),
          ),
        ),
      ),
    );
  }

  Widget _currencyField() {
    return Padding(
      padding: const EdgeInsets.only(bottom: 15),
      child: DropdownButtonFormField<String>(
        initialValue: _currency,
        decoration: InputDecoration(
          labelText: 'Currency',
          filled: true,
          fillColor: const Color(0xFFFCFBF8),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(13),
            borderSide: const BorderSide(color: _border),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(13),
            borderSide: const BorderSide(color: _border),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(13),
            borderSide: const BorderSide(color: _emerald, width: 1.5),
          ),
        ),
        items: const <DropdownMenuItem<String>>[
          DropdownMenuItem<String>(value: 'LKR', child: Text('LKR')),
          DropdownMenuItem<String>(value: 'USD', child: Text('USD')),
        ],
        onChanged: (String? value) {
          if (value == null) {
            return;
          }

          setState(() {
            _currency = value;
          });
        },
      ),
    );
  }
}

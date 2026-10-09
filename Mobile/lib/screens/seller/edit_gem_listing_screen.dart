import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';

import '../../models/gem_listing_model.dart';
import '../../services/gem_listing_service.dart';

class EditGemListingScreen extends StatefulWidget {
  final GemListingModel listing;

  const EditGemListingScreen({super.key, required this.listing});

  @override
  State<EditGemListingScreen> createState() => _EditGemListingScreenState();
}

class _EditGemListingScreenState extends State<EditGemListingScreen> {
  final _formKey = GlobalKey<FormState>();

  final GemListingService _listingService = GemListingService();

  late final TextEditingController _titleController;
  late final TextEditingController _gemTypeController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _caratController;
  late final TextEditingController _colorController;
  late final TextEditingController _clarityController;
  late final TextEditingController _cutController;
  late final TextEditingController _priceController;
  late final TextEditingController _certificateNumberController;
  late final TextEditingController _certificateAuthorityController;

  late String _currency;

  XFile? _newGemImage;
  XFile? _newCertificate;

  bool _saving = false;

  static const Color _darkEmerald = Color(0xFF08251E);
  static const Color _emerald = Color(0xFF16483B);
  static const Color _gold = Color(0xFFC99242);
  static const Color _ivory = Color(0xFFFAF7F0);
  static const Color _border = Color(0xFFE7DFD2);
  static const Color _danger = Color(0xFFA5433A);
  static const Color _success = Color(0xFF247A50);

  @override
  void initState() {
    super.initState();

    final listing = widget.listing;

    _titleController = TextEditingController(text: listing.title);

    _gemTypeController = TextEditingController(text: listing.gemType);

    _descriptionController = TextEditingController(text: listing.description);

    _caratController = TextEditingController(
      text: listing.caratWeight.toString(),
    );

    _colorController = TextEditingController(text: listing.color);

    _clarityController = TextEditingController(text: listing.clarity);

    _cutController = TextEditingController(text: listing.cut);

    _priceController = TextEditingController(text: listing.price.toString());

    _certificateNumberController = TextEditingController(
      text: listing.certificateNumber ?? '',
    );

    _certificateAuthorityController = TextEditingController(
      text: listing.certificateAuthority ?? '',
    );

    _currency = listing.currency.trim().isEmpty ? 'LKR' : listing.currency;
  }

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

  String? _required(String? value, String field, {int min = 1}) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return '$field is required.';
    }

    if (text.length < min) {
      return '$field must contain at least $min characters.';
    }

    return null;
  }

  String? _positiveNumber(String? value, String field) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) {
      return '$field is required.';
    }

    final number = double.tryParse(text);

    if (number == null || number <= 0) {
      return '$field must be greater than 0.';
    }

    return null;
  }

  Future<void> _pickImage() async {
    const group = XTypeGroup(
      label: 'Gemstone images',
      extensions: ['jpg', 'jpeg', 'png', 'webp'],
    );

    final file = await openFile(acceptedTypeGroups: [group]);

    if (file == null) {
      return;
    }

    final size = await file.length();

    if (size > 5 * 1024 * 1024) {
      _showError('Gemstone image must be 5 MB or smaller.');
      return;
    }

    if (!mounted) {
      return;
    }

    setState(() {
      _newGemImage = file;
    });
  }

  Future<void> _pickCertificate() async {
    const group = XTypeGroup(
      label: 'Certificates',
      extensions: ['pdf', 'jpg', 'jpeg', 'png'],
    );

    final file = await openFile(acceptedTypeGroups: [group]);

    if (file == null) {
      return;
    }

    final size = await file.length();

    if (size > 10 * 1024 * 1024) {
      _showError('Certificate must be 10 MB or smaller.');
      return;
    }

    if (!mounted) {
      return;
    }

    setState(() {
      _newCertificate = file;
    });
  }

  Future<void> _save() async {
    if (_saving) {
      return;
    }

    FocusScope.of(context).unfocus();

    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _saving = true;
    });

    try {
      await _listingService.updateListing(
        id: widget.listing.id,
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

      if (_newGemImage != null) {
        await _listingService.uploadGemImage(
          id: widget.listing.id,
          filePath: _newGemImage!.path,
        );
      }

      if (_newCertificate != null) {
        await _listingService.uploadCertificate(
          id: widget.listing.id,
          filePath: _newCertificate!.path,
        );
      }

      final updated = await _listingService.getListing(widget.listing.id);

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Gem listing updated successfully.'),
          backgroundColor: _success,
        ),
      );

      Navigator.of(context).pop(updated);
    } catch (error) {
      _showError(error.toString().replaceFirst('Exception: ', ''));
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      appBar: AppBar(
        backgroundColor: _darkEmerald,
        foregroundColor: Colors.white,
        title: const Text('Edit Gem Listing'),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(18, 20, 18, 100),
          children: [
            _section('Listing Information', [
              _field(
                controller: _titleController,
                label: 'Listing Title',
                icon: Icons.title,
                validator: (value) => _required(value, 'Title', min: 3),
              ),
              _field(
                controller: _gemTypeController,
                label: 'Gem Type',
                icon: Icons.diamond_outlined,
                validator: (value) => _required(value, 'Gem type'),
              ),
              _field(
                controller: _descriptionController,
                label: 'Description',
                icon: Icons.description_outlined,
                maxLines: 5,
                validator: (value) => _required(value, 'Description', min: 10),
              ),
            ]),

            const SizedBox(height: 18),

            _section('Gem Characteristics', [
              _field(
                controller: _caratController,
                label: 'Carat Weight',
                icon: Icons.scale_outlined,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) => _positiveNumber(value, 'Carat weight'),
              ),
              _field(
                controller: _colorController,
                label: 'Color',
                icon: Icons.palette_outlined,
                validator: (value) => _required(value, 'Color'),
              ),
              _field(
                controller: _clarityController,
                label: 'Clarity',
                icon: Icons.visibility_outlined,
                validator: (value) => _required(value, 'Clarity'),
              ),
              _field(
                controller: _cutController,
                label: 'Cut',
                icon: Icons.auto_awesome_outlined,
                validator: (value) => _required(value, 'Cut'),
              ),
            ]),

            const SizedBox(height: 18),

            _section('Price', [
              _field(
                controller: _priceController,
                label: 'Price',
                icon: Icons.payments_outlined,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) => _positiveNumber(value, 'Price'),
              ),
              DropdownButtonFormField<String>(
                initialValue: _currency,
                decoration: _inputDecoration(
                  'Currency',
                  Icons.currency_exchange,
                ),
                items: const [
                  DropdownMenuItem(value: 'LKR', child: Text('LKR')),
                  DropdownMenuItem(value: 'USD', child: Text('USD')),
                ],
                onChanged: (value) {
                  if (value == null) {
                    return;
                  }

                  setState(() {
                    _currency = value;
                  });
                },
              ),
            ]),

            const SizedBox(height: 18),

            _section('Certificate', [
              _field(
                controller: _certificateNumberController,
                label: 'Certificate Number',
                icon: Icons.verified_outlined,
              ),
              _field(
                controller: _certificateAuthorityController,
                label: 'Certificate Authority',
                icon: Icons.account_balance_outlined,
              ),
            ]),

            const SizedBox(height: 18),

            _section('Replace Evidence', [
              _uploadButton(
                title: _newGemImage == null
                    ? 'Replace Gem Image'
                    : _newGemImage!.name,
                icon: Icons.add_photo_alternate_outlined,
                onPressed: _pickImage,
              ),

              const SizedBox(height: 12),

              _uploadButton(
                title: _newCertificate == null
                    ? 'Replace Certificate'
                    : _newCertificate!.name,
                icon: Icons.upload_file_outlined,
                onPressed: _pickCertificate,
              ),
            ]),

            const SizedBox(height: 24),

            SizedBox(
              height: 52,
              child: FilledButton.icon(
                onPressed: _saving ? null : _save,
                style: FilledButton.styleFrom(
                  backgroundColor: _gold,
                  foregroundColor: _darkEmerald,
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
                    : const Icon(Icons.save_outlined),
                label: Text(
                  _saving ? 'Saving...' : 'Save Changes',
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _section(String title, List<Widget> children) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              color: _darkEmerald,
              fontSize: 17,
              fontWeight: FontWeight.w800,
            ),
          ),

          const SizedBox(height: 16),

          ...children,
        ],
      ),
    );
  }

  Widget _field({
    required TextEditingController controller,
    required String label,
    required IconData icon,
    String? Function(String?)? validator,
    TextInputType? keyboardType,
    int maxLines = 1,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: TextFormField(
        controller: controller,
        validator: validator,
        keyboardType: keyboardType,
        maxLines: maxLines,
        decoration: _inputDecoration(label, icon),
      ),
    );
  }

  InputDecoration _inputDecoration(String label, IconData icon) {
    return InputDecoration(
      labelText: label,
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
    );
  }

  Widget _uploadButton({
    required String title,
    required IconData icon,
    required VoidCallback onPressed,
  }) {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: onPressed,
        icon: Icon(icon),
        label: Text(title, maxLines: 1, overflow: TextOverflow.ellipsis),
        style: OutlinedButton.styleFrom(
          foregroundColor: _emerald,
          side: const BorderSide(color: _emerald),
          padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 14),
        ),
      ),
    );
  }
}

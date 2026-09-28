import 'package:flutter/material.dart';

import '../../models/export_request_model.dart';
import '../../services/export_compliance_service.dart';

class EditExportRequestScreen extends StatefulWidget {
  final ExportRequestModel request;

  const EditExportRequestScreen({
    super.key,
    required this.request,
  });

  @override
  State<EditExportRequestScreen> createState() =>
      _EditExportRequestScreenState();
}

class _EditExportRequestScreenState extends State<EditExportRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final _exportService = ExportComplianceService();

  late TextEditingController _originController;
  late TextEditingController _destinationController;
  late TextEditingController _declaredValueController;
  late TextEditingController _currencyController;
  late TextEditingController _purposeController;

  bool _isSaving = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _originController =
        TextEditingController(text: widget.request.originCountry);
    _destinationController =
        TextEditingController(text: widget.request.destinationCountry);
    _declaredValueController =
        TextEditingController(text: widget.request.declaredValue.toString());
    _currencyController =
        TextEditingController(text: widget.request.currency);
    _purposeController =
        TextEditingController(text: widget.request.purpose ?? '');
  }

  @override
  void dispose() {
    _originController.dispose();
    _destinationController.dispose();
    _declaredValueController.dispose();
    _currencyController.dispose();
    _purposeController.dispose();
    super.dispose();
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate() || _isSaving) return;

    setState(() {
      _isSaving = true;
      _errorMessage = null;
    });

    final declaredValue = double.parse(_declaredValueController.text.trim());

    try {
      await _exportService.updateExportRequest(
        widget.request.id,
        originCountry: _originController.text,
        destinationCountry: _destinationController.text,
        declaredValue: declaredValue,
        currency: _currencyController.text,
        purpose: _purposeController.text,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Export request updated successfully.'),
          backgroundColor: Colors.green,
        ),
      );

      Navigator.of(context).pop(true);
    } catch (err) {
      setState(() {
        _errorMessage = err.toString().replaceAll('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _isSaving = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Edit Request Details'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_errorMessage != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.red.shade300),
                  ),
                  child: Text(
                    _errorMessage!,
                    style: TextStyle(color: Colors.red.shade800, fontSize: 14),
                  ),
                ),
                const SizedBox(height: 16),
              ],

              // ORIGIN COUNTRY
              TextFormField(
                controller: _originController,
                decoration: const InputDecoration(
                  labelText: 'Origin Country *',
                  hintText: 'e.g. Sri Lanka',
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Origin country is required.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // DESTINATION COUNTRY
              TextFormField(
                controller: _destinationController,
                decoration: const InputDecoration(
                  labelText: 'Destination Country *',
                  hintText: 'e.g. United States',
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Destination country is required.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // DECLARED VALUE
              TextFormField(
                controller: _declaredValueController,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  labelText: 'Declared Value *',
                  hintText: 'e.g. 5000.00',
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Enter a declared value greater than 0.';
                  }
                  final val = double.tryParse(value.trim());
                  if (val == null || val <= 0) {
                    return 'Enter a declared value greater than 0.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // CURRENCY
              TextFormField(
                controller: _currencyController,
                decoration: const InputDecoration(
                  labelText: 'Currency *',
                  hintText: 'e.g. USD',
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Currency is required.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // PURPOSE (OPTIONAL)
              TextFormField(
                controller: _purposeController,
                maxLines: 3,
                decoration: const InputDecoration(
                  labelText: 'Purpose (optional)',
                  hintText: 'e.g. Commercial sale of sapphires',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 24),

              // ACTION BUTTONS
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed:
                          _isSaving ? null : () => Navigator.of(context).pop(),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                      ),
                      child: const Text('Cancel'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed: _isSaving ? null : _submitForm,
                      style: FilledButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                      ),
                      child: _isSaving
                          ? const Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                ),
                                SizedBox(width: 10),
                                Text('Saving...'),
                              ],
                            )
                          : const Text(
                              'Save Changes',
                              style: TextStyle(fontSize: 16),
                            ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

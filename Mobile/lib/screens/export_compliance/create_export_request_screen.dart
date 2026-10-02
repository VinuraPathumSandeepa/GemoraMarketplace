import 'package:flutter/material.dart';

import '../../constants/compliance_constants.dart';
import '../../services/export_compliance_service.dart';
import 'export_request_detail_screen.dart';

class CreateExportRequestScreen extends StatefulWidget {
  const CreateExportRequestScreen({super.key});

  @override
  State<CreateExportRequestScreen> createState() =>
      _CreateExportRequestScreenState();
}

class _CreateExportRequestScreenState extends State<CreateExportRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final _exportService = ExportComplianceService();

  String _originCountry = 'Sri Lanka';
  String _destinationCountry = 'United States';
  final _declaredValueController = TextEditingController();
  final _currencyController = TextEditingController(text: 'USD');
  final _purposeController = TextEditingController();

  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _declaredValueController.dispose();
    _currencyController.dispose();
    _purposeController.dispose();
    super.dispose();
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate() || _isSubmitting) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    final declaredValue = double.parse(_declaredValueController.text.trim());

    try {
      final newRequest = await _exportService.createExportRequest(
        originCountry: _originCountry,
        destinationCountry: _destinationCountry,
        declaredValue: declaredValue,
        currency: _currencyController.text,
        purpose: _purposeController.text,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Export request created successfully.'),
          backgroundColor: Colors.green,
        ),
      );

      // Navigate to newly created request detail and pop create screen
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => ExportRequestDetailScreen(requestId: newRequest.id),
        ),
      );
    } catch (err) {
      setState(() {
        _errorMessage = err.toString().replaceAll('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Export Request'),
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

              // ORIGIN COUNTRY DROPDOWN
              DropdownButtonFormField<String>(
                initialValue: _originCountry,
                decoration: const InputDecoration(
                  labelText: 'Origin Country *',
                  border: OutlineInputBorder(),
                ),
                items: ComplianceConstants.supportedCountries.map((c) {
                  return DropdownMenuItem<String>(
                    value: c,
                    child: Text(c),
                  );
                }).toList(),
                onChanged: _isSubmitting
                    ? null
                    : (val) {
                        if (val != null) {
                          setState(() {
                            _originCountry = val;
                          });
                        }
                      },
                validator: (val) {
                  if (val == null || val.trim().isEmpty) {
                    return 'Origin country is required.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // DESTINATION COUNTRY DROPDOWN
              DropdownButtonFormField<String>(
                initialValue: _destinationCountry,
                decoration: const InputDecoration(
                  labelText: 'Destination Country *',
                  border: OutlineInputBorder(),
                ),
                items: ComplianceConstants.supportedCountries.map((c) {
                  return DropdownMenuItem<String>(
                    value: c,
                    child: Text(c),
                  );
                }).toList(),
                onChanged: _isSubmitting
                    ? null
                    : (val) {
                        if (val != null) {
                          setState(() {
                            _destinationCountry = val;
                          });
                        }
                      },
                validator: (val) {
                  if (val == null || val.trim().isEmpty) {
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

              // SUBMIT BUTTON
              FilledButton(
                onPressed: _isSubmitting ? null : _submitForm,
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                ),
                child: _isSubmitting
                    ? const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          ),
                          SizedBox(width: 12),
                          Text('Creating request...'),
                        ],
                      )
                    : const Text(
                        'Create Request',
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

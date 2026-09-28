import 'package:flutter/material.dart';
import '../services/shipment_service.dart';

class CreateShipmentScreen extends StatefulWidget {
  const CreateShipmentScreen({super.key});

  @override
  State<CreateShipmentScreen> createState() => _CreateShipmentScreenState();
}

class _CreateShipmentScreenState extends State<CreateShipmentScreen> {
  final ShipmentService _shipmentService = ShipmentService();
  final _formKey = GlobalKey<FormState>();

  // Form fields
  final _orderIdController = TextEditingController();
  final _originController = TextEditingController(text: 'Colombo, Sri Lanka');
  final _destinationController = TextEditingController();
  final _packageDescriptionController = TextEditingController();
  final _selectedServiceController = TextEditingController(text: 'Express Courier');
  final _courierNameController = TextEditingController(text: 'DHL Express');

  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _orderIdController.dispose();
    _originController.dispose();
    _destinationController.dispose();
    _packageDescriptionController.dispose();
    _selectedServiceController.dispose();
    _courierNameController.dispose();
    super.dispose();
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      await _shipmentService.createShipment(
        orderId: _orderIdController.text.trim(),
        origin: _originController.text.trim(),
        destination: _destinationController.text.trim(),
        packageDescription: _packageDescriptionController.text.trim(),
        selectedService: _selectedServiceController.text.trim(),
        courierName: _courierNameController.text.trim(),
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Shipment created successfully!'),
          backgroundColor: Colors.green,
        ),
      );

      Navigator.pop(context);
    } catch (e) {
      setState(() {
        _error = e.toString().replaceFirst('Exception: ', '');
        _submitting = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Shipment'),
      ),
      body: _submitting
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (_error != null)
                      Container(
                        padding: const EdgeInsets.all(12),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(
                          color: Colors.red.shade50,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.red.shade200),
                        ),
                        child: Row(
                          children: [
                            const Icon(Icons.error_outline, color: Colors.red),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                _error!,
                                style: const TextStyle(color: Colors.red),
                              ),
                            ),
                          ],
                        ),
                      ),

                    // Order ID
                    TextFormField(
                      controller: _orderIdController,
                      decoration: const InputDecoration(
                        labelText: 'Order ID',
                        hintText: 'Enter order ID (e.g., from your orders)',
                        prefixIcon: Icon(Icons.shopping_cart),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Order ID is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),

                    // Origin
                    TextFormField(
                      controller: _originController,
                      decoration: const InputDecoration(
                        labelText: 'Origin',
                        prefixIcon: Icon(Icons.location_on),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Origin is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),

                    // Destination
                    TextFormField(
                      controller: _destinationController,
                      decoration: const InputDecoration(
                        labelText: 'Destination',
                        hintText: 'e.g., Kandy, Sri Lanka',
                        prefixIcon: Icon(Icons.flag),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Destination is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),

                    // Package Description
                    TextFormField(
                      controller: _packageDescriptionController,
                      decoration: const InputDecoration(
                        labelText: 'Package Description',
                        hintText: 'e.g., 2.5 carat blue sapphire with certificate',
                        prefixIcon: Icon(Icons.description),
                      ),
                      maxLines: 3,
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Package description is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),

                    // Selected Service
                    DropdownButtonFormField<String>(
                      value: _selectedServiceController.text,
                      decoration: const InputDecoration(
                        labelText: 'Shipping Service',
                        prefixIcon: Icon(Icons.local_shipping),
                      ),
                      items: const [
                        DropdownMenuItem(value: 'Express Courier', child: Text('Express Courier')),
                        DropdownMenuItem(value: 'Standard Ground', child: Text('Standard Ground')),
                        DropdownMenuItem(value: 'Premium Overnight', child: Text('Premium Overnight')),
                      ],
                      onChanged: (value) {
                        if (value != null) {
                          setState(() {
                            _selectedServiceController.text = value;
                          });
                        }
                      },
                    ),
                    const SizedBox(height: 16),

                    // Courier Name
                    DropdownButtonFormField<String>(
                      value: _courierNameController.text,
                      decoration: const InputDecoration(
                        labelText: 'Courier',
                        prefixIcon: Icon(Icons.business),
                      ),
                      items: const [
                        DropdownMenuItem(value: 'DHL Express', child: Text('DHL Express')),
                        DropdownMenuItem(value: 'FedEx', child: Text('FedEx')),
                        DropdownMenuItem(value: 'Sri Lanka Post', child: Text('Sri Lanka Post')),
                      ],
                      onChanged: (value) {
                        if (value != null) {
                          setState(() {
                            _courierNameController.text = value;
                          });
                        }
                      },
                    ),
                    const SizedBox(height: 24),

                    // Submit Button
                    ElevatedButton.icon(
                      onPressed: _submitForm,
                      icon: const Icon(Icons.send),
                      label: const Text('Create Shipment'),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                      ),
                    ),

                    const SizedBox(height: 16),

                    // Info Card
                    Card(
                      color: Colors.blue.shade50,
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Icon(Icons.info_outline, color: Colors.blue.shade700),
                                const SizedBox(width: 8),
                                Text(
                                  'Important Notes',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: Colors.blue.shade700,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              '• Only paid orders can create shipments\n'
                              '• You can only ship orders you sold\n'
                              '• Declared value will be taken from the order\n'
                              '• One active shipment per order',
                              style: TextStyle(fontSize: 12),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}

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
  String? _selectedOrderId;
  List<dynamic> _eligibleOrders = [];
  bool _loadingOrders = true;
  String? _ordersError;
  
  final _originAddressController = TextEditingController(text: 'Colombo');
  final _originRegionController = TextEditingController(text: 'Western Province');
  final _originCountryCodeController = TextEditingController(text: 'LK');
  final _destinationAddressController = TextEditingController();
  final _destinationRegionController = TextEditingController();
  final _destinationCountryCodeController = TextEditingController();
  final _packageDescriptionController = TextEditingController();
  final _preferredServiceController = TextEditingController(text: 'Standard');
  final _specialHandlingNotesController = TextEditingController();
  bool _exportRequired = false;

  bool _submitting = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadEligibleOrders();
  }

  Future<void> _loadEligibleOrders() async {
    setState(() {
      _loadingOrders = true;
      _ordersError = null;
    });

    try {
      final orders = await _shipmentService.getShipmentEligibleOrders();
      
      if (!mounted) return;
      
      setState(() {
        _eligibleOrders = orders;
        _loadingOrders = false;
        
        // Auto-select first order if available
        if (orders.isNotEmpty) {
          _selectedOrderId = orders[0]['id'].toString();
          // Auto-populate destination from first order
          _destinationAddressController.text = orders[0]['shippingAddress'] ?? '';
          _destinationRegionController.text = orders[0]['shippingRegion'] ?? '';
          _destinationCountryCodeController.text = orders[0]['shippingCountryCode'] ?? 'LK';
        }
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _ordersError = e.toString().replaceFirst('Exception: ', '');
        _loadingOrders = false;
      });
    }
  }

  @override
  void dispose() {
    _originAddressController.dispose();
    _originRegionController.dispose();
    _originCountryCodeController.dispose();
    _destinationAddressController.dispose();
    _destinationRegionController.dispose();
    _destinationCountryCodeController.dispose();
    _packageDescriptionController.dispose();
    _preferredServiceController.dispose();
    _specialHandlingNotesController.dispose();
    super.dispose();
  }

  String _formatOrder(dynamic order) {
    final id = order['id'].toString();
    final shortId = id.length > 8 ? '${id.substring(0, 8)}...' : id;
    final amount = order['totalAmount'];
    final currency = order['currency'];
    final region = order['shippingRegion'] ?? 'Unknown';
    
    return '$shortId - $currency$amount ($region)';
  }

  Future<void> _onOrderSelected(String? orderId) async {
    if (orderId == null) return;
    
    setState(() {
      _selectedOrderId = orderId;
    });
    
    // Find selected order and auto-populate fields
    final order = _eligibleOrders.firstWhere(
      (o) => o['id'].toString() == orderId,
      orElse: () => null,
    );
    
    if (order != null && mounted) {
      setState(() {
        _destinationAddressController.text = order['shippingAddress'] ?? '';
        _destinationRegionController.text = order['shippingRegion'] ?? '';
        _destinationCountryCodeController.text = order['shippingCountryCode'] ?? 'LK';
      });
    }
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate()) return;
    
    if (_selectedOrderId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please select an order'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      await _shipmentService.createShipment(
        orderId: _selectedOrderId!,
        originAddress: _originAddressController.text.trim(),
        originRegion: _originRegionController.text.trim(),
        originCountryCode: _originCountryCodeController.text.trim().toUpperCase(),
        destinationAddress: _destinationAddressController.text.trim(),
        destinationRegion: _destinationRegionController.text.trim(),
        destinationCountryCode: _destinationCountryCodeController.text.trim().toUpperCase(),
        packageDescription: _packageDescriptionController.text.trim(),
        preferredService: _preferredServiceController.text.trim().isEmpty 
            ? 'Standard' 
            : _preferredServiceController.text.trim(),
        specialHandlingNotes: _specialHandlingNotesController.text.trim(),
        exportRequired: _exportRequired,
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
      body: _submitting || _loadingOrders
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const CircularProgressIndicator(),
                  const SizedBox(height: 16),
                  Text(_loadingOrders ? 'Loading eligible orders...' : 'Creating shipment...'),
                ],
              ),
            )
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

                    // Orders Error (if failed to load)
                    if (_ordersError != null && !_loadingOrders)
                      Container(
                        padding: const EdgeInsets.all(12),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(
                          color: Colors.orange.shade50,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.orange.shade200),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.warning_amber, color: Colors.orange.shade700),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                _ordersError!,
                                style: TextStyle(color: Colors.orange.shade700),
                              ),
                            ),
                            IconButton(
                              icon: const Icon(Icons.refresh),
                              onPressed: _loadEligibleOrders,
                              tooltip: 'Retry',
                            ),
                          ],
                        ),
                      ),

                    // Order Dropdown
                    if (!_loadingOrders && _eligibleOrders.isEmpty)
                      Container(
                        padding: const EdgeInsets.all(16),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(
                          color: Colors.grey.shade100,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.grey.shade300),
                        ),
                        child: Column(
                          children: [
                            Icon(Icons.info_outline, size: 48, color: Colors.grey.shade600),
                            const SizedBox(height: 12),
                            Text(
                              'No eligible paid orders are available for shipment.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 14,
                                color: Colors.grey.shade700,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                            const SizedBox(height: 12),
                            ElevatedButton.icon(
                              onPressed: _loadEligibleOrders,
                              icon: const Icon(Icons.refresh),
                              label: const Text('Refresh'),
                            ),
                          ],
                        ),
                      )
                    else if (!_loadingOrders)
                      DropdownButtonFormField<String>(
                        value: _selectedOrderId,
                        decoration: const InputDecoration(
                          labelText: 'Select Order',
                          hintText: 'Choose an order to ship',
                          prefixIcon: Icon(Icons.shopping_cart),
                        ),
                        items: _eligibleOrders.map((order) {
                          return DropdownMenuItem<String>(
                            value: order['id'].toString(),
                            child: Text(
                              _formatOrder(order),
                              overflow: TextOverflow.ellipsis,
                            ),
                          );
                        }).toList(),
                        onChanged: _onOrderSelected,
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Please select an order';
                          }
                          return null;
                        },
                      ),
                    const SizedBox(height: 16),

                    // Origin Address
                    TextFormField(
                      controller: _originAddressController,
                      decoration: const InputDecoration(
                        labelText: 'Origin Address',
                        hintText: 'e.g., Colombo',
                        prefixIcon: Icon(Icons.location_on),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Origin address is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 12),

                    // Origin Region
                    TextFormField(
                      controller: _originRegionController,
                      decoration: const InputDecoration(
                        labelText: 'Origin Region',
                        hintText: 'e.g., Western Province',
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Origin region is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 12),

                    // Origin Country Code
                    TextFormField(
                      controller: _originCountryCodeController,
                      decoration: const InputDecoration(
                        labelText: 'Origin Country Code',
                        hintText: 'e.g., LK',
                      ),
                      maxLength: 2,
                      textCapitalization: TextCapitalization.characters,
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Origin country code is required';
                        }
                        if (value.trim().length != 2) {
                          return 'Country code must be 2 letters';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),

                    // Destination Address
                    TextFormField(
                      controller: _destinationAddressController,
                      decoration: const InputDecoration(
                        labelText: 'Destination Address',
                        hintText: 'e.g., Kandy',
                        prefixIcon: Icon(Icons.flag),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Destination address is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 12),

                    // Destination Region
                    TextFormField(
                      controller: _destinationRegionController,
                      decoration: const InputDecoration(
                        labelText: 'Destination Region',
                        hintText: 'e.g., Central Province',
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Destination region is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 12),

                    // Destination Country Code
                    TextFormField(
                      controller: _destinationCountryCodeController,
                      decoration: const InputDecoration(
                        labelText: 'Destination Country Code',
                        hintText: 'e.g., LK',
                      ),
                      maxLength: 2,
                      textCapitalization: TextCapitalization.characters,
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Destination country code is required';
                        }
                        if (value.trim().length != 2) {
                          return 'Country code must be 2 letters';
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

                    // Preferred Service
                    DropdownButtonFormField<String>(
                      value: _preferredServiceController.text.isEmpty 
                          ? 'Standard' 
                          : _preferredServiceController.text,
                      decoration: const InputDecoration(
                        labelText: 'Shipping Service',
                        prefixIcon: Icon(Icons.local_shipping),
                      ),
                      items: const [
                        DropdownMenuItem(value: 'Standard', child: Text('Standard')),
                        DropdownMenuItem(value: 'Express Insured', child: Text('Express Insured')),
                        DropdownMenuItem(value: 'Priority Insured', child: Text('Priority Insured')),
                        DropdownMenuItem(value: 'International Priority', child: Text('International Priority')),
                      ],
                      onChanged: (value) {
                        if (value != null) {
                          setState(() {
                            _preferredServiceController.text = value;
                          });
                        }
                      },
                    ),
                    const SizedBox(height: 16),

                    // Special Handling Notes
                    TextFormField(
                      controller: _specialHandlingNotesController,
                      decoration: const InputDecoration(
                        labelText: 'Special Handling Notes (Optional)',
                        hintText: 'e.g., Fragile - handle with care',
                        prefixIcon: Icon(Icons.note_add),
                      ),
                      maxLines: 2,
                    ),
                    const SizedBox(height: 16),

                    // Export Required Checkbox
                    CheckboxListTile(
                      title: const Text('Export Required'),
                      subtitle: const Text('Check if international export documentation is needed'),
                      value: _exportRequired,
                      onChanged: (value) {
                        setState(() {
                          _exportRequired = value ?? false;
                        });
                      },
                      controlAffinity: ListTileControlAffinity.leading,
                    ),
                    const SizedBox(height: 24),

                    // Submit Button
                    ElevatedButton.icon(
                      onPressed: _eligibleOrders.isEmpty ? null : _submitForm,
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

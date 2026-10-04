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
  
  final _originController = TextEditingController(text: 'Colombo, Sri Lanka');
  final _destinationController = TextEditingController();
  final _packageDescriptionController = TextEditingController();
  final _selectedServiceController = TextEditingController(text: 'Express Courier');
  final _courierNameController = TextEditingController(text: 'DHL Express');

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
          _destinationController.text = '${orders[0]['shippingAddress']}, ${orders[0]['shippingRegion']}';
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
    _originController.dispose();
    _destinationController.dispose();
    _packageDescriptionController.dispose();
    _selectedServiceController.dispose();
    _courierNameController.dispose();
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
        _destinationController.text = '${order['shippingAddress']}, ${order['shippingRegion']}';
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

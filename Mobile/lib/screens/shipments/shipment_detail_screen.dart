import 'package:flutter/material.dart';
import '../services/shipment_service.dart';

class ShipmentDetailScreen extends StatefulWidget {
  final String shipmentId;

  const ShipmentDetailScreen({super.key, required this.shipmentId});

  @override
  State<ShipmentDetailScreen> createState() => _ShipmentDetailScreenState();
}

class _ShipmentDetailScreenState extends State<ShipmentDetailScreen> {
  final ShipmentService _shipmentService = ShipmentService();

  Map<String, dynamic>? _shipment;
  List<dynamic> _trackingEvents = [];
  Map<String, dynamic>? _insuranceRecord;
  Map<String, dynamic>? _shippingPlan;

  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadDetails();
  }

  Future<void> _loadDetails() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final shipment = await _shipmentService.getShipmentById(widget.shipmentId);
      final trackingEvents = await _shipmentService.getTrackingEvents(widget.shipmentId).catchError((_) => []);
      final insuranceRecord = await _shipmentService.getInsuranceRecord(widget.shipmentId);
      final shippingPlan = await _shipmentService.getShippingPlan(widget.shipmentId);

      if (!mounted) return;

      setState(() {
        _shipment = shipment;
        _trackingEvents = trackingEvents as List<dynamic>;
        _insuranceRecord = insuranceRecord;
        _shippingPlan = shippingPlan;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  String _getStatusText(String status) {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'Pending';
      case 'planning':
        return 'Planning';
      case 'readyforbooking':
        return 'Ready for Booking';
      case 'booked':
        return 'Booked';
      case 'pickedup':
        return 'Picked Up';
      case 'intransit':
        return 'In Transit';
      case 'customshold':
        return 'Customs Hold';
      case 'outfordelivery':
        return 'Out for Delivery';
      case 'delivered':
        return 'Delivered';
      case 'deliveryfailed':
        return 'Delivery Failed';
      case 'cancelled':
        return 'Cancelled';
      case 'exception':
        return 'Exception';
      default:
        return status;
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'pending':
      case 'planning':
        return Colors.orange;
      case 'readyforbooking':
      case 'booked':
      case 'pickedup':
        return Colors.blue;
      case 'intransit':
      case 'outfordelivery':
        return Colors.purple;
      case 'delivered':
        return Colors.green;
      case 'deliveryfailed':
      case 'cancelled':
      case 'exception':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_shipment?['shipmentNumber'] ?? 'Shipment Details'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadDetails,
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.error_outline, size: 48, color: Colors.red),
                      const SizedBox(height: 16),
                      Text(_error!, style: const TextStyle(color: Colors.red)),
                      const SizedBox(height: 16),
                      ElevatedButton(onPressed: _loadDetails, child: const Text('Retry')),
                    ],
                  ),
                )
              : DefaultTabController(
                  length: 3,
                  child: Column(
                    children: [
                      TabBar(
                        tabs: const [
                          Tab(text: 'Details', icon: Icon(Icons.info_outline)),
                          Tab(text: 'Tracking', icon: Icon(Icons.timeline)),
                          Tab(text: 'Insurance', icon: Icon(Icons.security)),
                        ],
                      ),
                      Expanded(
                        child: TabBarView(
                          children: [
                            _buildDetailsTab(),
                            _buildTrackingTab(),
                            _buildInsuranceTab(),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }

  Widget _buildDetailsTab() {
    if (_shipment == null) return const Center(child: Text('No data'));

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Status Card
          Card(
            color: _getStatusColor(_shipment!['status']).withOpacity(0.1),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Icon(Icons.local_shipping, color: _getStatusColor(_shipment!['status']), size: 32),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Status', style: TextStyle(fontSize: 12, color: Colors.grey)),
                        Text(
                          _getStatusText(_shipment!['status']),
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                            color: _getStatusColor(_shipment!['status']),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Information
          _buildInfoCard('Shipment Information', [
            _buildInfoRow('Shipment #', 'Shipment ${(widget.shipmentId.length > 8 ? widget.shipmentId.substring(0, 8) : widget.shipmentId)}'),
            _buildInfoRow('Order ID', '${_shipment!['orderId'].toString().substring(0, 8)}...'),
            _buildInfoRow('Origin', '${_shipment!['originAddress'] ?? ''}, ${_shipment!['originRegion'] ?? ''}'),
            _buildInfoRow('Destination', '${_shipment!['destinationAddress'] ?? ''}, ${_shipment!['destinationRegion'] ?? ''}'),
            _buildInfoRow('Declared Value', '${_shipment!['currency']} ${_shipment!['declaredValue']}'),
            _buildInfoRow('Service', _shipment!['selectedService'] ?? 'Not selected'),
            _buildInfoRow('Courier', _shipment!['courierName'] ?? 'Not assigned'),
            _buildInfoRow('Tracking #', _shipment!['trackingNumber'] ?? 'Not assigned'),
          ]),

          if (_shippingPlan != null) ...[
            const SizedBox(height: 16),
            _buildInfoCard('Shipping Plan', [
              _buildInfoRow('Risk Level', _shippingPlan!['riskLevel']),
              _buildInfoRow('Service Type', _shippingPlan!['recommendedServiceType']),
              _buildInfoRow(
                'Insurance',
                _shippingPlan!['insuranceRecommended'] ? 'Recommended' : 'Not Required',
              ),
              if (_shippingPlan!['insuranceRecommended'])
                _buildInfoRow(
                  'Coverage',
                  '${_shipment!['currency']} ${_shippingPlan!['recommendedCoverageAmount'] ?? 'N/A'}',
                ),
              _buildInfoRow('Approved', _shippingPlan!['isApproved'] ? 'Yes' : 'No'),
            ]),
          ],
        ],
      ),
    );
  }

  Widget _buildTrackingTab() {
    if (_trackingEvents.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.timeline_outlined, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            const Text('No tracking events yet', style: TextStyle(fontSize: 18, color: Colors.grey)),
          ],
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: _trackingEvents.length,
      itemBuilder: (context, index) {
        final event = _trackingEvents[index];
        final occurredAt = DateTime.parse(event['occurredAt']);

        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: ListTile(
            leading: CircleAvatar(
              backgroundColor: Colors.blue.shade100,
              child: Icon(Icons.event, color: Colors.blue.shade700),
            ),
            title: Text(event['eventType'] ?? 'Unknown Event', style: const TextStyle(fontWeight: FontWeight.bold)),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(event['description'] ?? ''),
                const SizedBox(height: 4),
                Text('Location: ${event['location'] ?? 'Unknown'}'),
                const SizedBox(height: 4),
                Text(
                  'Occurred: ${occurredAt.toString()}',
                  style: const TextStyle(fontSize: 12, color: Colors.grey),
                ),
              ],
            ),
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SizedBox(height: 4),
                Text(event['description']),
                if (event['locationText'] != null && event['locationText'].isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Icon(Icons.location_on, size: 16, color: Colors.grey),
                      const SizedBox(width: 4),
                      Text(event['locationText'], style: const TextStyle(color: Colors.grey)),
                    ],
                  ),
                ],
                const SizedBox(height: 4),
                Text(
                  '${occurredAt.day}/${occurredAt.month}/${occurredAt.year} ${occurredAt.hour}:${occurredAt.minute.toString().padLeft(2, '0')}',
                  style: const TextStyle(fontSize: 12, color: Colors.grey),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildInsuranceTab() {
    if (_insuranceRecord == null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.security_outlined, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            const Text('No insurance records', style: TextStyle(fontSize: 18, color: Colors.grey)),
            const SizedBox(height: 8),
            const Text('Contact admin to add insurance', style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(Icons.security, color: Colors.green.shade700, size: 28),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _insuranceRecord!['providerName'] ?? 'DEMO Gemora Insurance Sandbox',
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        Text(
                          'Policy: ${_insuranceRecord!['policyReference'] ?? 'N/A'}',
                          style: const TextStyle(color: Colors.grey),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.green.shade50,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.green.shade200),
                    ),
                    child: Text(
                      _insuranceRecord!['status'],
                      style: TextStyle(color: Colors.green.shade700, fontWeight: FontWeight.bold),
                    ),
                  ),
                ],
              ),
              const Divider(height: 24),
              _buildInfoRow('Declared Value', '${_insuranceRecord!['currency']} ${_insuranceRecord!['declaredValue']}'),
              _buildInfoRow('Coverage Amount', '${_insuranceRecord!['currency']} ${_insuranceRecord!['coverageAmount']}'),
              _buildInfoRow('Coverage Type', _insuranceRecord!['coverageType']),
              _buildInfoRow('Premium', '${_insuranceRecord!['currency']} ${_insuranceRecord!['premiumAmount']}'),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoCard(String title, List<Widget> children) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const Divider(height: 24),
            ...children,
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
            child: Text(label, style: const TextStyle(color: Colors.grey, fontSize: 12)),
          ),
          Expanded(
            child: Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
          ),
        ],
      ),
    );
  }
}

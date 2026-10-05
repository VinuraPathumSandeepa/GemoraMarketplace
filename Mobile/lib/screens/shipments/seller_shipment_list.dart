import 'package:flutter/material.dart';
import '../services/shipment_service.dart';
import 'create_shipment_screen.dart';
import 'shipment_detail_screen.dart';

class SellerShipmentList extends StatefulWidget {
  const SellerShipmentList({super.key});

  @override
  State<SellerShipmentList> createState() => _SellerShipmentListState();
}

class _SellerShipmentListState extends State<SellerShipmentList> {
  final ShipmentService _shipmentService = ShipmentService();
  List<dynamic> _shipments = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadShipments();
  }

  Future<void> _loadShipments() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final shipments = await _shipmentService.getMyShipments();
      setState(() {
        _shipments = shipments;
        _loading = false;
      });
    } catch (e) {
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
        title: const Text('My Shipments'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadShipments,
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
                      ElevatedButton(
                        onPressed: _loadShipments,
                        child: const Text('Retry'),
                      ),
                    ],
                  ),
                )
              : _shipments.isEmpty
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(Icons.local_shipping_outlined, size: 64, color: Colors.grey),
                          const SizedBox(height: 16),
                          const Text('No shipments yet', style: TextStyle(fontSize: 18, color: Colors.grey)),
                          const SizedBox(height: 8),
                          const Text('Create a shipment for your sold orders', style: TextStyle(color: Colors.grey)),
                        ],
                      ),
                    )
                  : RefreshIndicator(
                      onRefresh: _loadShipments,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16),
                        itemCount: _shipments.length,
                        itemBuilder: (context, index) {
                          final shipment = _shipments[index];
                          final status = shipment['status'] ?? 'Unknown';
                          return Card(
                            margin: const EdgeInsets.only(bottom: 12),
                            child: ListTile(
                              leading: Icon(
                                Icons.local_shipping,
                                color: _getStatusColor(status),
                              ),
                              title: Text(
                                'Shipment #${(shipment['id'] ?? '').toString().substring(0, 8)}',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const SizedBox(height: 4),
                                  Text('${shipment['originAddress'] ?? ''}, ${shipment['originRegion'] ?? ''} → ${shipment['destinationAddress'] ?? ''}, ${shipment['destinationRegion'] ?? ''}'),
                                  const SizedBox(height: 4),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: _getStatusColor(status).withOpacity(0.1),
                                      borderRadius: BorderRadius.circular(12),
                                    ),
                                    child: Text(
                                      _getStatusText(status),
                                      style: TextStyle(
                                        color: _getStatusColor(status),
                                        fontSize: 12,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              trailing: const Icon(Icons.chevron_right),
                              onTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => ShipmentDetailScreen(
                                      shipmentId: shipment['id'],
                                    ),
                                  ),
                                );
                              },
                            ),
                          );
                        },
                      ),
                    ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => const CreateShipmentScreen(),
            ),
          ).then((_) => _loadShipments());
        },
        icon: const Icon(Icons.add),
        label: const Text('New Shipment'),
      ),
    );
  }
}

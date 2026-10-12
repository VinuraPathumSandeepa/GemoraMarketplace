import 'package:flutter/material.dart';

import '../../models/shipment_model.dart';
import '../../services/shipment_service.dart';
import 'shipment_widgets.dart';

class ShipmentDetailScreen extends StatefulWidget {
  final String shipmentId;
  final ShipmentService? service;
  const ShipmentDetailScreen({
    super.key,
    required this.shipmentId,
    this.service,
  });
  @override
  State<ShipmentDetailScreen> createState() => _ShipmentDetailScreenState();
}

class _ShipmentDetailScreenState extends State<ShipmentDetailScreen> {
  late final ShipmentService _service = widget.service ?? ShipmentService();
  ShipmentModel? _shipment;
  Map<String, dynamic>? _plan, _insurance;
  List<dynamic> _events = [];
  bool _loading = true, _generating = false;
  String? _error, _planError, _insuranceError, _trackingError;
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    if (widget.service == null) _service.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
      _planError = null;
      _insuranceError = null;
      _trackingError = null;
    });
    try {
      final shipment = await _service.getShipmentById(widget.shipmentId);
      Map<String, dynamic>? plan, insurance;
      List<dynamic> events = [];
      String? planError, insuranceError, trackingError;
      await Future.wait([
        (() async {
          try {
            plan = await _service.getShippingPlan(widget.shipmentId);
          } catch (e) {
            planError = shippingError(e);
          }
        })(),
        (() async {
          try {
            insurance = await _service.getInsuranceRecord(widget.shipmentId);
          } catch (e) {
            insuranceError = shippingError(e);
          }
        })(),
        (() async {
          try {
            events = await _service.getTrackingEvents(widget.shipmentId);
          } catch (e) {
            trackingError = shippingError(e);
          }
        })(),
      ]);
      if (!mounted) return;
      setState(() {
        _shipment = ShipmentModel.fromJson(shipment);
        _plan = plan;
        _insurance = insurance;
        _events = events;
        _planError = planError;
        _insuranceError = insuranceError;
        _trackingError = trackingError;
        _loading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = shippingError(e);
          _loading = false;
        });
      }
    }
  }

  Future<void> _generate() async {
    if (_generating) return;
    setState(() {
      _generating = true;
      _planError = null;
    });
    try {
      await _service.generateShippingPlan(widget.shipmentId);
      if (mounted) await _load();
    } catch (e) {
      if (mounted) setState(() => _planError = shippingError(e));
    } finally {
      if (mounted) setState(() => _generating = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: shippingCream,
    appBar: AppBar(
      title: const Text('Shipment Details'),
      backgroundColor: shippingGreen,
      foregroundColor: Colors.white,
      actions: [
        IconButton(
          tooltip: 'Refresh',
          onPressed: _generating ? null : _load,
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    body: SafeArea(
      child: RefreshIndicator(
        onRefresh: _load,
        child: shippingBody([
          const ShippingHeader(
            label: "YOUR GEMSTONE'S JOURNEY",
            title: 'Shipment Details',
            description: 'Review your delivery information, shipping recommendations, insurance, and tracking updates.',
          ),
          if (_loading)
            const Center(child: CircularProgressIndicator())
          else if (_error != null)
            ShippingFeedback(_error!, retry: _load)
          else if (_shipment != null)
            ..._details(),
        ]),
      ),
    ),
  );
  List<Widget> _details() {
    final s = _shipment!;
    final data = s.data;
    return [
      ShippingSection(
        title: 'Shipment Information',
        children: [
          Align(
            alignment: Alignment.centerLeft,
            child: ShipmentStatusBadge(s.status),
          ),
          const SizedBox(height: 16),
          ShippingValue('Shipment ID', s.id),
          ShippingValue('Order ID', s.orderId),
          ShippingValue('Created', shippingDate(data['createdAt'])),
          ShippingValue('Declared Value', shippingMoney(s.value, s.currency)),
          ShippingValue('Origin', s.route('origin')),
          ShippingValue('Destination', s.route('destination')),
          ShippingValue('Preferred Service', data['preferredService']),
          ShippingValue('Selected Service', data['selectedService']),
          ShippingValue('Courier', data['courierName']),
          ShippingValue('Tracking Number', data['trackingNumber']),
          ShippingValue('Tracking URL', data['trackingUrl']),
          if (data['bookedAt'] != null)
            ShippingValue('Booked', shippingDate(data['bookedAt'])),
          if (data['shippedAt'] != null)
            ShippingValue('Shipped', shippingDate(data['shippedAt'])),
          if (data['deliveredAt'] != null)
            ShippingValue('Delivered', shippingDate(data['deliveredAt'])),
        ],
      ),
      ShippingSection(
        title: 'Package Details',
        children: [
          ShippingValue('Description', data['packageDescription']),
          ShippingValue('Weight (kg)', data['packageWeight']),
          ShippingValue('Dimensions', data['packageDimensions']),
          ShippingValue('Special Handling Notes', data['specialHandlingNotes']),
          ShippingValue(
            'Export Documentation Required',
            data['exportRequired'] == true ? 'Yes' : 'No',
          ),
        ],
      ),
      ShippingSection(
        title: 'Shipping Plan',
        children: [
          if (_planError != null) ShippingFeedback(_planError!, retry: _load),
          if (_plan != null) ...[
            ShippingValue('Risk Level', _plan!['riskLevel']),
            ShippingValue('Risk Reasons', _plan!['riskReasons']),
            ShippingValue(
              'Recommended Service',
              _plan!['recommendedServiceType'],
            ),
            ShippingValue(
              'Insurance Recommended',
              _plan!['insuranceRecommended'] == true ? 'Yes' : 'No',
            ),
            ShippingValue(
              'Recommended Coverage',
              shippingMoney(_plan!['recommendedCoverageAmount'], s.currency),
            ),
            ShippingValue(
              'Handling Requirements',
              _plan!['handlingRequirements'],
            ),
            ShippingValue('Required Documents', _plan!['requiredDocuments']),
            ShippingValue('Warnings', _plan!['warnings']),
            ShippingValue(
              'Approval Status',
              _plan!['isApproved'] == true
                  ? 'Approved by administrator'
                  : 'Pending Admin Approval',
            ),
            if (_plan!['approvedAt'] != null)
              ShippingValue('Approved', shippingDate(_plan!['approvedAt'])),
          ] else if (_planError == null)
            const Text('No shipping plan has been generated yet.'),
          if (_plan == null) ...[
            const SizedBox(height: 12),
            FilledButton(
              onPressed: _generating ? null : _generate,
              style: FilledButton.styleFrom(backgroundColor: shippingGreen),
              child: Text(
                _generating ? 'Generating plan…' : 'Generate Shipping Plan',
              ),
            ),
          ],
        ],
      ),
      ShippingSection(
        title: 'Insurance',
        children: [
          if (_insuranceError != null)
            ShippingFeedback(_insuranceError!, retry: _load)
          else if (_insurance == null)
            const Text('No insurance record yet.')
          else ...[
            ShippingValue('Provider', _insurance!['providerName']),
            ShippingValue('Policy Number', _insurance!['policyNumber']),
            ShippingValue('Policy Reference', _insurance!['policyReference']),
            ShippingValue('Status', _insurance!['status']),
            ShippingValue(
              'Coverage',
              shippingMoney(
                _insurance!['coverageAmount'],
                _insurance!['currency']?.toString() ?? s.currency,
              ),
            ),
            ShippingValue('Coverage Type', _insurance!['coverageType']),
            ShippingValue(
              'Premium',
              shippingMoney(
                _insurance!['premiumAmount'],
                _insurance!['currency']?.toString() ?? s.currency,
              ),
            ),
            if (_insurance!['policyStartDate'] != null)
              ShippingValue(
                'Policy Start',
                shippingDate(_insurance!['policyStartDate']),
              ),
            if (_insurance!['policyEndDate'] != null)
              ShippingValue(
                'Policy End',
                shippingDate(_insurance!['policyEndDate']),
              ),
          ],
        ],
      ),
      ShippingSection(
        title: 'Tracking Timeline',
        children: [
          if (_trackingError != null)
            ShippingFeedback(_trackingError!, retry: _load)
          else if (_events.isEmpty)
            const Text('No tracking events yet.')
          else
            ..._events.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 18),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Icon(
                      Icons.local_shipping_outlined,
                      color: shippingGold,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            e['eventType']?.toString() ?? '',
                            style: const TextStyle(fontWeight: FontWeight.bold),
                          ),
                          Text(e['description']?.toString() ?? ''),
                          Text(e['location']?.toString() ?? ''),
                          Text(shippingDate(e['occurredAt'])),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    ];
  }
}

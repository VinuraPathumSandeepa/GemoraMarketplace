import 'package:flutter/material.dart';

import '../../models/shipment_model.dart';
import '../../services/shipment_service.dart';
import 'shipment_detail_screen.dart';
import 'shipment_widgets.dart';

class CreateShipmentScreen extends StatefulWidget {
  final ShipmentService? service;
  const CreateShipmentScreen({super.key, this.service});
  @override
  State<CreateShipmentScreen> createState() => _CreateShipmentScreenState();
}

class _CreateShipmentScreenState extends State<CreateShipmentScreen> {
  late final ShipmentService _service = widget.service ?? ShipmentService();
  final _form = GlobalKey<FormState>();
  final _fields = {
    for (final key in [
      'originAddress',
      'originRegion',
      'originCountryCode',
      'destinationAddress',
      'destinationRegion',
      'destinationCountryCode',
      'packageDescription',
      'packageWeight',
      'packageDimensions',
      'specialHandlingNotes',
    ])
      key: TextEditingController(),
  };
  List<ShipmentOrder> _orders = [];
  ShipmentOrder? _order;
  bool _loading = true, _saving = false, _export = false;
  String _preferred = 'Standard';
  String? _orderError, _error;
  @override
  void initState() {
    super.initState();
    _loadOrders();
  }

  @override
  void dispose() {
    for (final c in _fields.values) {
      c.dispose();
    }
    if (widget.service == null) _service.dispose();
    super.dispose();
  }

  Future<void> _loadOrders() async {
    setState(() {
      _loading = true;
      _orderError = null;
    });
    try {
      final rows = await _service.getShipmentEligibleOrders();
      if (mounted) {
        setState(() {
          _orders = rows
              .map((r) => ShipmentOrder.fromJson(Map<String, dynamic>.from(r)))
              .toList();
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _orderError = shippingError(e);
          _loading = false;
        });
      }
    }
  }

  void _select(String? id) {
    if (id == null) return;
    final order = _orders.firstWhere((o) => o.id == id);
    setState(() {
      _order = order;
      _fields['destinationAddress']!.text = order.text('shippingAddress');
      _fields['destinationRegion']!.text = order.text('shippingRegion');
      _fields['destinationCountryCode']!.text = order.text(
        'shippingCountryCode',
      );
    });
  }

  Widget _field(
    String key,
    String label, {
    bool required = true,
    int max = 500,
    int lines = 1,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: 16),
    child: TextFormField(
      controller: _fields[key],
      enabled: !_saving,
      maxLines: lines,
      keyboardType: key == 'packageWeight'
          ? const TextInputType.numberWithOptions(decimal: true)
          : lines > 1
          ? TextInputType.multiline
          : TextInputType.text,
      textCapitalization: key.endsWith('CountryCode')
          ? TextCapitalization.characters
          : TextCapitalization.sentences,
      decoration: InputDecoration(
        labelText: '$label${required ? ' *' : ''}',
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
        filled: true,
        fillColor: shippingCream.withValues(alpha: .4),
      ),
      validator: (v) {
        if (key.endsWith('CountryCode')) return ShipmentValidation.country(v);
        if (key == 'packageWeight') return ShipmentValidation.weight(v);
        if (required) return ShipmentValidation.requiredText(v, max: max);
        return (v?.trim().length ?? 0) > max
            ? 'Use $max characters or fewer.'
            : null;
      },
    ),
  );
  Future<void> _save() async {
    if (_saving || !_form.currentState!.validate() || _order == null) return;
    FocusScope.of(context).unfocus();
    setState(() {
      _saving = true;
      _error = null;
    });
    String value(String key) => _fields[key]!.text.trim();
    try {
      final result = await _service.createShipment(
        orderId: _order!.id,
        originAddress: value('originAddress'),
        originRegion: value('originRegion'),
        originCountryCode: value('originCountryCode'),
        destinationAddress: value('destinationAddress'),
        destinationRegion: value('destinationRegion'),
        destinationCountryCode: value('destinationCountryCode'),
        packageDescription: value('packageDescription'),
        packageWeight: double.tryParse(value('packageWeight')),
        packageDimensions: value('packageDimensions'),
        preferredService: _preferred,
        specialHandlingNotes: value('specialHandlingNotes'),
        exportRequired: _export,
      );
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) =>
              ShipmentDetailScreen(shipmentId: result['id'].toString()),
        ),
      );
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = shippingError(e);
          _saving = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: shippingCream,
    appBar: AppBar(
      title: const Text('My Shipments'),
      backgroundColor: shippingGreen,
      foregroundColor: Colors.white,
    ),
    body: SafeArea(
      child: Form(
        key: _form,
        child: shippingBody([
          const ShippingHeader(
            label: 'NEW GEMSTONE SHIPMENT',
            title: 'Create Shipment',
            description: 'Prepare your gemstone for a secure journey. Add delivery details, package information, and your preferred shipping service.',
          ),
          if (_error != null) ShippingFeedback(_error!),
          ShippingSection(
            number: '01',
            title: 'Order Information',
            children: [
              const Text('Connect this shipment to a paid gemstone order.'),
              const SizedBox(height: 16),
              if (_loading)
                const LinearProgressIndicator()
              else if (_orderError != null)
                ShippingFeedback(_orderError!, retry: _loadOrders)
              else if (_orders.isEmpty)
                ShippingFeedback(
                  'No paid orders are available. Orders must belong to your seller account and have no existing shipment.',
                  retry: _loadOrders,
                )
              else
                DropdownButtonFormField<String>(
                  isExpanded: true,
                  initialValue: _order?.id,
                  decoration: const InputDecoration(
                    labelText: 'Order ID *',
                    border: OutlineInputBorder(),
                  ),
                  items: _orders
                      .map(
                        (o) => DropdownMenuItem(
                          value: o.id,
                          child: Text(
                            '${o.text('gemTitle')} · ${shippingMoney(o.amount, o.text('currency'))} · ${shortId(o.id)}',
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: _saving ? null : _select,
                  validator: (v) => v == null ? 'Select a paid order.' : null,
                ),
              const SizedBox(height: 12),
              const Text(
                'Selecting an order fills in its delivery address, value, and currency.',
              ),
              if (_order != null) ...[
                const SizedBox(height: 12),
                ShippingValue(
                  'Paid order value',
                  shippingMoney(_order!.amount, _order!.text('currency')),
                ),
              ],
            ],
          ),
          ShippingSection(
            number: '02',
            title: 'Origin Address',
            children: [
              _field('originAddress', 'Address'),
              _field('originRegion', 'Region', max: 100),
              _field('originCountryCode', 'Country Code'),
            ],
          ),
          ShippingSection(
            number: '03',
            title: 'Destination Address',
            children: [
              _field('destinationAddress', 'Address'),
              _field('destinationRegion', 'Region', max: 100),
              _field('destinationCountryCode', 'Country Code'),
            ],
          ),
          ShippingSection(
            number: '04',
            title: 'Package Details',
            children: [
              _field(
                'packageDescription',
                'Package Description',
                max: 2000,
                lines: 3,
              ),
              _field('packageWeight', 'Weight (kg)', required: false),
              _field(
                'packageDimensions',
                'Dimensions (L × W × H)',
                required: false,
                max: 200,
              ),
            ],
          ),
          ShippingSection(
            number: '05',
            title: 'Shipping Options',
            children: [
              DropdownButtonFormField<String>(
                initialValue: _preferred,
                decoration: const InputDecoration(
                  labelText: 'Preferred Service',
                  border: OutlineInputBorder(),
                ),
                items: ['Standard', 'Express', 'Priority']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                    .toList(),
                onChanged: _saving
                    ? null
                    : (v) => setState(() => _preferred = v!),
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Export Documentation Required'),
                value: _export,
                onChanged: _saving ? null : (v) => setState(() => _export = v),
              ),
              _field(
                'specialHandlingNotes',
                'Special Handling Notes',
                required: false,
                max: 2000,
                lines: 3,
              ),
              const Text(
                'Insurance recommendations are generated with the shipping plan. Approval and insurance purchase remain with an administrator.',
              ),
            ],
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: shippingGreen),
            onPressed: _saving || _loading || _orders.isEmpty ? null : _save,
            child: _saving
                ? const Padding(
                    padding: EdgeInsets.all(8),
                    child: SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    ),
                  )
                : const Text('Create Shipment'),
          ),
          TextButton(
            onPressed: _saving ? null : () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
        ]),
      ),
    ),
  );
}

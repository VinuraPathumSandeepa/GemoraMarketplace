import 'package:flutter/material.dart';

import '../../models/marketplace_gem_model.dart';
import '../../services/marketplace_service.dart';

class GemDetailScreen extends StatefulWidget {
  final MarketplaceGemModel gem;

  const GemDetailScreen({super.key, required this.gem});

  @override
  State<GemDetailScreen> createState() => _GemDetailScreenState();
}

class _GemDetailScreenState extends State<GemDetailScreen> {
  bool _ordering = false;

  Future<void> _buy() async {
    final deliveryDetails = await _collectDeliveryDetails();

    if (deliveryDetails == null || !mounted) return;

    setState(() => _ordering = true);

    try {
      final order = await MarketplaceService().createOrder(
        widget.gem.id,
        deliveryDetails: deliveryDetails,
      );

      if (!mounted) return;

      await showDialog<void>(
        context: context,
        builder: (_) => AlertDialog(
          title: const Text('Order created'),
          content: Text('Order ${order.orderNumber} was created successfully.'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('OK'),
            ),
          ],
        ),
      );

      if (mounted) Navigator.pop(context, true);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              error.toString().replaceFirst('Exception: ', ''),
            ),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _ordering = false);
    }
  }

  Future<Map<String, dynamic>?> _collectDeliveryDetails() async {
    final formKey = GlobalKey<FormState>();
    final controllers = {
      'recipientName': TextEditingController(),
      'recipientPhone': TextEditingController(),
      'addressLine1': TextEditingController(),
      'city': TextEditingController(),
      'district': TextEditingController(),
      'region': TextEditingController(),
      'postalCode': TextEditingController(),
      'countryCode': TextEditingController(text: 'LK'),
    };

    try {
      return await showDialog<Map<String, dynamic>>(
        context: context,
        builder: (dialogContext) {
          return AlertDialog(
            title: const Text('Delivery details'),
            content: SizedBox(
              width: 420,
              child: ConstrainedBox(
                constraints: BoxConstraints(
                  maxHeight: MediaQuery.sizeOf(dialogContext).height * 0.62,
                ),
                child: Form(
                  key: formKey,
                  child: SingleChildScrollView(
                    child: Column(
                      children: [
                        _requiredField(controllers['recipientName']!, 'Recipient name'),
                        _requiredField(controllers['recipientPhone']!, 'Phone number', keyboardType: TextInputType.phone),
                        _requiredField(controllers['addressLine1']!, 'Address'),
                        _requiredField(controllers['city']!, 'City'),
                        _requiredField(controllers['district']!, 'District'),
                        _requiredField(controllers['region']!, 'Province / region'),
                        _requiredField(controllers['postalCode']!, 'Postal code'),
                        _requiredField(controllers['countryCode']!, 'Country code'),
                      ],
                    ),
                  ),
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(dialogContext),
                child: const Text('Cancel'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate()) return;

                  Navigator.pop(dialogContext, {
                    'recipientName': controllers['recipientName']!.text.trim(),
                    'recipientPhone': controllers['recipientPhone']!.text.trim(),
                    'addressLine1': controllers['addressLine1']!.text.trim(),
                    'city': controllers['city']!.text.trim(),
                    'district': controllers['district']!.text.trim(),
                    'region': controllers['region']!.text.trim(),
                    'postalCode': controllers['postalCode']!.text.trim(),
                    'countryCode': controllers['countryCode']!.text.trim().toUpperCase(),
                  });
                },
                child: const Text('Continue'),
              ),
            ],
          );
        },
      );
    } finally {
      for (final controller in controllers.values) {
        controller.dispose();
      }
    }
  }

  Widget _requiredField(
    TextEditingController controller,
    String label, {
    TextInputType? keyboardType,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextFormField(
        controller: controller,
        keyboardType: keyboardType,
        decoration: InputDecoration(labelText: label),
        validator: (value) => value == null || value.trim().isEmpty
            ? '$label is required'
            : null,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final g = widget.gem;

    return Scaffold(
      appBar: AppBar(title: Text(g.title)),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          if (g.primaryImageUrl != null)
            ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: Image.network(
                g.primaryImageUrl!,
                height: 240,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const SizedBox(
                  height: 120,
                  child: Icon(Icons.diamond, size: 80),
                ),
              ),
            ),
          const SizedBox(height: 20),
          Text(
            g.title,
            style: Theme.of(context)
                .textTheme
                .headlineSmall
                ?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            '${g.currency} ${g.price.toStringAsFixed(2)}',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 18),
          Wrap(
            spacing: 10,
            runSpacing: 10,
            children: [
              Chip(label: Text(g.gemType)),
              Chip(label: Text('${g.caratWeight} ct')),
              Chip(label: Text(g.color)),
              Chip(label: Text(g.cut)),
              Chip(label: Text(g.clarity)),
            ],
          ),
          const SizedBox(height: 16),
          Text(g.description),
          const SizedBox(height: 16),
          Text('Seller: ${g.sellerName}'),
          Text('Location: ${g.region}, ${g.countryCode}'),
          if (g.certificateNumber != null)
            Text(
              'Certificate: ${g.certificateAuthority ?? ''} ${g.certificateNumber}',
            ),
          const SizedBox(height: 28),
          FilledButton.icon(
            onPressed: _ordering ? null : _buy,
            icon: const Icon(Icons.shopping_cart_checkout),
            label: Text(
              _ordering ? 'Creating order...' : 'Purchase gemstone',
            ),
          ),
        ],
      ),
    );
  }
}

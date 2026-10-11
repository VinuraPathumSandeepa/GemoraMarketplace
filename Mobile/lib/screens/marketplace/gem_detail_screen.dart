import 'package:flutter/material.dart';
import '../../models/marketplace/marketplace_gem.dart';
import '../../services/marketplace_service.dart';
import 'my_orders_screen.dart';

class GemDetailScreen extends StatefulWidget {
  final MarketplaceGem gem;
  final MarketplaceService? service;

  const GemDetailScreen({super.key, required this.gem, this.service});

  @override
  State<GemDetailScreen> createState() => _GemDetailScreenState();
}

class _GemDetailScreenState extends State<GemDetailScreen> {
  late final MarketplaceService _service;
  bool _ordering = false;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? MarketplaceService();
  }

  void _buy() {
    String selectedCountry = 'LK';
    final addressController =
        TextEditingController(text: '120 Galle Road, Colombo 03');
    final regionController =
        TextEditingController(text: 'Western Province');
    final formKey = GlobalKey<FormState>();

    const countries = [
      {
        'code': 'LK',
        'name': 'Sri Lanka (Domestic - LK)',
        'region': 'Western Province',
        'address': '120 Galle Road, Colombo 03',
      },
      {
        'code': 'US',
        'name': 'United States (International - US)',
        'region': 'California',
        'address': '742 Evergreen Terrace, Springfield',
      },
      {
        'code': 'GB',
        'name': 'United Kingdom (International - GB)',
        'region': 'Greater London',
        'address': '10 Downing Street, London',
      },
      {
        'code': 'AU',
        'name': 'Australia (International - AU)',
        'region': 'New South Wales',
        'address': '42 Wallaby Way, Sydney',
      },
      {
        'code': 'SG',
        'name': 'Singapore (International - SG)',
        'region': 'Central Region',
        'address': '10 Marina Boulevard, Singapore',
      },
    ];

    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) {
        return StatefulBuilder(
          builder: (context, setModalState) {
            final isDomestic = selectedCountry == 'LK';

            return Padding(
              padding: EdgeInsets.only(
                bottom: MediaQuery.of(context).viewInsets.bottom,
                left: 20,
                right: 20,
                top: 20,
              ),
              child: Form(
                key: formKey,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Confirm Purchase: ${widget.gem.title}',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Price: ${widget.gem.currency} ${widget.gem.price.toStringAsFixed(2)}',
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<String>(
                        initialValue: selectedCountry,
                        decoration: const InputDecoration(
                          labelText: 'Shipping Country',
                          border: OutlineInputBorder(),
                        ),
                        items: countries.map((c) {
                          return DropdownMenuItem<String>(
                            value: c['code'],
                            child: Text(c['name']!),
                          );
                        }).toList(),
                        onChanged: (val) {
                          if (val == null) return;
                          setModalState(() {
                            selectedCountry = val;
                            final match = countries.firstWhere(
                              (c) => c['code'] == val,
                              orElse: () => countries.first,
                            );
                            regionController.text = match['region']!;
                            addressController.text = match['address']!;
                          });
                        },
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: addressController,
                        decoration: const InputDecoration(
                          labelText: 'Shipping Address',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) =>
                            (v == null || v.trim().isEmpty) ? 'Required' : null,
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: regionController,
                        decoration: const InputDecoration(
                          labelText: 'Region / Province / State',
                          border: OutlineInputBorder(),
                        ),
                        validator: (v) =>
                            (v == null || v.trim().isEmpty) ? 'Required' : null,
                      ),
                      const SizedBox(height: 12),
                      Text(
                        isDomestic
                            ? '• Domestic Order: Export clearance not required'
                            : '• International Order: Export clearance required (Component 4)',
                        style: TextStyle(
                          color: isDomestic ? Colors.green.shade800 : Colors.orange.shade900,
                          fontWeight: FontWeight.w600,
                          fontSize: 12,
                        ),
                      ),
                      const SizedBox(height: 18),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          TextButton(
                            onPressed: () => Navigator.pop(sheetContext),
                            child: const Text('Cancel'),
                          ),
                          const SizedBox(width: 8),
                          FilledButton(
                            onPressed: () async {
                              if (!formKey.currentState!.validate()) return;
                              Navigator.pop(sheetContext);
                              await _executeOrder(
                                shippingAddress: addressController.text.trim(),
                                shippingRegion: regionController.text.trim(),
                                shippingCountryCode: selectedCountry,
                              );
                            },
                            child: const Text('Place Order'),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                    ],
                  ),
                ),
              ),
            );
          },
        );
      },
    );
  }

  Future<void> _executeOrder({
    required String shippingAddress,
    required String shippingRegion,
    required String shippingCountryCode,
  }) async {
    setState(() => _ordering = true);
    try {
      final order = await _service.createOrder(
        widget.gem.id,
        shippingAddress: shippingAddress,
        shippingRegion: shippingRegion,
        shippingCountryCode: shippingCountryCode,
      );
      if (!mounted) return;
      setState(() => _ordering = false);
      await showDialog<void>(
        context: context,
        builder: (dialogCtx) => AlertDialog(
          title: const Text('Order created'),
          content: Text(
            'Order ${order.orderNumber} was created successfully.\n'
            '${order.shippingCountryCode != 'LK' ? "International order: export clearance required." : "Domestic order: export clearance not required."}',
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(dialogCtx);
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (_) => const MyOrdersScreen()),
                );
              },
              child: const Text('View My Orders'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogCtx),
              child: const Text('OK'),
            ),
          ],
        ),
      );
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))),
        );
      }
    } finally {
      if (mounted) setState(() => _ordering = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final g = widget.gem;
    final imageUrl = g.resolvedImageUrl;

    return Scaffold(
      appBar: AppBar(title: Text(g.title)),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(16),
            child: imageUrl != null
                ? Image.network(
                    imageUrl,
                    height: 240,
                    fit: BoxFit.cover,
                    loadingBuilder: (context, child, progress) {
                      if (progress == null) return child;
                      return const SizedBox(
                        height: 240,
                        child: Center(child: CircularProgressIndicator()),
                      );
                    },
                    errorBuilder: (_, _, _) => const SizedBox(
                      height: 120,
                      child: Icon(Icons.diamond, size: 80),
                    ),
                  )
                : const SizedBox(
                    height: 120,
                    child: Icon(Icons.diamond, size: 80),
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
          Wrap(spacing: 10, runSpacing: 10, children: [
            Chip(label: Text(g.gemType)),
            Chip(label: Text('${g.caratWeight} ct')),
            Chip(label: Text(g.color)),
            Chip(label: Text(g.cut)),
            Chip(label: Text(g.clarity)),
          ]),
          const SizedBox(height: 16),
          Text(g.description),
          const SizedBox(height: 16),
          Text('Seller: ${g.sellerName}'),
          Text('Location: ${g.region}, ${g.countryCode}'),
          if (g.certificateNumber != null)
            Text('Certificate: ${g.certificateAuthority ?? ''} ${g.certificateNumber}'),
          const SizedBox(height: 28),
          FilledButton.icon(
            onPressed: _ordering ? null : _buy,
            icon: _ordering
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                  )
                : const Icon(Icons.shopping_cart_checkout),
            label: Text(_ordering ? 'Creating order...' : 'Purchase gemstone'),
          ),
        ],
      ),
    );
  }
}

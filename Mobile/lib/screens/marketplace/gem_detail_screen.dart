import 'package:flutter/material.dart';
import '../../models/marketplace/marketplace_gem.dart';
import '../../services/marketplace_service.dart';

class GemDetailScreen extends StatefulWidget {
  final MarketplaceGem gem;
  const GemDetailScreen({super.key, required this.gem});

  @override
  State<GemDetailScreen> createState() => _GemDetailScreenState();
}

class _GemDetailScreenState extends State<GemDetailScreen> {
  bool _ordering = false;

  Future<void> _buy() async {
    setState(() => _ordering = true);
    try {
      final order = await MarketplaceService().createOrder(widget.gem.id);
      if (!mounted) return;
      await showDialog(
        context: context,
        builder: (_) => AlertDialog(
          title: const Text('Order created'),
          content: Text('Order ${order.orderNumber} was created successfully.'),
          actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('OK'))],
        ),
      );
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString().replaceFirst('Exception: ', ''))));
    } finally {
      if (mounted) setState(() => _ordering = false);
    }
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
            ClipRRect(borderRadius: BorderRadius.circular(16), child: Image.network(g.primaryImageUrl!, height: 240, fit: BoxFit.cover, errorBuilder: (_, _, _) => const SizedBox(height: 120, child: Icon(Icons.diamond, size: 80)))),
          const SizedBox(height: 20),
          Text(g.title, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Text('${g.currency} ${g.price.toStringAsFixed(2)}', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 18),
          Wrap(spacing: 10, runSpacing: 10, children: [
            Chip(label: Text(g.gemType)), Chip(label: Text('${g.caratWeight} ct')),
            Chip(label: Text(g.color)), Chip(label: Text(g.cut)), Chip(label: Text(g.clarity)),
          ]),
          const SizedBox(height: 16),
          Text(g.description),
          const SizedBox(height: 16),
          Text('Seller: ${g.sellerName}'),
          Text('Location: ${g.region}, ${g.countryCode}'),
          if (g.certificateNumber != null) Text('Certificate: ${g.certificateAuthority ?? ''} ${g.certificateNumber}'),
          const SizedBox(height: 28),
          FilledButton.icon(onPressed: _ordering ? null : _buy, icon: const Icon(Icons.shopping_cart_checkout), label: Text(_ordering ? 'Creating order...' : 'Purchase gemstone')),
        ],
      ),
    );
  }
}

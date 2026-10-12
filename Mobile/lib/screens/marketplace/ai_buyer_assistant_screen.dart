import 'package:flutter/material.dart';
import '../../services/marketplace_service.dart';

class AiBuyerAssistantScreen extends StatefulWidget {
  const AiBuyerAssistantScreen({super.key});
  @override
  State<AiBuyerAssistantScreen> createState() => _AiBuyerAssistantScreenState();
}

class _AiBuyerAssistantScreenState extends State<AiBuyerAssistantScreen> {
  final _controller = TextEditingController();
  bool _loading = false;
  Map<String, dynamic>? _result;

  Future<void> _ask() async {
    if (_controller.text.trim().length < 2) return;
    setState(() => _loading = true);
    try { _result = await MarketplaceService().askAgent(_controller.text.trim()); }
    catch (e) { if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString().replaceFirst('Exception: ', '')))); }
    finally { if (mounted) setState(() => _loading = false); }
  }

  @override
  Widget build(BuildContext context) {
    final recs = ((_result?['recommendations'] ?? []) as List);
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text('Buyer Assistance Agent', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        const Text('Ask for gemstone options. The agent can only recommend verified, currently eligible marketplace listings.'),
        const SizedBox(height: 16),
        TextField(controller: _controller, minLines: 2, maxLines: 4, decoration: InputDecoration(hintText: 'Example: I want a blue sapphire under LKR 500,000', border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)))),
        const SizedBox(height: 12),
        FilledButton.icon(onPressed: _loading ? null : _ask, icon: const Icon(Icons.auto_awesome), label: Text(_loading ? 'Checking marketplace...' : 'Ask assistant')),
        if (_result != null) ...[
          const SizedBox(height: 20),
          Text(_result!['summary'] ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
          const SizedBox(height: 10),
          ...recs.map((r) => Card(child: ListTile(title: Text(r['title'] ?? ''), subtitle: Text(((r['reasons'] ?? []) as List).join('\n'))))),
        ],
      ],
    );
  }
}

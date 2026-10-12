import 'package:flutter/material.dart';

import '../../services/marketplace_service.dart';

class AiBuyerAssistantScreen extends StatefulWidget {
  const AiBuyerAssistantScreen({super.key});

  @override
  State<AiBuyerAssistantScreen> createState() =>
      _AiBuyerAssistantScreenState();
}

class _AiBuyerAssistantScreenState extends State<AiBuyerAssistantScreen> {
  final _controller = TextEditingController();
  final _service = MarketplaceService();

  bool _loading = false;
  Map<String, dynamic>? _result;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _ask() async {
    final message = _controller.text.trim();

    if (message.length < 2) return;

    setState(() => _loading = true);

    try {
      final result = await _service.askAgent(message);

      if (mounted) {
        setState(() => _result = result);
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(error.toString().replaceFirst('Exception: ', '')),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final message = _result?['message']?.toString();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'Buyer Assistance Agent',
          style: Theme.of(context)
              .textTheme
              .headlineSmall
              ?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 8),
        const Text(
          'Ask about verified gemstones, marketplace listings, or your orders.',
        ),
        const SizedBox(height: 16),
        TextField(
          controller: _controller,
          minLines: 2,
          maxLines: 4,
          textInputAction: TextInputAction.newline,
          decoration: InputDecoration(
            hintText: 'Example: I want a blue sapphire under LKR 500,000',
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(14),
            ),
          ),
        ),
        const SizedBox(height: 12),
        FilledButton.icon(
          onPressed: _loading ? null : _ask,
          icon: const Icon(Icons.auto_awesome),
          label: Text(
            _loading ? 'Checking marketplace...' : 'Ask assistant',
          ),
        ),
        if (message != null && message.isNotEmpty) ...[
          const SizedBox(height: 20),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Text(message, style: const TextStyle(height: 1.5)),
            ),
          ),
        ],
      ],
    );
  }
}

import 'package:flutter/material.dart';

import '../../services/marketplace_service.dart';

class AiBuyerAssistantScreen extends StatefulWidget {
  const AiBuyerAssistantScreen({super.key});

  @override
  State<AiBuyerAssistantScreen> createState() => _AiBuyerAssistantScreenState();
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
    final summary = (_result?['summary'] ?? _result?['message'])?.toString();
    final rawRecommendations = _result?['recommendations'];
    final recommendations = rawRecommendations is List
        ? rawRecommendations.whereType<Map>().toList()
        : const <Map>[];

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'Buyer Assistance Agent',
          style: Theme.of(context).textTheme.headlineSmall
              ?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 8),
        const Text(
          'Ask for gemstone options. The assistant recommends verified, currently eligible marketplace listings.',
        ),
        const SizedBox(height: 16),
        TextField(
          controller: _controller,
          minLines: 2,
          maxLines: 4,
          textInputAction: TextInputAction.newline,
          decoration: InputDecoration(
            hintText: 'Example: I want a blue sapphire under LKR 500,000',
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
          ),
        ),
        const SizedBox(height: 12),
        FilledButton.icon(
          onPressed: _loading ? null : _ask,
          icon: const Icon(Icons.auto_awesome),
          label: Text(_loading ? 'Checking marketplace...' : 'Ask assistant'),
        ),
        if (summary != null && summary.isNotEmpty) ...[
          const SizedBox(height: 20),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Text(summary, style: const TextStyle(height: 1.5)),
            ),
          ),
        ],
        if (recommendations.isNotEmpty) ...[
          const SizedBox(height: 20),
          ...recommendations.map((recommendation) {
            final rawReasons = recommendation['reasons'];
            final reasons = rawReasons is List
                ? rawReasons.map((reason) => reason.toString()).join('\n')
                : rawReasons?.toString() ?? '';
            return Card(
              child: ListTile(
                title: Text(recommendation['title']?.toString() ?? ''),
                subtitle: reasons.isEmpty ? null : Text(reasons),
              ),
            );
          }),
        ],
      ],
    );
  }
}

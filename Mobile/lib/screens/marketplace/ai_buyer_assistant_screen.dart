import 'package:flutter/material.dart';

import '../../constants/gemora_colors.dart';
import '../../services/marketplace_service.dart';
import '../../widgets/buyer_glass.dart';

class AiBuyerAssistantScreen extends StatefulWidget {
  final ScrollController? scrollController;

  const AiBuyerAssistantScreen({super.key, this.scrollController});

  @override
  State<AiBuyerAssistantScreen> createState() => _AiBuyerAssistantScreenState();
}

class _AiBuyerAssistantScreenState extends State<AiBuyerAssistantScreen> {
  final _controller = TextEditingController();
  final _service = MarketplaceService();
  bool _loading = false;
  Map<String, dynamic>? _result;

  static const _prompts = [
    'Blue sapphire under LKR 500,000',
    'Show verified investment gems',
    'Help me choose a gemstone',
  ];

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _ask([String? suggestion]) async {
    if (suggestion != null) _controller.text = suggestion;
    final message = _controller.text.trim();
    if (message.length < 2 || _loading) return;
    setState(() => _loading = true);
    try {
      final result = await _service.askAgent(message);
      if (mounted) setState(() => _result = result);
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
      controller: widget.scrollController,
      padding: const EdgeInsets.fromLTRB(18, 6, 18, 34),
      children: [
        Row(
          children: [
            Container(
              width: 52,
              height: 52,
              decoration: BoxDecoration(
                color: GemoraColors.darkGreen,
                borderRadius: BorderRadius.circular(18),
              ),
              child: const Icon(
                Icons.auto_awesome_rounded,
                color: GemoraColors.gold,
              ),
            ),
            const SizedBox(width: 13),
            const Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Ask Gemora AI',
                    style: TextStyle(
                      color: GemoraColors.darkGreen,
                      fontSize: 23,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  SizedBox(height: 3),
                  Text(
                    'Your intelligent gemstone guide',
                    style: TextStyle(
                      color: GemoraColors.mutedText,
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),
        BuyerGlassPanel(
          tint: GemoraColors.darkGreen.withValues(alpha: .96),
          child: const Text(
            'Tell me your preferred gemstone, colour, carat size or budget. I will search verified marketplace listings for you.',
            style: TextStyle(color: Colors.white70, height: 1.55),
          ),
        ),
        const SizedBox(height: 14),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: _prompts
                .map(
                  (prompt) => Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ActionChip(
                      onPressed: _loading ? null : () => _ask(prompt),
                      avatar: const Icon(Icons.auto_awesome_rounded, size: 15),
                      label: Text(prompt),
                      backgroundColor: Colors.white.withValues(alpha: .75),
                      side: BorderSide(
                        color: GemoraColors.green.withValues(alpha: .14),
                      ),
                    ),
                  ),
                )
                .toList(),
          ),
        ),
        const SizedBox(height: 14),
        TextField(
          controller: _controller,
          minLines: 3,
          maxLines: 5,
          textInputAction: TextInputAction.newline,
          decoration: InputDecoration(
            hintText: 'What kind of gemstone are you looking for?',
            filled: true,
            fillColor: Colors.white.withValues(alpha: .8),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: BorderSide.none,
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(18),
              borderSide: BorderSide(
                color: GemoraColors.darkGreen.withValues(alpha: .11),
              ),
            ),
          ),
        ),
        const SizedBox(height: 11),
        FilledButton.icon(
          onPressed: _loading ? null : () => _ask(),
          style: FilledButton.styleFrom(
            backgroundColor: GemoraColors.darkGreen,
            padding: const EdgeInsets.symmetric(vertical: 15),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
            ),
          ),
          icon: _loading
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    color: Colors.white,
                    strokeWidth: 2,
                  ),
                )
              : const Icon(Icons.send_rounded, size: 18),
          label: Text(
            _loading ? 'Searching verified gems...' : 'Ask assistant',
          ),
        ),
        if (summary != null && summary.isNotEmpty) ...[
          const SizedBox(height: 20),
          BuyerGlassPanel(
            child: Text(summary, style: const TextStyle(height: 1.55)),
          ),
        ],
        if (recommendations.isNotEmpty) ...[
          const SizedBox(height: 20),
          const BuyerSectionHeading(
            eyebrow: 'AI shortlist',
            title: 'Recommended for you',
          ),
          const SizedBox(height: 12),
          ...recommendations.map((recommendation) {
            final rawReasons = recommendation['reasons'];
            final reasons = rawReasons is List
                ? rawReasons.map((reason) => reason.toString()).join('\n')
                : rawReasons?.toString() ?? '';
            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: BuyerGlassPanel(
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const CircleAvatar(
                    backgroundColor: GemoraColors.darkGreen,
                    child: Icon(
                      Icons.diamond_outlined,
                      color: GemoraColors.gold,
                    ),
                  ),
                  title: Text(
                    recommendation['title']?.toString() ?? '',
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                  subtitle: reasons.isEmpty ? null : Text(reasons),
                ),
              ),
            );
          }),
        ],
      ],
    );
  }
}

import 'package:flutter/material.dart';

import '../../models/shipment_model.dart';

const shippingCream = Color(0xFFF8F4EA);
const shippingGreen = Color(0xFF142D27);
const shippingGold = Color(0xFFB88436);
const shippingBorder = Color(0xFFE8DCCA);
String shippingError(Object error) =>
    error.toString().replaceFirst('Exception: ', '');
String shortId(String value) =>
    value.length > 8 ? '${value.substring(0, 8)}…' : value;
String shippingDate(dynamic value) {
  final date = ShipmentModel.parseDate(value);
  if (date == null) return 'Not available';
  return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
}

String shippingMoney(dynamic amount, String currency) {
  final number = ShipmentModel.number(amount);
  return number == null
      ? 'Not available'
      : '$currency ${number.toStringAsFixed(2)}';
}

class ShippingSection extends StatelessWidget {
  final String title;
  final String? number;
  final List<Widget> children;
  const ShippingSection({
    super.key,
    required this.title,
    this.number,
    required this.children,
  });
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 18),
    padding: const EdgeInsets.all(20),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      border: Border.all(color: shippingBorder),
    ),
    child: Material(
      color: Colors.transparent,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              if (number != null) ...[
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: shippingCream,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    number!,
                    style: const TextStyle(
                      color: shippingGold,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const SizedBox(width: 12),
              ],
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                    color: shippingGreen,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          ...children,
        ],
      ),
    ),
  );
}

class ShippingHeader extends StatelessWidget {
  final String label, title, description;
  const ShippingHeader({
    super.key,
    required this.label,
    required this.title,
    required this.description,
  });
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 20),
    child: Column(
      children: [
        Text(
          label,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: shippingGold,
            fontSize: 10,
            letterSpacing: 2,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 12),
        Text(
          title,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: shippingGreen,
            fontSize: 32,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 12),
        Text(
          description,
          textAlign: TextAlign.center,
          style: const TextStyle(color: Color(0xFF667B78), height: 1.5),
        ),
      ],
    ),
  );
}

class ShipmentStatusBadge extends StatelessWidget {
  final String status;
  const ShipmentStatusBadge(this.status, {super.key});
  @override
  Widget build(BuildContext context) {
    final color = switch (status.toLowerCase()) {
      'plangenerated' || 'booked' || 'readyforbooking' => Colors.blue,
      'intransit' || 'outfordelivery' => Colors.deepPurple,
      'planapproved' => Colors.green,
      'delivered' => Colors.blueGrey,
      'cancelled' || 'deliveryfailed' => Colors.red,
      'pending' || 'planning' || 'exception' || 'customshold' => Colors.orange,
      _ => Colors.grey,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        status.isEmpty ? 'Not available' : status,
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class ShippingFeedback extends StatelessWidget {
  final String message;
  final VoidCallback? retry;
  const ShippingFeedback(this.message, {super.key, this.retry});
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(20),
    child: Column(
      children: [
        Text(message, textAlign: TextAlign.center),
        if (retry != null)
          TextButton.icon(
            onPressed: retry,
            icon: const Icon(Icons.refresh),
            label: const Text('Retry'),
          ),
      ],
    ),
  );
}

class ShippingValue extends StatelessWidget {
  final String label;
  final dynamic value;
  const ShippingValue(this.label, this.value, {super.key});
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(color: Color(0xFF667B78), fontSize: 12),
        ),
        const SizedBox(height: 4),
        SelectableText(
          value == null || '$value'.isEmpty
              ? 'Not available'
              : value is List
              ? (value as List).join('\n')
              : '$value',
          style: const TextStyle(color: shippingGreen),
        ),
      ],
    ),
  );
}

Widget shippingBody(List<Widget> children) => Center(
  child: ConstrainedBox(
    constraints: const BoxConstraints(maxWidth: 780),
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(18),
      children: children,
    ),
  ),
);

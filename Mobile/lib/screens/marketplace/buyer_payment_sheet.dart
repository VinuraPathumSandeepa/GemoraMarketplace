import 'package:flutter/material.dart';

import '../../models/marketplace/order_model.dart';
import '../../services/marketplace_service.dart';

Future<bool> showBuyerPaymentSheet(
  BuildContext context, {
  required OrderModel order,
  required MarketplaceService service,
}) async {
  return await showModalBottomSheet<bool>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        backgroundColor: Colors.transparent,
        builder: (_) => _BuyerPaymentSheet(order: order, service: service),
      ) ??
      false;
}

class _BuyerPaymentSheet extends StatefulWidget {
  final OrderModel order;
  final MarketplaceService service;

  const _BuyerPaymentSheet({required this.order, required this.service});

  @override
  State<_BuyerPaymentSheet> createState() => _BuyerPaymentSheetState();
}

class _BuyerPaymentSheetState extends State<_BuyerPaymentSheet> {
  PaymentIntentModel? _intent;
  bool _busy = false;
  bool _accepted = false;
  String? _error;

  Future<void> _start() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final intent = await widget.service.createPaymentIntent(widget.order.id);
      if (mounted) setState(() => _intent = intent);
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pay() async {
    final intent = _intent;
    if (intent == null || !_accepted) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      // The local sandbox gateway accepts an opaque token. No card number or
      // security code is sent to or stored by Gemora.
      final token = 'demo_card_mobile_${DateTime.now().microsecondsSinceEpoch}';
      await widget.service.confirmPayment(
        widget.order.id,
        paymentIntentId: intent.paymentIntentId,
        paymentMethodToken: token,
      );
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final intent = _intent;
    return Container(
      padding: EdgeInsets.fromLTRB(
        24,
        12,
        24,
        24 + MediaQuery.viewInsetsOf(context).bottom,
      ),
      decoration: const BoxDecoration(
        color: Color(0xFFFAF7F0),
        borderRadius: BorderRadius.vertical(top: Radius.circular(30)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Center(
            child: Container(
              width: 52,
              height: 5,
              decoration: BoxDecoration(
                color: const Color(0xFFD8D0C3),
                borderRadius: BorderRadius.circular(10),
              ),
            ),
          ),
          const SizedBox(height: 22),
          const Text(
            'Secure payment',
            style: TextStyle(
              color: Color(0xFF08251E),
              fontSize: 24,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 8),
          Text('${widget.order.orderNumber} · ${widget.order.gemTitle}'),
          const SizedBox(height: 20),
          if (intent == null)
            _PaymentSummary(
              rows: [('Order total', widget.order.agreedPrice)],
              currency: widget.order.currency,
            )
          else
            _PaymentSummary(
              rows: [
                ('Order total', intent.orderTotalAmount),
                ('Advance payment', intent.amount),
                ('Remaining balance', intent.remainingAmount),
              ],
              currency: intent.currency,
            ),
          if (intent != null) ...[
            const SizedBox(height: 12),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              value: _accepted,
              activeColor: const Color(0xFF0B473A),
              onChanged: _busy
                  ? null
                  : (value) => setState(() => _accepted = value ?? false),
              title: const Text(
                'I confirm this sandbox card payment.',
                style: TextStyle(fontWeight: FontWeight.w700),
              ),
              subtitle: const Text(
                'Gemora creates an opaque demo token and never stores card details.',
              ),
            ),
          ],
          if (_error != null) ...[
            const SizedBox(height: 10),
            Text(_error!, style: const TextStyle(color: Colors.red)),
          ],
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: _busy
                ? null
                : intent == null
                ? _start
                : (_accepted ? _pay : null),
            icon: _busy
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Icon(intent == null ? Icons.lock_outline : Icons.credit_card),
            label: Text(intent == null ? 'Review payment' : 'Confirm payment'),
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFD39A3C),
              foregroundColor: const Color(0xFF08251E),
              minimumSize: const Size.fromHeight(52),
            ),
          ),
          TextButton(
            onPressed: _busy ? null : () => Navigator.pop(context, false),
            child: const Text('Not now'),
          ),
        ],
      ),
    );
  }
}

class _PaymentSummary extends StatelessWidget {
  final List<(String, double)> rows;
  final String currency;

  const _PaymentSummary({required this.rows, required this.currency});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFE7DFD2)),
      ),
      child: Column(
        children: rows
            .map(
              (row) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 5),
                child: Row(
                  children: [
                    Expanded(child: Text(row.$1)),
                    Text(
                      '$currency ${row.$2.toStringAsFixed(2)}',
                      style: const TextStyle(fontWeight: FontWeight.w800),
                    ),
                  ],
                ),
              ),
            )
            .toList(),
      ),
    );
  }
}

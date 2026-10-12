import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../constants/gemora_colors.dart';
import '../../models/marketplace/order_model.dart';
import '../../services/marketplace_service.dart';
import '../../widgets/buyer_glass.dart';

class CardPaymentScreen extends StatefulWidget {
  final OrderModel order;
  final MarketplaceService? service;

  const CardPaymentScreen({super.key, required this.order, this.service});

  @override
  State<CardPaymentScreen> createState() => _CardPaymentScreenState();
}

class _CardPaymentScreenState extends State<CardPaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _number = TextEditingController();
  final _expiry = TextEditingController();
  final _cvv = TextEditingController();
  late final MarketplaceService _service;
  String _cardKind = 'Credit';
  bool _processing = false;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? MarketplaceService();
  }

  @override
  void dispose() {
    _name.dispose();
    _number.dispose();
    _expiry.dispose();
    _cvv.dispose();
    super.dispose();
  }

  Future<void> _pay() async {
    if (!_formKey.currentState!.validate() || _processing) return;
    FocusScope.of(context).unfocus();
    setState(() => _processing = true);

    try {
      final intent = await _service.createPaymentIntent(widget.order.id);
      final intentId = intent['paymentIntentId']?.toString() ?? '';
      if (intentId.isEmpty) {
        throw Exception('The payment session could not be created.');
      }

      // Raw card data stays on this screen. The API receives only an opaque
      // one-time token understood by the currently configured gateway.
      final digits = _digits(_number.text);
      final token = 'demo_card_mobile_${digits.substring(digits.length - 4)}';
      final paidOrder = await _service.confirmCardPayment(
        orderId: widget.order.id,
        paymentIntentId: intentId,
        paymentMethodToken: token,
      );

      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(24),
          ),
          icon: const Icon(
            Icons.check_circle_rounded,
            color: Color(0xFF2E7D32),
            size: 52,
          ),
          title: const Text('Payment approved'),
          content: Text(
            '${paidOrder.orderNumber} is paid and ready for the seller to prepare for delivery.',
            textAlign: TextAlign.center,
          ),
          actionsAlignment: MainAxisAlignment.center,
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(context),
              style: FilledButton.styleFrom(
                backgroundColor: GemoraColors.darkGreen,
              ),
              child: const Text('Back to orders'),
            ),
          ],
        ),
      );
      if (mounted) Navigator.pop(context, true);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFF8C2E2A),
            content: Text(error.toString().replaceFirst('Exception: ', '')),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _processing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final estimatedAdvance =
        (widget.order.agreedPrice * .60 / 1000).round() * 1000;

    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: AppBar(
        elevation: 0,
        backgroundColor: GemoraColors.darkGreen,
        foregroundColor: Colors.white,
        title: const Text('Secure Card Payment'),
      ),
      body: BuyerGradientBackground(
        child: SafeArea(
          top: false,
          child: Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(18, 22, 18, 38),
              children: [
                BuyerGlassPanel(
                  tint: GemoraColors.darkGreen.withValues(alpha: .96),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Row(
                        children: [
                          Icon(Icons.lock_rounded, color: GemoraColors.gold),
                          SizedBox(width: 9),
                          Text(
                            'SECURE CHECKOUT',
                            style: TextStyle(
                              color: GemoraColors.gold,
                              fontSize: 11,
                              fontWeight: FontWeight.w900,
                              letterSpacing: 1.2,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 18),
                      Text(
                        widget.order.gemTitle,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Payable now · ${widget.order.currency} ${estimatedAdvance.toStringAsFixed(2)}',
                        style: const TextStyle(
                          color: Colors.white70,
                          fontSize: 15,
                        ),
                      ),
                      const SizedBox(height: 6),
                      const Text(
                        '60% advance payment. Your card number and security code are never stored by Gemora.',
                        style: TextStyle(
                          color: Colors.white60,
                          fontSize: 11,
                          height: 1.4,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 18),
                BuyerGlassPanel(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Text(
                        'Choose card type',
                        style: TextStyle(
                          color: GemoraColors.darkGreen,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 12),
                      SegmentedButton<String>(
                        segments: const [
                          ButtonSegment(
                            value: 'Credit',
                            icon: Icon(Icons.credit_card_rounded),
                            label: Text('Credit card'),
                          ),
                          ButtonSegment(
                            value: 'Debit',
                            icon: Icon(Icons.payment_rounded),
                            label: Text('Debit card'),
                          ),
                        ],
                        selected: {_cardKind},
                        onSelectionChanged: (value) =>
                            setState(() => _cardKind = value.first),
                        showSelectedIcon: false,
                        style: ButtonStyle(
                          backgroundColor: WidgetStateProperty.resolveWith(
                            (states) => states.contains(WidgetState.selected)
                                ? GemoraColors.darkGreen
                                : Colors.white.withValues(alpha: .45),
                          ),
                          foregroundColor: WidgetStateProperty.resolveWith(
                            (states) => states.contains(WidgetState.selected)
                                ? Colors.white
                                : GemoraColors.darkGreen,
                          ),
                        ),
                      ),
                      const SizedBox(height: 22),
                      _field(
                        controller: _name,
                        label: 'Name on card',
                        hint: 'As printed on your card',
                        icon: Icons.person_outline_rounded,
                        textCapitalization: TextCapitalization.words,
                        validator: (value) => (value?.trim().length ?? 0) < 3
                            ? 'Enter the cardholder name'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      _field(
                        controller: _number,
                        label: 'Card number',
                        hint: '0000 0000 0000 0000',
                        icon: Icons.credit_card_rounded,
                        keyboardType: TextInputType.number,
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                          LengthLimitingTextInputFormatter(19),
                          _CardNumberFormatter(),
                        ],
                        validator: (value) => _isValidCard(value ?? '')
                            ? null
                            : 'Enter a valid card number',
                      ),
                      const SizedBox(height: 14),
                      Row(
                        children: [
                          Expanded(
                            child: _field(
                              controller: _expiry,
                              label: 'Expiry date',
                              hint: 'MM/YY',
                              icon: Icons.calendar_today_outlined,
                              keyboardType: TextInputType.number,
                              inputFormatters: [
                                FilteringTextInputFormatter.digitsOnly,
                                LengthLimitingTextInputFormatter(4),
                                _ExpiryFormatter(),
                              ],
                              validator: _validateExpiry,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: _field(
                              controller: _cvv,
                              label: 'CVV',
                              hint: '123',
                              icon: Icons.shield_outlined,
                              obscureText: true,
                              keyboardType: TextInputType.number,
                              inputFormatters: [
                                FilteringTextInputFormatter.digitsOnly,
                                LengthLimitingTextInputFormatter(4),
                              ],
                              validator: (value) {
                                final length = value?.length ?? 0;
                                return length < 3 ? 'Invalid CVV' : null;
                              },
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      FilledButton.icon(
                        onPressed: _processing ? null : _pay,
                        style: FilledButton.styleFrom(
                          backgroundColor: GemoraColors.darkGreen,
                          foregroundColor: Colors.white,
                          disabledBackgroundColor: GemoraColors.darkGreen
                              .withValues(alpha: .55),
                          padding: const EdgeInsets.symmetric(vertical: 17),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                        ),
                        icon: _processing
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Icon(Icons.lock_rounded, size: 19),
                        label: Text(
                          _processing
                              ? 'Authorizing payment...'
                              : 'Pay ${widget.order.currency} ${estimatedAdvance.toStringAsFixed(2)}',
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _field({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    required String? Function(String?) validator,
    TextInputType? keyboardType,
    List<TextInputFormatter>? inputFormatters,
    TextCapitalization textCapitalization = TextCapitalization.none,
    bool obscureText = false,
  }) {
    return TextFormField(
      controller: controller,
      keyboardType: keyboardType,
      inputFormatters: inputFormatters,
      textCapitalization: textCapitalization,
      obscureText: obscureText,
      validator: validator,
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        prefixIcon: Icon(icon, size: 20),
        filled: true,
        fillColor: Colors.white.withValues(alpha: .72),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(15),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(15),
          borderSide: BorderSide(
            color: GemoraColors.darkGreen.withValues(alpha: .12),
          ),
        ),
      ),
    );
  }

  String? _validateExpiry(String? value) {
    final digits = _digits(value ?? '');
    if (digits.length != 4) return 'Use MM/YY';
    final month = int.tryParse(digits.substring(0, 2)) ?? 0;
    final year = 2000 + (int.tryParse(digits.substring(2)) ?? 0);
    final now = DateTime.now();
    if (month < 1 || month > 12) return 'Invalid month';
    if (DateTime(year, month + 1).isBefore(now)) return 'Card expired';
    return null;
  }
}

String _digits(String value) => value.replaceAll(RegExp(r'\D'), '');

bool _isValidCard(String value) {
  final digits = _digits(value);
  if (digits.length < 13 || digits.length > 19) return false;
  var sum = 0;
  var alternate = false;
  for (var index = digits.length - 1; index >= 0; index--) {
    var digit = int.parse(digits[index]);
    if (alternate) {
      digit *= 2;
      if (digit > 9) digit -= 9;
    }
    sum += digit;
    alternate = !alternate;
  }
  return sum % 10 == 0;
}

class _CardNumberFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = _digits(newValue.text);
    final groups = <String>[];
    for (var index = 0; index < digits.length; index += 4) {
      groups.add(digits.substring(index, (index + 4).clamp(0, digits.length)));
    }
    final text = groups.join(' ');
    return TextEditingValue(
      text: text,
      selection: TextSelection.collapsed(offset: text.length),
    );
  }
}

class _ExpiryFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = _digits(newValue.text);
    final text = digits.length > 2
        ? '${digits.substring(0, 2)}/${digits.substring(2)}'
        : digits;
    return TextEditingValue(
      text: text,
      selection: TextSelection.collapsed(offset: text.length),
    );
  }
}

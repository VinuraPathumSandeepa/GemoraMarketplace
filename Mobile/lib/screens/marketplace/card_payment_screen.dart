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
  final _deliveryFormKey = GlobalKey<FormState>();
  final _deliveryFields = <String, TextEditingController>{};
  final _scrollController = ScrollController();
  final _name = TextEditingController();
  final _number = TextEditingController();
  final _expiry = TextEditingController();
  final _cvv = TextEditingController();
  late final MarketplaceService _service;
  String _cardKind = 'Credit';
  bool _processing = false;
  bool _deliveryReady = false;
  String? _paymentError;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? MarketplaceService();
    final delivery = widget.order.deliveryDetails;
    _deliveryFields.addAll({
      'recipientName': TextEditingController(
        text: delivery?.recipientName ?? widget.order.buyerName,
      ),
      'recipientPhone': TextEditingController(
        text: delivery?.recipientPhone ?? '',
      ),
      'alternatePhone': TextEditingController(
        text: delivery?.alternatePhone ?? '',
      ),
      'addressLine1': TextEditingController(text: delivery?.addressLine1 ?? ''),
      'addressLine2': TextEditingController(text: delivery?.addressLine2 ?? ''),
      'city': TextEditingController(text: delivery?.city ?? ''),
      'district': TextEditingController(text: delivery?.district ?? ''),
      'region': TextEditingController(text: delivery?.region ?? ''),
      'postalCode': TextEditingController(text: delivery?.postalCode ?? ''),
      'countryCode': TextEditingController(
        text: delivery?.countryCode.isNotEmpty == true
            ? delivery!.countryCode
            : 'LK',
      ),
      'nearestLandmark': TextEditingController(
        text: delivery?.nearestLandmark ?? '',
      ),
      'deliveryInstructions': TextEditingController(
        text: delivery?.deliveryInstructions ?? '',
      ),
    });
  }

  @override
  void dispose() {
    _scrollController.dispose();
    for (final controller in _deliveryFields.values) {
      controller.dispose();
    }
    _name.dispose();
    _number.dispose();
    _expiry.dispose();
    _cvv.dispose();
    super.dispose();
  }

  Future<void> _saveDelivery() async {
    if (!_deliveryFormKey.currentState!.validate() || _processing) return;
    FocusScope.of(context).unfocus();
    setState(() => _processing = true);
    try {
      await _service.updateOrderDeliveryDetails(widget.order.id, {
        for (final entry in _deliveryFields.entries)
          entry.key: entry.key == 'countryCode'
              ? entry.value.text.trim().toUpperCase()
              : entry.value.text.trim(),
      });
      if (mounted) _showStep(payment: true);
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

  void _showStep({required bool payment}) {
    FocusScope.of(context).unfocus();
    setState(() => _deliveryReady = payment);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted && _scrollController.hasClients) {
        _scrollController.jumpTo(0);
      }
    });
  }

  Future<void> _pay() async {
    if (!_deliveryReady || !_formKey.currentState!.validate() || _processing) {
      return;
    }
    FocusScope.of(context).unfocus();
    setState(() {
      _processing = true;
      _paymentError = null;
    });

    try {
      final intent = await _service.createPaymentIntent(widget.order.id);
      final intentId = intent['paymentIntentId']?.toString() ?? '';
      if (intentId.isEmpty) {
        throw Exception('The payment session could not be created.');
      }

      // This is a demo gateway. Only known test numbers are accepted and the
      // API receives a demo token, never the entered card fields.
      final digits = _digits(_number.text);
      final token = digits == '4000000000000002'
          ? 'demo_card_mobile_decline'
          : 'demo_card_mobile_4242';
      final paidOrder = await _service.confirmCardPayment(
        orderId: widget.order.id,
        paymentIntentId: intentId,
        paymentMethodToken: token,
      );

      if (!mounted) return;
      setState(() => _processing = false);
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
          title: const Text('Payment successful'),
          content: Text(
            'Your 60% advance payment for order ${paidOrder.orderNumber} was successful. The seller will prepare your order for delivery.',
            textAlign: TextAlign.center,
          ),
          actionsAlignment: MainAxisAlignment.center,
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(context),
              style: FilledButton.styleFrom(
                backgroundColor: GemoraColors.darkGreen,
              ),
              child: const Text('View my orders'),
            ),
          ],
        ),
      );
      if (mounted) Navigator.pop(context, true);
    } catch (error) {
      if (mounted) {
        setState(
          () =>
              _paymentError = error.toString().replaceFirst('Exception: ', ''),
        );
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFF8C2E2A),
            content: Text(_paymentError!),
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
        title: Text(_deliveryReady ? 'Card Payment' : 'Delivery Details'),
      ),
      body: BuyerGradientBackground(
        child: SafeArea(
          top: false,
          child: Form(
            key: _deliveryReady ? _formKey : _deliveryFormKey,
            child: ListView(
              controller: _scrollController,
              padding: const EdgeInsets.fromLTRB(18, 22, 18, 38),
              children: [
                Text(
                  _deliveryReady
                      ? 'STEP 2 OF 2 · CARD PAYMENT'
                      : 'STEP 1 OF 2 · DELIVERY DETAILS',
                  style: const TextStyle(
                    color: GemoraColors.darkGreen,
                    fontWeight: FontWeight.w800,
                    fontSize: 12,
                  ),
                ),
                const SizedBox(height: 14),
                if (!_deliveryReady) ...[
                  BuyerGlassPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Text(
                          'Confirm delivery address',
                          style: TextStyle(
                            color: GemoraColors.darkGreen,
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Save the recipient and address before entering card details.',
                        ),
                        const SizedBox(height: 18),
                        for (final field in [
                          (
                            'recipientName',
                            'Recipient name',
                            Icons.person_outline_rounded,
                            TextInputType.name,
                          ),
                          (
                            'recipientPhone',
                            'Phone number',
                            Icons.phone_outlined,
                            TextInputType.phone,
                          ),
                          (
                            'alternatePhone',
                            'Alternate phone (optional)',
                            Icons.phone_android_outlined,
                            TextInputType.phone,
                          ),
                          (
                            'addressLine1',
                            'Street address',
                            Icons.home_outlined,
                            TextInputType.streetAddress,
                          ),
                          (
                            'addressLine2',
                            'Address line 2 (optional)',
                            Icons.home_work_outlined,
                            TextInputType.streetAddress,
                          ),
                          (
                            'city',
                            'City or town',
                            Icons.location_city_outlined,
                            TextInputType.text,
                          ),
                          (
                            'district',
                            'District',
                            Icons.map_outlined,
                            TextInputType.text,
                          ),
                          (
                            'region',
                            'Province or region',
                            Icons.public_outlined,
                            TextInputType.text,
                          ),
                          (
                            'postalCode',
                            'Postal code',
                            Icons.markunread_mailbox_outlined,
                            TextInputType.text,
                          ),
                          (
                            'countryCode',
                            'Country code (e.g. LK)',
                            Icons.flag_outlined,
                            TextInputType.text,
                          ),
                          (
                            'nearestLandmark',
                            'Nearest landmark (optional)',
                            Icons.place_outlined,
                            TextInputType.text,
                          ),
                          (
                            'deliveryInstructions',
                            'Delivery instructions (optional)',
                            Icons.notes_outlined,
                            TextInputType.text,
                          ),
                        ]) ...[
                          _field(
                            controller: _deliveryFields[field.$1]!,
                            label: field.$2,
                            hint: field.$2,
                            icon: field.$3,
                            keyboardType: field.$4,
                            validator: (value) {
                              final text = value?.trim() ?? '';
                              if ({
                                'alternatePhone',
                                'addressLine2',
                                'nearestLandmark',
                                'deliveryInstructions',
                              }.contains(field.$1)) {
                                return null;
                              }
                              if (text.isEmpty) {
                                return '${field.$2} is required';
                              }
                              if (field.$1 == 'countryCode' &&
                                  !RegExp(r'^[a-zA-Z]{2}$').hasMatch(text)) {
                                return 'Use a two-letter country code';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 12),
                        ],
                        FilledButton.icon(
                          onPressed: _processing ? null : _saveDelivery,
                          icon: _processing
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(Icons.arrow_forward_rounded),
                          label: Text(
                            _processing
                                ? 'Saving delivery details...'
                                : 'Save and continue to card payment',
                          ),
                          style: FilledButton.styleFrom(
                            backgroundColor: GemoraColors.darkGreen,
                            padding: const EdgeInsets.symmetric(vertical: 16),
                          ),
                        ),
                      ],
                    ),
                  ),
                ] else ...[
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
                              'DEMO CARD CHECKOUT',
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
                          '60% advance payment. Use test card 4242 4242 4242 4242 for success or 4000 0000 0000 0002 for decline. Do not enter a real card.',
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
                  if (_paymentError != null) ...[
                    BuyerGlassPanel(
                      child: Text(
                        'Payment failed: $_paymentError. Check the details and try again.',
                        style: const TextStyle(
                          color: Color(0xFF8C2E2A),
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    const SizedBox(height: 18),
                  ],
                  BuyerGlassPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Text(
                          'Deliver to',
                          style: TextStyle(fontWeight: FontWeight.w800),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          [
                            _deliveryFields['recipientName']!.text.trim(),
                            _deliveryFields['recipientPhone']!.text.trim(),
                            _deliveryFields['addressLine1']!.text.trim(),
                            _deliveryFields['addressLine2']!.text.trim(),
                            '${_deliveryFields['city']!.text.trim()}, ${_deliveryFields['district']!.text.trim()}',
                            '${_deliveryFields['region']!.text.trim()} ${_deliveryFields['postalCode']!.text.trim()}',
                            _deliveryFields['countryCode']!.text
                                .trim()
                                .toUpperCase(),
                          ].where((line) => line.isNotEmpty).join('\n'),
                        ),
                        Align(
                          alignment: Alignment.centerLeft,
                          child: TextButton.icon(
                            onPressed: _processing
                                ? null
                                : () => _showStep(payment: false),
                            icon: const Icon(Icons.edit_location_alt_outlined),
                            label: const Text('Edit delivery details'),
                          ),
                        ),
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
                          onSelectionChanged: _processing
                              ? null
                              : (value) =>
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
                              : 'Use one of the demo card numbers shown above',
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
      enabled: !_processing,
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
  return digits == '4242424242424242' || digits == '4000000000000002';
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

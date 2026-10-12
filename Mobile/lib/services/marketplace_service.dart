import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/marketplace_gem_model.dart';
import '../models/marketplace/order_model.dart';
import 'token_storage_service.dart';

class MarketplaceService {
  final TokenStorageService _tokenStorage = TokenStorageService();
  final http.Client? client;
  final Future<String?> Function()? tokenLoader;

  MarketplaceService({this.client, this.tokenLoader});

  Future<http.Response> _get(Uri uri, {Map<String, String>? headers}) =>
      client != null
      ? client!.get(uri, headers: headers)
      : http.get(uri, headers: headers);

  Future<http.Response> _post(
    Uri uri, {
    Map<String, String>? headers,
    Object? body,
  }) => client != null
      ? client!.post(uri, headers: headers, body: body)
      : http.post(uri, headers: headers, body: body);

  Future<List<MarketplaceGemModel>> getGems({
    String? search,
    String? gemType,
  }) async {
    final queryParameters = <String, String>{'page': '1', 'pageSize': '50'};

    final cleanSearch = search?.trim() ?? '';
    final cleanGemType = gemType?.trim() ?? '';

    if (cleanSearch.isNotEmpty) {
      queryParameters['search'] = cleanSearch;
    }

    if (cleanGemType.isNotEmpty && cleanGemType.toLowerCase() != 'all') {
      queryParameters['gemType'] = cleanGemType;
    }

    final uri = Uri.parse('${ApiConfig.baseUrl}/marketplace/gems')
        .replace(queryParameters: queryParameters);

    final response = await _get(
      uri,
      headers: const {'Accept': 'application/json'},
    );

    _ensureSuccess(response, 'We could not load the gemstone marketplace.');

    final decoded = _decode(response.body);
    final items = decoded is Map<String, dynamic> ? decoded['items'] : decoded;

    if (items is! List) {
      throw Exception('The marketplace returned an unexpected response.');
    }

    return items
        .whereType<Map>()
        .map(
          (item) =>
              MarketplaceGemModel.fromJson(Map<String, dynamic>.from(item)),
        )
        .toList();
  }

  Future<List<MarketplaceGemModel>> getBuyerGems({String? search}) {
    return getGems(search: search);
  }

  Future<MarketplaceGemModel> getGem(int id) async {
    final response = await _get(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/gems/$id'),
      headers: const {'Accept': 'application/json'},
    );

    _ensureSuccess(response, 'This gemstone is not currently available.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception(
        'The marketplace returned an unexpected gemstone response.',
      );
    }

    return MarketplaceGemModel.fromJson(decoded);
  }

  Future<Map<String, dynamic>> askAgent(
    String message, {
    String? conversationId,
  }) async {
    final response = await _post(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/agent/assist'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'message': message.trim(),
        'conversationId': ?conversationId,
      }),
    );

    _ensureSuccess(response, 'The buyer assistant is unavailable.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The buyer assistant returned an unexpected response.');
    }

    return decoded;
  }

  Future<List<OrderModel>> getMyOrders() async {
    final response = await _get(
      Uri.parse('${ApiConfig.baseUrl}/orders/my'),
      headers: await _authenticatedHeaders(),
    );

    _ensureSuccess(response, 'We could not load your orders.');

    final decoded = _decode(response.body);

    if (decoded is! List) {
      throw Exception('The orders service returned an unexpected response.');
    }

    return decoded
        .whereType<Map<String, dynamic>>()
        .map(OrderModel.fromJson)
        .toList();
  }

  Future<OrderModel> createOrder(
    int gemListingId, {
    Map<String, dynamic>? deliveryDetails,
    String shippingAddress = 'Main Street',
    String shippingRegion = 'Western',
    String shippingCountryCode = 'LK',
  }) async {
    final response = await _post(
      Uri.parse('${ApiConfig.baseUrl}/orders'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'gemListingId': gemListingId,
        'deliveryDetails': ?deliveryDetails,
        if (deliveryDetails == null) ...{
          'shippingAddress': shippingAddress.trim(),
          'shippingRegion': shippingRegion.trim(),
          'shippingCountryCode': shippingCountryCode.trim().toUpperCase(),
        },
      }),
    );

    _ensureSuccess(response, 'We could not create the order.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The order service returned an unexpected response.');
    }

    return OrderModel.fromJson(decoded);
  }

  Future<OrderModel> cancelOrder(
    String orderId, {
    String reason = 'Cancelled by buyer.',
  }) async {
    final response = await _post(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/cancel'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({'reason': reason}),
    );

    _ensureSuccess(response, 'We could not cancel this order.');

    final decoded = _decode(response.body);

    if (decoded is! Map<String, dynamic>) {
      throw Exception('The order service returned an unexpected response.');
    }

    return OrderModel.fromJson(decoded);
  }

  Future<Map<String, dynamic>> createPaymentIntent(String orderId) async {
    final response = await _post(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/payment/intent'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({'paymentMethod': 'Card'}),
    );

    _ensureSuccess(response, 'We could not start your card payment.');
    final decoded = _decode(response.body);
    if (decoded is! Map<String, dynamic>) {
      throw Exception('The payment service returned an unexpected response.');
    }
    return decoded;
  }

  Future<OrderModel> confirmCardPayment({
    required String orderId,
    required String paymentIntentId,
    required String paymentMethodToken,
  }) async {
    final response = await _post(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/payment/confirm'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'paymentIntentId': paymentIntentId,
        'paymentMethodToken': paymentMethodToken,
        'paymentMethod': 'Card',
      }),
    );

    _ensureSuccess(response, 'Your card payment could not be completed.');
    final decoded = _decode(response.body);
    final order = decoded is Map<String, dynamic> ? decoded['order'] : null;
    if (order is! Map) {
      throw Exception('The payment service returned an unexpected response.');
    }
    return OrderModel.fromJson(Map<String, dynamic>.from(order));
  }

  Future<List<OrderModel>> getSellerOrders() async {
    final response = await _get(
      Uri.parse(ApiConfig.sellerOrders),
      headers: await _authenticatedHeaders(),
    );

    _ensureSuccess(response, 'Could not load seller orders.');
    final decoded = _decode(response.body);
    if (decoded is! List) {
      throw Exception('Seller orders returned an unexpected response.');
    }
    return decoded
        .whereType<Map>()
        .map((item) => OrderModel.fromJson(Map<String, dynamic>.from(item)))
        .toList();
  }

  Future<OrderModel> approveOrder(String orderId) => _postOrderAction(
    orderId,
    'approve',
    sellerRoute: true,
    fallback: 'Could not approve this order.',
  );

  Future<OrderModel> rejectOrder(
    String orderId, {
    required String reason,
    bool alreadySold = false,
  }) => _postOrderAction(
    orderId,
    'reject',
    sellerRoute: true,
    body: {'reason': reason.trim(), 'alreadySold': alreadySold},
    fallback: 'Could not reject this order.',
  );

  Future<OrderModel> startPreparing(String orderId) => _postOrderAction(
    orderId,
    'prepare',
    fallback: 'Could not start preparing this order.',
  );

  Future<OrderModel> markReadyForDispatch(String orderId) => _postOrderAction(
    orderId,
    'ready-for-dispatch',
    fallback: 'Could not mark this order ready for dispatch.',
  );

  Future<OrderModel> handOverToCourier(
    String orderId, {
    required String courierName,
    required String trackingNumber,
    String? trackingUrl,
    DateTime? expectedDeliveryDate,
    String? dispatchNote,
  }) => _postOrderAction(
    orderId,
    'handover',
    body: {
      'courierName': courierName.trim(),
      'trackingNumber': trackingNumber.trim(),
      'trackingUrl': trackingUrl?.trim(),
      'expectedDeliveryDate': expectedDeliveryDate?.toUtc().toIso8601String(),
      'dispatchNote': dispatchNote?.trim(),
    },
    fallback: 'Could not hand this order over to the courier.',
  );

  Future<OrderModel> markInTransit(String orderId) => _postOrderAction(
    orderId,
    'in-transit',
    fallback: 'Could not mark this order in transit.',
  );

  Future<OrderModel> markOutForDelivery(String orderId) => _postOrderAction(
    orderId,
    'out-for-delivery',
    fallback: 'Could not mark this order out for delivery.',
  );

  Future<OrderModel> markDelivered(String orderId) => _postOrderAction(
    orderId,
    'delivered',
    fallback: 'Could not mark this order delivered.',
  );

  Future<OrderModel> completeOrder(String orderId) => _postOrderAction(
    orderId,
    'complete',
    body: {'reason': 'Buyer confirmed successful delivery.'},
    fallback: 'Could not confirm delivery.',
  );

  Future<OrderModel> _postOrderAction(
    String orderId,
    String action, {
    Map<String, dynamic>? body,
    bool sellerRoute = false,
    required String fallback,
  }) async {
    final path = sellerRoute ? 'seller/orders' : 'orders';
    final response = await _post(
      Uri.parse('${ApiConfig.baseUrl}/$path/$orderId/$action'),
      headers: {
        ...await _authenticatedHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode(body ?? const <String, dynamic>{}),
    );

    _ensureSuccess(response, fallback);
    final decoded = _decode(response.body);
    if (decoded is! Map) {
      throw Exception('$fallback The server returned an invalid response.');
    }
    return OrderModel.fromJson(Map<String, dynamic>.from(decoded));
  }

  Future<Map<String, String>> _authenticatedHeaders() async {
    final token = await (tokenLoader?.call() ?? _tokenStorage.getToken());

    return {
      'Accept': 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };
  }

  dynamic _decode(String body) {
    try {
      return jsonDecode(body);
    } catch (_) {
      throw Exception('The server returned an invalid response.');
    }
  }

  void _ensureSuccess(http.Response response, String fallback) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return;
    }

    throw Exception(_errorMessage(response, fallback));
  }

  String _errorMessage(http.Response response, String fallback) {
    try {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        final message =
            (decoded['message'] ?? decoded['detail'] ?? decoded['title'])
                ?.toString()
                .trim();

        if (message != null && message.isNotEmpty) {
          return message;
        }
      }
    } catch (_) {
      // Use the endpoint-specific fallback for non-JSON errors.
    }

    return fallback;
  }
}

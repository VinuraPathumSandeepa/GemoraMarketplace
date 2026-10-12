import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/marketplace/marketplace_gem.dart';
import '../models/marketplace/order_model.dart';
import 'token_storage_service.dart';

class MarketplaceService {
  final TokenStorageService _tokenStorage = TokenStorageService();
  final http.Client? client;
  final Future<String?> Function()? tokenLoader;

  MarketplaceService({this.client, this.tokenLoader});

  Future<Map<String, String>> _authHeaders({bool isJson = false}) async {
    final token = await (tokenLoader?.call() ?? _tokenStorage.getToken());

    final headers = <String, String>{
      'Accept': 'application/json',
      if (isJson) 'Content-Type': 'application/json',
      if (token != null && token.trim().isNotEmpty)
        'Authorization': 'Bearer ${token.trim()}',
    };

    return headers;
  }

  Future<List<MarketplaceGem>> getGems({
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

    final httpClient = client;
    final response = httpClient != null
        ? await httpClient.get(
            uri,
            headers: const {'Accept': 'application/json'},
          )
        : await http.get(uri, headers: const {'Accept': 'application/json'});

    _ensureSuccess(response, 'We could not load the gemstone marketplace.');

    final decoded = _decode(response.body);
    final dynamic responseItems = decoded is Map ? decoded['items'] : decoded;

    if (responseItems is! List) {
      throw Exception('The marketplace returned an unexpected response.');
    }

    return responseItems
        .whereType<Map>()
        .map((item) => MarketplaceGem.fromJson(Map<String, dynamic>.from(item)))
        .toList();
  }

  Future<MarketplaceGem> getGem(int id) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/Marketplace/gems/$id');
    final httpClient = client;
    final response = httpClient != null
        ? await httpClient.get(
            uri,
            headers: const {'Accept': 'application/json'},
          )
        : await http.get(uri, headers: const {'Accept': 'application/json'});

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is! Map) {
        throw Exception(
          'The marketplace returned an unexpected gemstone response.',
        );
      }

      return MarketplaceGem.fromJson(Map<String, dynamic>.from(decoded));
    }

    throw Exception(
      _errorMessage(response, 'This gemstone is not currently available.'),
    );
  }

  Future<List<MarketplaceGem>> getBuyerGems({String? search}) {
    return getGems(search: search);
  }

  Future<Map<String, dynamic>> askAgent(String message) async {
    final decoded = await _request(
      Uri.parse('${ApiConfig.baseUrl}/marketplace/agent/assist'),
      body: {'message': message.trim()},
      fallback: 'Could not get response from AI assistant.',
    );

    if (decoded is Map<String, dynamic>) {
      return decoded;
    }

    throw Exception('The AI assistant returned an unexpected response format.');
  }

  Future<OrderModel> createOrder(
    int gemListingId, {
    String shippingAddress = 'Main Street',
    String shippingRegion = 'Western',
    String shippingCountryCode = 'LK',
    Map<String, dynamic>? deliveryDetails,
  }) async {
    final headers = await _authHeaders(isJson: true);

    final payload = jsonEncode({
      'gemListingId': gemListingId,
      'shippingAddress': shippingAddress.trim(),
      'shippingRegion': shippingRegion.trim(),
      'shippingCountryCode': shippingCountryCode.trim().toUpperCase(),
      'deliveryDetails': ?deliveryDetails,
    });

    final uri = Uri.parse('${ApiConfig.baseUrl}/orders');
    final httpClient = client;
    final response = httpClient != null
        ? await httpClient.post(uri, headers: headers, body: payload)
        : await http.post(uri, headers: headers, body: payload);

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is Map<String, dynamic>) {
        return OrderModel.fromJson(decoded);
      }

      throw Exception('Order creation returned an invalid response.');
    }

    throw Exception(_errorMessage(response, 'Could not create order.'));
  }

  Future<List<OrderModel>> getMyOrders() async {
    final headers = await _authHeaders();
    final uri = Uri.parse(ApiConfig.myOrders);
    final response = client != null
        ? await client!.get(uri, headers: headers)
        : await http.get(uri, headers: headers);

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is List) {
        return decoded
            .whereType<Map<String, dynamic>>()
            .map(OrderModel.fromJson)
            .toList();
      }

      throw Exception('My orders returned an unexpected response.');
    }

    throw Exception(_errorMessage(response, 'Could not load your orders.'));
  }

  Future<List<OrderModel>> getSellerOrders() async {
    final headers = await _authHeaders();

    final uri = Uri.parse(ApiConfig.sellerOrders);
    final response = client != null
        ? await client!.get(uri, headers: headers)
        : await http.get(uri, headers: headers);

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final decoded = jsonDecode(response.body);

      if (decoded is List) {
        return decoded
            .whereType<Map<String, dynamic>>()
            .map(OrderModel.fromJson)
            .toList();
      }

      throw Exception('Seller orders returned an unexpected response.');
    }

    throw Exception(_errorMessage(response, 'Could not load seller orders.'));
  }

  Future<void> cancelOrder(dynamic orderId) async {
    await _postOrderAction(
      orderId,
      'cancel',
      fallback: 'Could not cancel order.',
    );
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

  Future<PaymentIntentModel> createPaymentIntent(String orderId) async {
    final decoded = await _request(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/payment/intent'),
      body: {'paymentMethod': 'Card'},
      fallback: 'Could not start payment.',
    );
    if (decoded is Map) {
      return PaymentIntentModel.fromJson(Map<String, dynamic>.from(decoded));
    }
    throw Exception('Payment setup returned an unexpected response.');
  }

  Future<OrderModel> confirmPayment(
    String orderId, {
    required String paymentIntentId,
    required String paymentMethodToken,
  }) async {
    final decoded = await _request(
      Uri.parse('${ApiConfig.baseUrl}/orders/$orderId/payment/confirm'),
      body: {
        'paymentIntentId': paymentIntentId,
        'paymentMethodToken': paymentMethodToken,
        'paymentMethod': 'Card',
      },
      fallback: 'Could not confirm payment.',
    );
    if (decoded is Map && decoded['order'] is Map) {
      return OrderModel.fromJson(
        Map<String, dynamic>.from(decoded['order'] as Map),
      );
    }
    throw Exception('Payment confirmation returned an unexpected response.');
  }

  Future<OrderModel> _postOrderAction(
    dynamic orderId,
    String action, {
    Map<String, dynamic>? body,
    bool sellerRoute = false,
    required String fallback,
  }) async {
    final headers = await _authHeaders(isJson: true);
    final path = sellerRoute ? 'seller/orders' : 'orders';
    final uri = Uri.parse('${ApiConfig.baseUrl}/$path/$orderId/$action');
    final payload = jsonEncode(body ?? <String, dynamic>{});
    final response = client != null
        ? await client!.post(uri, headers: headers, body: payload)
        : await http.post(uri, headers: headers, body: payload);
    _ensureSuccess(response, fallback);
    final decoded = _decode(response.body);
    if (decoded is Map) {
      return OrderModel.fromJson(Map<String, dynamic>.from(decoded));
    }
    throw Exception('$fallback The server returned an invalid response.');
  }

  Future<dynamic> _request(
    Uri uri, {
    Map<String, dynamic>? body,
    bool authenticated = true,
    String? fallback,
  }) async {
    final headers = <String, String>{
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    };

    if (authenticated) {
      final token = await (tokenLoader?.call() ?? _tokenStorage.getToken());
      if (token == null || token.trim().isEmpty) {
        throw Exception('Please sign in to continue.');
      }
      headers['Authorization'] = 'Bearer ${token.trim()}';
    }

    final httpClient = client;
    final response =
        await (body == null
                ? (httpClient?.get(uri, headers: headers) ??
                      http.get(uri, headers: headers))
                : (httpClient?.post(
                        uri,
                        headers: headers,
                        body: jsonEncode(body),
                      ) ??
                      http.post(uri, headers: headers, body: jsonEncode(body))))
            .timeout(const Duration(seconds: 60));

    _ensureSuccess(
      response,
      fallback ?? 'The request failed (${response.statusCode}).',
    );

    return response.body.isEmpty ? null : _decode(response.body);
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

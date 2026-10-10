import 'package:flutter/material.dart';
import '../../models/marketplace/order_model.dart';
import '../../services/marketplace_service.dart';

class MyOrdersScreen extends StatefulWidget {
  const MyOrdersScreen({super.key});
  @override
  State<MyOrdersScreen> createState() => _MyOrdersScreenState();
}

class _MyOrdersScreenState extends State<MyOrdersScreen> {
  final _service = MarketplaceService();
  bool _loading = true;
  List<OrderModel> _orders = [];
  String? _error;

  @override
  void initState() { super.initState(); _load(); }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try { _orders = await _service.getMyOrders(); }
    catch (e) { _error = e.toString().replaceFirst('Exception: ', ''); }
    finally { if (mounted) setState(() => _loading = false); }
  }

  Future<void> _cancel(OrderModel order) async {
    try { await _service.cancelOrder(order.id); await _load(); }
    catch (e) { if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString().replaceFirst('Exception: ', '')))); }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return Center(child: Text(_error!));
    if (_orders.isEmpty) return const Center(child: Text('You have no orders yet.'));
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _orders.length,
        itemBuilder: (_, index) {
          final o = _orders[index];
          final canCancel = o.status == 'Pending' || o.status == 'Confirmed';
          return Card(
            margin: const EdgeInsets.only(bottom: 14),
            child: ExpansionTile(
              title: Text(o.gemTitle, style: const TextStyle(fontWeight: FontWeight.w700)),
              subtitle: Text('${o.orderNumber}\n${o.currency} ${o.agreedPrice.toStringAsFixed(2)} • ${o.status}'),
              childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              children: [
                ...o.statusHistory.map((h) => ListTile(
                  dense: true,
                  leading: const Icon(Icons.circle, size: 12),
                  title: Text(h.newStatus),
                  subtitle: Text('${h.changedByName}${h.reason == null ? '' : ' • ${h.reason}'}'),
                )),
                if (canCancel) Align(alignment: Alignment.centerRight, child: OutlinedButton(onPressed: () => _cancel(o), child: const Text('Cancel order'))),
              ],
            ),
          );
        },
      ),
    );
  }
}

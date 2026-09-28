import 'package:flutter/material.dart';

import '../../models/export_request_model.dart';
import '../../services/export_compliance_service.dart';
import 'create_export_request_screen.dart';
import 'export_request_detail_screen.dart';

class MyExportRequestsScreen extends StatefulWidget {
  const MyExportRequestsScreen({super.key});

  @override
  State<MyExportRequestsScreen> createState() => _MyExportRequestsScreenState();
}

class _MyExportRequestsScreenState extends State<MyExportRequestsScreen> {
  final _exportService = ExportComplianceService();

  late Future<List<ExportRequestModel>> _requestsFuture;

  @override
  void initState() {
    super.initState();
    _loadRequests();
  }

  void _loadRequests() {
    setState(() {
      _requestsFuture = _exportService.getMyExportRequests();
    });
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
        return Colors.blueGrey;
      case 'submitted':
        return Colors.blue;
      case 'undercompliancereview':
        return Colors.purple;
      case 'underofficerreview':
        return Colors.orange;
      case 'revisionrequired':
        return Colors.amber.shade800;
      case 'approved':
        return Colors.green;
      case 'rejected':
        return Colors.red;
      case 'cancelled':
        return Colors.grey;
      default:
        return Colors.blueGrey;
    }
  }

  void _openCreateScreen() {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => const CreateExportRequestScreen(),
      ),
    ).then((_) => _loadRequests());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Export Requests'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _loadRequests,
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _openCreateScreen,
        icon: const Icon(Icons.add),
        label: const Text('Create Request'),
      ),
      body: FutureBuilder<List<ExportRequestModel>>(
        future: _requestsFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      size: 60,
                      color: Colors.red,
                    ),
                    const SizedBox(height: 16),
                    Text(
                      snapshot.error.toString().replaceAll('Exception: ', ''),
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 16),
                    ),
                    const SizedBox(height: 20),
                    FilledButton.icon(
                      onPressed: _loadRequests,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            );
          }

          final requests = snapshot.data ?? [];

          if (requests.isEmpty) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.description_outlined,
                      size: 72,
                      color: Colors.grey,
                    ),
                    const SizedBox(height: 16),
                    const Text(
                      "You haven't created any export requests yet.",
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 24),
                    FilledButton.icon(
                      onPressed: _openCreateScreen,
                      icon: const Icon(Icons.add),
                      label: const Text('Create Export Request'),
                    ),
                  ],
                ),
              ),
            );
          }

          return RefreshIndicator(
            onRefresh: () async => _loadRequests(),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: requests.length,
              itemBuilder: (context, index) {
                final req = requests[index];
                final statusColor = _getStatusColor(req.status);

                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  elevation: 2,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: ListTile(
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 8,
                    ),
                    title: Text(
                      '${req.originCountry} → ${req.destinationCountry}',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                      ),
                    ),
                    subtitle: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SizedBox(height: 4),
                        Text(
                          'Declared Value: ${req.declaredValue.toStringAsFixed(2)} ${req.currency}',
                          style: const TextStyle(fontSize: 14),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          'Created: ${req.createdAt.toLocal().toString().split('.')[0]}',
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey.shade600,
                          ),
                        ),
                      ],
                    ),
                    trailing: Chip(
                      label: Text(
                        req.status,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      backgroundColor: statusColor,
                      padding: EdgeInsets.zero,
                    ),
                    onTap: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) =>
                              ExportRequestDetailScreen(requestId: req.id),
                        ),
                      ).then((_) => _loadRequests());
                    },
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

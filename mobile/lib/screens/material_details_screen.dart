import 'package:mobile/widgets/location_card.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/routing/app_router.dart';

class MaterialDetailsScreen extends StatefulWidget {
  const MaterialDetailsScreen({
    required this.gateway,
    required this.user,
    required this.listingId,
    this.locationLookup,
    super.key,
  });

  final MaterialInventoryGateway gateway;
  final AppUser user;
  final String listingId;
  final AddressLookup? locationLookup;

  @override
  State<MaterialDetailsScreen> createState() => _MaterialDetailsScreenState();
}

class _MaterialDetailsScreenState extends State<MaterialDetailsScreen> {
  MaterialListing? _listing;
  List<MaterialListingHistoryEntry> _history = const [];
  bool _isLoading = true;
  bool _isActionBusy = false;
  String? _error;
  String? _historyError;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
      _historyError = null;
    });
    try {
      final listing = await widget.gateway.getById(widget.listingId);
      List<MaterialListingHistoryEntry> history = const [];
      String? historyError;
      if (widget.user.role == AppRole.seller ||
          widget.user.role == AppRole.manager) {
        try {
          history = await widget.gateway.history(widget.listingId);
        } on ApiException catch (error) {
          historyError = error.message;
        } on Object {
          historyError = 'Unable to load listing history.';
        }
      }
      if (!mounted) return;
      setState(() {
        _listing = listing;
        _history = history;
        _historyError = historyError;
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } on Object {
      if (mounted) setState(() => _error = 'Unable to load this material.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _publish() async {
    setState(() => _isActionBusy = true);
    try {
      await widget.gateway.publish(widget.listingId);
      if (mounted) {
        _showMessage('Listing submitted for manager verification.');
        await _load();
      }
    } on ApiException catch (error) {
      if (mounted) _showMessage(error.message);
    } on Object {
      if (mounted) _showMessage('Unable to publish this listing.');
    } finally {
      if (mounted) setState(() => _isActionBusy = false);
    }
  }

  Future<void> _delete() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete material?'),
        content: const Text('This cannot be undone.'),
        actions: [
          TextButton(
            onPressed: () => context.pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => context.pop(true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    setState(() => _isActionBusy = true);
    try {
      await widget.gateway.delete(widget.listingId);
      if (mounted) {
        if (context.canPop()) {
          context.pop(true);
        } else {
          context.go('/materials');
        }
      }
    } on ApiException catch (error) {
      if (mounted) _showMessage(error.message);
    } on Object {
      if (mounted) _showMessage('Unable to delete this listing.');
    } finally {
      if (mounted) setState(() => _isActionBusy = false);
    }
  }

  void _showMessage(String message) =>
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(message)));

  @override
  Widget build(BuildContext context) {
    final listing = _listing;
    return Scaffold(
      appBar: AppBar(
        leading: const DashboardBackButton(fallback: '/materials'),
        title: const Text('Material Details'),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            onPressed: _isLoading ? null : _load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? _DetailsError(message: _error!, onRetry: _load)
          : listing == null
          ? const SizedBox.shrink()
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(
                  listing.title,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  children: [
                    Chip(label: Text(listing.status)),
                    Chip(label: Text(listing.condition)),
                    Chip(label: Text(listing.categoryName)),
                  ],
                ),
                const SizedBox(height: 12),
                Text(listing.description),
                const SizedBox(height: 16),
                _DetailsTable(
                  listing: listing,
                  locationLookup: widget.locationLookup,
                ),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Seller',
                          style: TextStyle(fontWeight: FontWeight.bold),
                        ),
                        Text(
                          listing.seller?.fullName?.isNotEmpty == true
                              ? listing.seller!.fullName!
                              : 'Seller profile not completed',
                        ),
                        if (listing.seller?.businessName?.isNotEmpty == true)
                          Text(listing.seller!.businessName!),
                        if (listing.seller != null)
                          SelectableText(listing.seller!.email),
                        if (listing.seller?.phoneNumber?.isNotEmpty == true)
                          SelectableText(listing.seller!.phoneNumber!),
                      ],
                    ),
                  ),
                ),
                if (listing.photos.isNotEmpty) ...[
                  const SizedBox(height: 20),
                  Text(
                    'Photos',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 8),
                  SizedBox(
                    height: 160,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: listing.photos.length,
                      separatorBuilder: (_, _) => const SizedBox(width: 8),
                      itemBuilder: (_, index) => ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: Image.network(
                          widget.gateway.photoUrl(
                            listing.photos[index].photoUrl,
                          ),
                          width: 200,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => const SizedBox(
                            width: 200,
                            child: Center(
                              child: Icon(Icons.broken_image_outlined),
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
                if (widget.user.role == AppRole.seller) ...[
                  const SizedBox(height: 20),
                  if (listing.canEdit)
                    FilledButton.icon(
                      key: const Key('edit-material'),
                      onPressed: _isActionBusy
                          ? null
                          : () async {
                              final changed = await context.push<bool>(
                                '${AppRoutes.materials}/${listing.id}/edit',
                              );
                              if (changed == true && mounted) await _load();
                            },
                      icon: const Icon(Icons.edit_outlined),
                      label: const Text('Edit material'),
                    ),
                  if (listing.canPublish) ...[
                    const SizedBox(height: 8),
                    FilledButton.icon(
                      key: const Key('publish-material'),
                      onPressed: _isActionBusy ? null : _publish,
                      icon: const Icon(Icons.publish_outlined),
                      label: const Text('Publish for verification'),
                    ),
                  ],
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    key: const Key('delete-material'),
                    onPressed: _isActionBusy ? null : _delete,
                    icon: const Icon(Icons.delete_outline),
                    label: const Text('Delete material'),
                  ),
                ],
                const SizedBox(height: 24),
                Text(
                  'Listing history',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 8),
                if (_historyError != null)
                  Text(
                    _historyError!,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  )
                else if (_history.isEmpty)
                  const Text('No history entries are available yet.')
                else
                  for (final entry in _history)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Icons.history),
                      title: Text(entry.action.replaceAll('_', ' ')),
                      subtitle: Text(
                        entry.createdAtUtc.toString().substring(0, 16),
                      ),
                    ),
              ],
            ),
    );
  }
}

class _DetailsTable extends StatelessWidget {
  const _DetailsTable({required this.listing, this.locationLookup});
  final MaterialListing listing;
  final AddressLookup? locationLookup;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Quantity: ${listing.quantity.toStringAsFixed(2)} ${listing.unit}',
          ),
          Text(
            'Reserved: ${listing.reservedQuantity.toStringAsFixed(2)} ${listing.unit}',
          ),
          Text(
            'Remaining: ${listing.remainingQuantity.toStringAsFixed(2)} ${listing.unit}',
          ),
          Text('Unit price: Rs. ${listing.unitPrice.toStringAsFixed(2)}'),
          Text(
            'Available until: ${listing.availableUntil.toString().substring(0, 16)}',
          ),
          if (listing.latitude != null && listing.longitude != null)
            LocationCard(
              latitude: listing.latitude!,
              longitude: listing.longitude!,
              lookup: locationLookup ?? unavailableAddress,
            ),
        ],
      ),
    ),
  );
}

class _DetailsError extends StatelessWidget {
  const _DetailsError({required this.message, required this.onRetry});
  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 48),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
    ),
  );
}

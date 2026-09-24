import 'package:mobile/materials/material_quantity_display.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';
import 'package:mobile/categories/category_filter.dart';
import 'package:mobile/categories/material_category.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/routing/app_router.dart';
import 'package:mobile/widgets/role_navigation.dart';

class MyMaterialsScreen extends StatefulWidget {
  const MyMaterialsScreen({
    required this.gateway,
    required this.user,
    super.key,
  });

  final MaterialInventoryGateway gateway;
  final AppUser user;

  @override
  State<MyMaterialsScreen> createState() => _MyMaterialsScreenState();
}

class _MyMaterialsScreenState extends State<MyMaterialsScreen> {
  final _searchController = TextEditingController();
  String? _category;
  final List<MaterialListing> _listings = [];
  String? _status = 'ACTIVE';
  int _filterReset = 0;
  int _page = 0;
  int _totalPages = 0;
  int _totalCount = 0;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.user.hasRole(AppRole.seller)) _load(reset: true);
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  MaterialListingQuery _query(int page) => MaterialListingQuery(
    mineOnly: true,
    search: _searchController.text,
    category: _category,
    status: _status,
    page: page,
  );

  Future<void> _load({required bool reset}) async {
    final requestedPage = reset ? 1 : _page + 1;
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await widget.gateway.search(_query(requestedPage));
      if (!mounted) return;
      setState(() {
        _page = result.page;
        _totalPages = result.totalPages;
        _totalCount = result.totalCount;
        if (reset) _listings.clear();
        _listings.addAll(result.items);
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } on FormatException {
      if (mounted) {
        setState(() => _error = 'The API returned invalid material data.');
      }
    } on Object {
      if (mounted) setState(() => _error = 'Unable to load your materials.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!widget.user.hasRole(AppRole.seller)) {
      return Scaffold(
        appBar: AppBar(
          leading: const DashboardBackButton(fallback: '/home'),
          title: const Text('My Materials'),
        ),
        body: const Center(
          child: Text('Material inventory is available to seller accounts.'),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        leading: const DashboardBackButton(fallback: '/home'),
        title: const Text('My Materials'),
      ),
      floatingActionButton: FloatingActionButton.extended(
        key: const Key('add-material-button'),
        onPressed: () async {
          final changed = await context.push<bool>(AppRoutes.addMaterial);
          if (changed == true && mounted) await _load(reset: true);
        },
        icon: const Icon(Icons.add),
        label: const Text('Add material'),
      ),
      body: RefreshIndicator(
        onRefresh: () => _load(reset: true),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            _FilterPanel(
              key: ValueKey(_filterReset),
              onReset: () {
                setState(() {
                  _searchController.clear();
                  _category = null;
                  _status = 'ACTIVE';
                  _filterReset++;
                });
                _load(reset: true);
              },
              searchController: _searchController,
              loadCategories: widget.gateway.categories,
              onCategoryChanged: (value) => setState(() => _category = value),
              status: _status,
              onStatusChanged: (value) => setState(() => _status = value),
              onApply: () => _load(reset: true),
            ),
            const SizedBox(height: 16),
            if (_isLoading && _listings.isEmpty)
              const Padding(
                padding: EdgeInsets.all(48),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_error != null)
              _ErrorState(message: _error!, onRetry: () => _load(reset: true))
            else if (_listings.isEmpty)
              const _EmptyState()
            else ...[
              Text('$_totalCount material${_totalCount == 1 ? '' : 's'} found'),
              const SizedBox(height: 8),
              for (final listing in _listings)
                Card(
                  child: ListTile(
                    key: Key('material-card-${listing.id}'),
                    title: Text(listing.title),
                    subtitle: Text(
                      '${listing.categoryName}\n'
                      'Total Quantity: ${formatMaterialQuantity(listing.quantity, listing.unit)} ${listing.unit}\n'
                      'Reserved Quantity: ${formatMaterialQuantity(listing.reservedQuantity, listing.unit)} ${listing.unit}\n'
                      'Remaining Stock: ${formatMaterialQuantity(listing.remainingQuantity, listing.unit)} ${listing.unit}\n'
                      '${listing.status} · LKR ${listing.unitPrice.toStringAsFixed(2)}',
                    ),
                    isThreeLine: true,
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () async {
                      await context.push<bool>(
                        '${AppRoutes.materials}/${listing.id}',
                      );
                      // Details can publish or edit before a normal back action.
                      if (mounted) await _load(reset: true);
                    },
                  ),
                ),
              if (_isLoading)
                const Padding(
                  padding: EdgeInsets.all(12),
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (_page < _totalPages)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: OutlinedButton.icon(
                    key: const Key('load-more-materials'),
                    onPressed: () => _load(reset: false),
                    icon: const Icon(Icons.expand_more),
                    label: Text('Load more · page $_page of $_totalPages'),
                  ),
                )
              else
                Padding(
                  padding: const EdgeInsets.all(12),
                  child: Center(child: Text('Page $_page of $_totalPages')),
                ),
            ],
          ],
        ),
      ),
      bottomNavigationBar: RoleNavigation(
        user: widget.user,
        current: '/materials',
      ),
    );
  }
}

class _FilterPanel extends StatelessWidget {
  const _FilterPanel({
    required this.searchController,
    required this.loadCategories,
    required this.onCategoryChanged,
    required this.status,
    required this.onStatusChanged,
    required this.onApply,
    required this.onReset,
    super.key,
  });

  final TextEditingController searchController;
  final Future<List<MaterialCategory>> Function() loadCategories;
  final ValueChanged<String?> onCategoryChanged;
  final String? status;
  final ValueChanged<String?> onStatusChanged;
  final VoidCallback onApply;
  final VoidCallback onReset;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        children: [
          TextField(
            key: const Key('materials-search'),
            controller: searchController,
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => onApply(),
            decoration: const InputDecoration(
              labelText: 'Search title, description, or category',
              prefixIcon: Icon(Icons.search),
            ),
          ),
          const SizedBox(height: 8),
          CategoryFilter(
            key: const Key('materials-category-filter'),
            loadCategories: loadCategories,
            onChanged: onCategoryChanged,
          ),
          const SizedBox(height: 8),
          DropdownButtonFormField<String>(
            key: const Key('materials-status-filter'),
            initialValue: status,
            decoration: const InputDecoration(labelText: 'Status'),
            items: const [
              DropdownMenuItem(value: null, child: Text('Any status')),
              DropdownMenuItem(value: 'DRAFT', child: Text('DRAFT')),
              DropdownMenuItem(
                value: 'PENDING_VERIFICATION',
                child: Text('PENDING VERIFICATION'),
              ),
              DropdownMenuItem(value: 'ACTIVE', child: Text('ACTIVE')),
              DropdownMenuItem(value: 'REJECTED', child: Text('REJECTED')),
              DropdownMenuItem(value: 'AVAILABLE', child: Text('AVAILABLE')),
              DropdownMenuItem(value: 'RESERVED', child: Text('RESERVED')),
              DropdownMenuItem(value: 'SOLD', child: Text('SOLD')),
              DropdownMenuItem(value: 'CLOSED', child: Text('CLOSED')),
            ],
            onChanged: onStatusChanged,
          ),
          const SizedBox(height: 8),
          const SizedBox(height: 12),
          TextButton(onPressed: onReset, child: const Text('Clear filters')),
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton(
              onPressed: onApply,
              child: const Text('Apply filters'),
            ),
          ),
        ],
      ),
    ),
  );
}

class _EmptyState extends StatelessWidget {
  const _EmptyState();

  @override
  Widget build(BuildContext context) => const Padding(
    padding: EdgeInsets.all(48),
    child: Center(
      child: Column(
        children: [
          Icon(Icons.inventory_2_outlined, size: 64),
          SizedBox(height: 12),
          Text('No materials match these filters.'),
        ],
      ),
    ),
  );
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(32),
    child: Center(
      child: Column(
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

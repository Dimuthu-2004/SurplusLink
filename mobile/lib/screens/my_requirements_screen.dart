import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';
import 'package:mobile/requirements/requirement_widgets.dart';

class MyRequirementsScreen extends StatefulWidget {
  const MyRequirementsScreen({required this.gateway, super.key});
  final RequirementGateway gateway;
  @override
  State<MyRequirementsScreen> createState() => _MyRequirementsScreenState();
}

class _MyRequirementsScreenState extends State<MyRequirementsScreen> {
  final _search = TextEditingController();
  List<RequirementCategory>? _categories;
  RequirementPage<BuyerRequirement>? _data;
  String? _status, _category, _error;
  String _sort = 'createdAt', _sortDir = 'desc';
  DateTimeRange? _range;
  int _pageSize = 10, _generation = 0;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _load([int page = 1]) async {
    final generation = ++_generation;
    final query = RequirementQuery(
      search: _search.text,
      status: _status,
      categoryId: _category,
      deadlineFrom: _range?.start,
      deadlineTo: _range == null
          ? null
          : DateTime(
              _range!.end.year,
              _range!.end.month,
              _range!.end.day,
              23,
              59,
              59,
              999,
              999,
            ),
      sort: _sort,
      sortDir: _sortDir,
      page: page,
      pageSize: _pageSize,
    );
    setState(() {
      _loading = true;
      _error = null;
      _data = null;
    });
    try {
      final categories = _categories ?? await widget.gateway.categories();
      final result = await widget.gateway.my(query);
      if (!mounted || generation != _generation) return;
      setState(() {
        _categories = categories;
        _data = result;
      });
    } on Object catch (error) {
      if (mounted && generation == _generation) {
        setState(() => _error = requirementError(error));
      }
    } finally {
      if (mounted && generation == _generation) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _dates() async {
    final now = DateTime.now();
    final result = await showDateRangePicker(
      context: context,
      initialDateRange: _range,
      firstDate: DateTime(2000),
      lastDate: DateTime(now.year + 100, 12, 31),
    );
    if (result != null && mounted) setState(() => _range = result);
  }

  Future<void> _open(String path) async {
    await context.push(path);
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('My Requirements'),
      leading: const RequirementBackButton(fallback: '/home'),
      actions: [
        IconButton(
          tooltip: 'Refresh requirements',
          onPressed: _loading ? null : _load,
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    floatingActionButton: FloatingActionButton.extended(
      key: const Key('create-requirement'),
      onPressed: () => _open('/requirements/new'),
      icon: const Icon(Icons.add),
      label: const Text('Create'),
    ),
    body: Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 850),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 100),
          children: [
            TextField(
              key: const Key('requirements-search'),
              controller: _search,
              maxLength: 200,
              decoration: const InputDecoration(
                labelText: 'Search category or notes',
                prefixIcon: Icon(Icons.search),
              ),
              onSubmitted: (_) => _load(),
            ),
            ExpansionTile(
              title: const Text('Filter and sort'),
              childrenPadding: const EdgeInsets.all(8),
              children: [
                DropdownButtonFormField<String>(
                  key: ValueKey('status-${_status ?? 'all'}'),
                  initialValue: _status ?? '',
                  decoration: const InputDecoration(labelText: 'Status'),
                  items: [
                    const DropdownMenuItem(
                      value: '',
                      child: Text('All statuses'),
                    ),
                    ...requirementStatuses.map(
                      (x) => DropdownMenuItem(
                        value: x,
                        child: Text(statusLabel(x)),
                      ),
                    ),
                  ],
                  onChanged: (value) =>
                      setState(() => _status = value == '' ? null : value),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  key: ValueKey('category-${_category ?? 'all'}'),
                  initialValue: _category ?? '',
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Category'),
                  items: [
                    const DropdownMenuItem(
                      value: '',
                      child: Text('All categories'),
                    ),
                    ...?_categories?.map(
                      (x) => DropdownMenuItem(value: x.id, child: Text(x.name)),
                    ),
                  ],
                  onChanged: (value) =>
                      setState(() => _category = value == '' ? null : value),
                ),
                const SizedBox(height: 12),
                OutlinedButton.icon(
                  onPressed: _dates,
                  icon: const Icon(Icons.date_range),
                  label: Text(
                    _range == null
                        ? 'Deadline range'
                        : '${requirementDate(_range!.start).split(' ').first} to ${requirementDate(_range!.end).split(' ').first}',
                  ),
                ),
                if (_range != null)
                  TextButton(
                    onPressed: () => setState(() => _range = null),
                    child: const Text('Clear deadline range'),
                  ),
                DropdownButtonFormField<String>(
                  key: ValueKey('sort-$_sort'),
                  initialValue: _sort,
                  decoration: const InputDecoration(labelText: 'Sort by'),
                  items: const [
                    DropdownMenuItem(
                      value: 'createdAt',
                      child: Text('Created date'),
                    ),
                    DropdownMenuItem(
                      value: 'deadline',
                      child: Text('Deadline'),
                    ),
                    DropdownMenuItem(value: 'budget', child: Text('Budget')),
                  ],
                  onChanged: (value) => setState(() => _sort = value!),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  key: ValueKey('direction-$_sortDir'),
                  initialValue: _sortDir,
                  decoration: const InputDecoration(
                    labelText: 'Sort direction',
                  ),
                  items: const [
                    DropdownMenuItem(value: 'asc', child: Text('Ascending')),
                    DropdownMenuItem(value: 'desc', child: Text('Descending')),
                  ],
                  onChanged: (value) => setState(() => _sortDir = value!),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  key: ValueKey(_pageSize),
                  initialValue: _pageSize,
                  decoration: const InputDecoration(
                    labelText: 'Items per page',
                  ),
                  items: [10, 20, 50]
                      .map((x) => DropdownMenuItem(value: x, child: Text('$x')))
                      .toList(),
                  onChanged: (value) => setState(() => _pageSize = value!),
                ),
                TextButton(
                  onPressed: () {
                    setState(() {
                      _search.clear();
                      _status = null;
                      _category = null;
                      _range = null;
                      _sort = 'createdAt';
                      _sortDir = 'desc';
                      _pageSize = 10;
                    });
                    _load();
                  },
                  child: const Text('Reset filters'),
                ),
              ],
            ),
            FilledButton(
              key: const Key('requirements-apply'),
              onPressed: _loading ? null : _load,
              child: const Text('Apply search and filters'),
            ),
            const SizedBox(height: 16),
            if (_loading) const Center(child: CircularProgressIndicator()),
            if (_error != null) RequirementErrorBox(_error!, onRetry: _load),
            if (_data != null) ...[
              Text(
                '${_data!.total} requirements',
                key: const Key('requirements-total'),
              ),
              if (_data!.items.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Text(
                    'No requirements found. Create a requirement or change your filters.',
                  ),
                ),
              ..._data!.items.map(
                (row) => Card(
                  child: ListTile(
                    key: ValueKey(row.id),
                    title: Text(
                      _categories!
                              .where((x) => x.id == row.categoryId)
                              .firstOrNull
                              ?.name ??
                          'Requirement',
                    ),
                    subtitle: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${row.requiredQuantity} ${row.unit} · Budget ${row.maximumBudget}',
                        ),
                        Text('Deadline ${requirementDate(row.deadline)}'),
                        RequirementStatusChip(row.status),
                        if (row.notes.isNotEmpty)
                          Text(
                            row.notes,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                          ),
                      ],
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => _open('/requirements/${row.id}'),
                  ),
                ),
              ),
              RequirementPager(
                page: _data!.page,
                totalPages: _data!.totalPages,
                onPage: _load,
              ),
            ],
          ],
        ),
      ),
    ),
  );
}

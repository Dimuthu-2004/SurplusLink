import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/config/app_config.dart';
import 'package:mobile/materials/quantity_format.dart' as quantities;
import 'package:mobile/matches/match_formatters.dart';
import 'package:mobile/matches/match_gateway.dart';
import 'package:mobile/matches/match_models.dart';
import 'package:mobile/matches/match_widgets.dart';
import 'package:mobile/widgets/dashboard_back_button.dart';
import 'package:url_launcher/url_launcher.dart';

Future<bool> launchExternalMap(Uri uri) =>
    launchUrl(uri, mode: LaunchMode.externalApplication);

class MatchDetailsScreen extends StatefulWidget {
  const MatchDetailsScreen({
    required this.gateway,
    required this.requirementId,
    required this.matchId,
    this.launchMap = launchExternalMap,
    super.key,
  });
  final MatchGateway gateway;
  final String requirementId, matchId;
  final Future<bool> Function(Uri uri) launchMap;
  @override
  State<MatchDetailsScreen> createState() => _MatchDetailsScreenState();
}

class _MatchDetailsScreenState extends State<MatchDetailsScreen> {
  RecommendedMatch? _match;
  MatchPage<MatchHistoryEntry>? _history;
  String? _error, _historyError;
  bool _loading = true,
      _historyLoading = false,
      _selecting = false,
      _routing = false;
  String? _selectionError;
  int _historyPage = 1;
  MatchAllocation? _selection;
  double? get _selectedQuantity => _selection?.quantity;
  final TextEditingController _quantityController = TextEditingController();

  @override
  void dispose() {
    _quantityController.dispose();
    super.dispose();
  }
  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(MatchDetailsScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.matchId != widget.matchId ||
        oldWidget.requirementId != widget.requirementId ||
        oldWidget.gateway != widget.gateway) {
      _load();
    }
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final match = await widget.gateway.get(
        widget.requirementId,
        widget.matchId,
      );
      if (!mounted) return;
      setState(() {
        _match = match;
        _loading = false;
        if (match.availableQuantity != null && match.quantity != null) {
          final desired = match.availableQuantity! < match.quantity!
              ? match.availableQuantity! : match.quantity!;
          _setSelectedQuantity(match.isPackaged
              ? (match.packageCountFor(match.packageQuantityFor(desired)) ?? 0).toDouble()
              : desired.clamp(0.0, match.availableQuantity!).toDouble());
          _quantityController.text = quantities.formatQuantity(
            _selectedQuantity!,
            match.unit ?? '',
          );
        }
      });
      await _loadHistory(1);
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _error = matchDetailsError(error);
          _loading = false;
        });
      }
    }
  }

  Future<void> _loadHistory(int page) async {
    setState(() {
      _historyLoading = true;
      _historyError = null;
      _historyPage = page;
    });
    try {
      final history = await widget.gateway.history(widget.matchId, page: page);
      if (mounted) setState(() => _history = history);
    } on Object catch (error) {
      if (mounted) setState(() => _historyError = matchError(error));
    } finally {
      if (mounted) setState(() => _historyLoading = false);
    }
  }

  Future<void> _retryRoute() async {
    setState(() => _routing = true);
    try {
      final match = await widget.gateway.retryRoute(widget.matchId);
      if (mounted) setState(() => _match = match);
      await _loadHistory(1);
    } on Object catch (error) {
      if (mounted) setState(() => _error = matchDetailsError(error));
    } finally {
      if (mounted) setState(() => _routing = false);
    }
  }

  String? _readableLocation(RecommendedMatch match) {
    for (final value in [match.fullAddress, match.sellerAddress, match.displayLocation]) {
      if (value?.trim().isNotEmpty == true) return value!.trim();
    }
    return null;
  }

  Uri? _mapUri(RecommendedMatch match) {
    final latitude = match.latitude;
    final longitude = match.longitude;
    final validCoordinates = latitude != null && longitude != null &&
        latitude.isFinite && longitude.isFinite && latitude.abs() <= 90 && longitude.abs() <= 180;
    final query = validCoordinates ? '$latitude,$longitude' : _readableLocation(match);
    return query == null ? null : Uri.https('www.google.com', '/maps/search/', {'api': '1', 'query': query});
  }

  Future<void> _openMap(RecommendedMatch match) async {
    final uri = _mapUri(match);
    if (uri == null) return;
    final opened = await widget.launchMap(uri);
    if (!opened && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not open Google Maps.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Match Details'),
      leading: DashboardBackButton(
        fallback: '/requirements/${widget.requirementId}/matches',
      ),
      actions: [
        IconButton(
          tooltip: 'Refresh match',
          onPressed: _loading || _historyLoading || _selecting ? null : _load,
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    bottomNavigationBar: !_loading &&
            _error == null &&
            _match?.requirementStatus == 'MATCH_FOUND' &&
            _match!.isSelectable
        ? SafeArea(
            minimum: const EdgeInsets.all(16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_selectionError != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Text(
                      _selectionError!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  margin: const EdgeInsets.only(bottom: 8),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.surfaceContainerHighest,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Wrap(
                    alignment: WrapAlignment.spaceBetween,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      Text(
                        _match!.isPackaged
                            ? 'Quantity to take (${_match!.packageType?.toLowerCase() ?? 'package'}s):'
                            : 'Quantity (${_match!.unit ?? ''}):',
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          IconButton(
                            icon: const Icon(
                              Icons.remove_circle_outline,
                              size: 20,
                            ),
                            onPressed: (_selectionControlValue(_match!) >
                                    _stepFor(_match!.unit))
                                ? () => setState(
                                      () => _setSelectedQuantity(
                                        _selectionControlValue(_match!) -
                                            _stepFor(_match!.unit),
                                      ),
                                    )
                                : null,
                          ),
                          SizedBox(
                            width: 80,
                            child: TextFormField(
                              key: const Key('details-quantity-input'),
                              controller: _quantityController,
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                decimal: true,
                              ),
                              textAlign: TextAlign.center,
                              decoration: InputDecoration(
                                isDense: true,
                                contentPadding: const EdgeInsets.symmetric(
                                  horizontal: 6,
                                  vertical: 6,
                                ),
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(6),
                                ),
                              ),
                              onChanged: (text) {
                                final parsed = double.tryParse(text.trim());
                                if (parsed != null) {
                                  setState(() => _setSelectedQuantity(parsed));
                                }
                              },
                            ),
                          ),
                          IconButton(
                            icon: const Icon(
                              Icons.add_circle_outline,
                              size: 20,
                            ),
                            onPressed: (_selectionControlValue(_match!) <
                                    _maximumSelectable(_match!))
                                ? () => setState(
                                      () => _setSelectedQuantity(
                                        (_selectionControlValue(_match!) +
                                                _stepFor(_match!.unit))
                                            .clamp(
                                              0.0,
                                              _maximumSelectable(_match!),
                                            )
                                            .toDouble(),
                                      ),
                                    )
                                : null,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    key: const Key('select-match'),
                    onPressed: _selecting ||
                            ((_selectedQuantity ?? 0) <= 0 ||
                                (_selectedQuantity ?? 0) >
                                    _maximumSelectable(_match!))
                        ? null
                        : _selectMatch,
                    icon: const Icon(Icons.check_circle_outline),
                    label: Text(
                      _selecting ? 'Selecting...' : 'Select this match',
                    ),
                  ),
                ),
              ],
            ),
          )
        : null,
    body: _loading
        ? const Center(child: CircularProgressIndicator())
        : ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (_error != null)
                MatchErrorBox(_error!, onRetry: _load)
              else if (_match case final match?) ...[
                _SummaryCard(match: match),
                if (match.isPartial && !match.isRejected)
                  Card(
                    key: const Key('partial-quantity-warning'),
                    color: const Color(0xFFFEF3C7),
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(
                            Icons.warning_amber_rounded,
                            color: Color(0xFFB45309),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Partial quantity available',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: Color(0xFF92400E),
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  match.partialWarning(),
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w600,
                                    color: Color(0xFF78350F),
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  'You can choose a quantity up to ${quantities.formatQuantity(match.availableQuantity ?? 0, match.unit ?? '')} ${match.unit ?? ''}.',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: Color(0xFF92400E),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                if (match.isRejected || match.status == 'ROUTE_FAILED')
                  _warning(match),
                if (match.requirementStatus == 'PENDING_APPROVAL')
                  const Text('Waiting for manager approval'),
                if (match.requirementStatus == 'MATCHING')
                  const Text('Matching and evaluation in progress.'),
                if (match.requirementStatus == 'APPROVED')
                  const Text('Requirement approved.'),
                _card('Material', [
                  if (match.categoryName != null)
                    _detail('Category', match.categoryName!),
                  _detail('Condition', match.condition ?? 'Not recorded'),
                  _detail(
                    'Available quantity',
                    _quantity(match.availableQuantity, match.unit),
                  ),
                  if (match.isPackaged) ...[
                    _detail('Physical stock', '${match.packageCountAvailable ?? 0} ${match.packageType?.toLowerCase() ?? 'packages'} (${_quantity(match.availableQuantity, match.unit)} total)'),
                    _detail('Package size', '${quantities.formatQuantity(match.packageSize ?? 1, match.unit ?? '')} ${match.unit ?? ''} per ${match.packageType?.toLowerCase() ?? 'package'}'),
                    _detail('Quantity to take', '${match.packageCountFor(_selectedQuantity ?? 0) ?? 0} ${match.packageType?.toLowerCase() ?? 'packages'} = ${_quantity(_selectedQuantity, match.unit)}'),
                    if ((_selectedQuantity ?? 0) > (match.quantity ?? 0))
                      _detail('Package overage', _quantity((_selectedQuantity ?? 0) - (match.quantity ?? 0), match.unit)),
                  ],
                  _detail(
                    'Required / selected quantity',
                    _quantity(match.quantity, match.unit),
                  ),
                  _detail(match.isPackaged ? 'Price per ${match.packageType?.toLowerCase() ?? 'package'}' : 'Unit price', formatCurrency(match.unitPrice)),
                  _detail(
                    'Material value',
                    formatCurrency(match.materialCostFor(_selectedQuantity ?? 0)),
                  ),
                ]),
                if (match.listingContext != null)
                  _card('Listing details', [
                    if (match.listingContext!.templateName != null) _detail('Item', match.listingContext!.templateName!),
                    if (match.listingContext!.description?.trim().isNotEmpty == true) _detail('Description', match.listingContext!.description!),
                    if (match.listingContext!.photos.isNotEmpty) SizedBox(
                      height: 112,
                      child: ListView(scrollDirection: Axis.horizontal, children: match.listingContext!.photos
                        .map((url) => Padding(padding: const EdgeInsets.only(right: 8), child: InkWell(
                          onTap: () => _showPhotoViewer(match.listingContext!.photos, url),
                          child: ClipRRect(borderRadius: BorderRadius.circular(8), child: Image.network(_mediaUrl(url), width: 140, fit: BoxFit.cover,
                            errorBuilder: (_, _, _) => const SizedBox(width: 140, child: Center(child: Icon(Icons.broken_image_outlined))),
                          )),
                        )))
                        .toList()),
                    ),
                    if (match.listingContext!.specificationsJson?.trim().isNotEmpty == true)
                      _specifications(match.listingContext!.specificationsJson!),
                  ]),
                if (match.preferenceCompatibility?.hasMismatch == true)
                  _preferenceWarning(match.preferenceCompatibility!),
                _locationCard(match),
                _card('Delivery / Logistics', [
                  if (getRoutingState(match) == RoutingUiState.notEvaluated)
                    const Text('Delivery route has not been evaluated yet.'),
                  _detail(
                    'Distance',
                    match.distance == null
                        ? 'Not available'
                        : '${match.distance!.toStringAsFixed(1)} km',
                  ),
                  _detail(
                    'Estimated travel time',
                    _duration(match.durationMinutes),
                  ),
                  _detail(
                    'Transport estimate',
                    formatCurrency(match.estimatedTransportCost),
                  ),
                  _detail(
                    'Total estimated cost',
                    formatCurrency(
                      match.materialCostFor(_selectedQuantity ?? 0) != null &&
                              match.estimatedTransportCost != null
                          ? match.materialCostFor(_selectedQuantity ?? 0)! +
                                match.estimatedTransportCost!
                          : null,
                    ),
                  ),
                ]),
                _card('Match evaluation', [
                  _detail(
                    'Match score',
                    '${(match.score * 100).toStringAsFixed(1)}%',
                  ),
                  _detail(
                    'Fit / recommendation',
                    match.isSelectable
                        ? (match.aiRecommended
                              ? 'AI highlights this valid candidate for your review.'
                              : 'This candidate passed the match evaluation.')
                        : 'This candidate is not currently selectable.',
                  ),
                  _detail(
                    'Warnings',
                    match.isRejected || match.status == 'ROUTE_FAILED'
                        ? 'Review the warning above before continuing.'
                        : 'No additional warning information provided.',
                  ),
                  if (match.maximumBudget != null)
                    _detail(
                      'Maximum budget',
                      formatCurrency(match.maximumBudget),
                    ),
                  if (match.availableUntil != null)
                    Text(
                      'Available until: ${formatMatchDate(match.availableUntil)}',
                    ),
                  if (match.requiredBy != null)
                    Text('Required by: ${formatMatchDate(match.requiredBy)}'),
                ]),
                ExpansionTile(
                  key: const Key('technical-details-tile'),
                  title: const Text('Technical details'),
                  expandedCrossAxisAlignment: CrossAxisAlignment.start,
                  childrenPadding: const EdgeInsets.all(16),
                  children: [
                    SelectableText('Match ID: ${match.id}'),
                    SelectableText('Material listing: ${match.listingId}'),
                    SelectableText('Requirement ID: ${match.requirementId}'),
                    if (match.sellerId != null)
                      SelectableText('Seller ID: ${match.sellerId}'),
                    SelectableText('Status: ${match.status}'),
                    if (match.rejectionReason != null)
                      Text('Reason: ${routingText(context, match.rejectionReason!)}'),
                    Text('Created: ${formatMatchDateTime(match.createdAt)}'),
                  ],
                ),
                const SizedBox(height: 16),
                const Text(
                  'Seller contact is available after manager approval.',
                ),
                const SizedBox(height: 16),
                Text(
                  'Match history',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                _buildHistory(),
              ],
            ],
          ),
  );

  Future<void> _selectMatch() async {
    final match = _match;
    if (_selecting ||
        match == null ||
        !match.isSelectable ||
        match.requirementStatus != 'MATCH_FOUND') {
      return;
    }
    setState(() {
      _selecting = true;
      _selectionError = null;
    });
    try {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Select this match?'),
          content: Text(
            match.isPartial
                ? 'You selected ${quantities.formatQuantity(_selectedQuantity ?? 0, match.unit ?? '')} ${match.unit ?? ''}. Your selection will be checked again and sent for manager approval. No material is reserved by this choice.'
                : 'Your selected match will be checked again and sent for manager approval. No material is reserved by this choice.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Confirm selection'),
            ),
          ],
        ),
      );
      if (confirmed != true || !mounted) return;
      final selection = _selection;
      if (selection == null || selection.matchId != match.id) return;
      await widget.gateway.select(
        widget.requirementId,
        match.id,
        quantity: selection.quantity,
        packageCount: selection.packageCount,
      );
      await _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Allocation saved and sent for manager approval.',
            ),
          ),
        );
        context.go('/requirements/${widget.requirementId}');
      }
    } on Object catch (error) {
      if (mounted) setState(() => _selectionError = matchError(error));
    } finally {
      if (mounted) setState(() => _selecting = false);
    }
  }

  double _stepFor(String? unit) => _match?.isPackaged == true
      ? 1
      : _match?.selectionStep ??
          (quantities.isDiscreteUnit(unit ?? '') ? 1 : 0.1);

  double _maximumSelectable(RecommendedMatch match) => match.isPackaged
      ? (match.packageCountAvailable ?? 0).toDouble()
      : (match.availableQuantity ?? 0).clamp(0.0, match.quantity ?? double.infinity).toDouble();
  double _selectionControlValue(RecommendedMatch match) => match.isPackaged
      ? (_selection?.packageCount ?? 0).toDouble()
      : _selectedQuantity ?? 0;
  void _setSelectedQuantity(double value) {
    final match = _match;
    if (match == null) return;
    final max = _maximumSelectable(match);
    final base = value.clamp(0.0, max).toDouble();
    final double normalized = match.isPackaged
        ? base.round() * (match.packageSize ?? 1)
        : base;
    _selection = MatchAllocation(matchId: match.id, quantity: normalized,
        packageCount: match.packageCountFor(normalized));
    _quantityController.text = quantities.formatQuantity(_selectionControlValue(match), match.isPackaged ? '' : (match.unit ?? ''));
  }

  String _quantity(double? value, String? unit) => value == null
      ? 'Not available'
      : '${quantities.formatQuantity(value, unit ?? '')} ${formatUnit(unit)}'
            .trim();

  String _duration(double? minutes) {
    if (minutes == null) return 'Not available';
    final rounded = minutes.round();
    if (rounded == 0 && minutes > 0) return '<1 min';
    if (rounded < 60) return '$rounded min';
    return '${rounded ~/ 60} h${rounded % 60 == 0 ? '' : ' ${rounded % 60} min'}';
  }

  Widget _detail(String label, String value) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 7),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: Theme.of(context).textTheme.labelMedium
              ?.copyWith(color: Theme.of(context).colorScheme.onSurfaceVariant),
        ),
        const SizedBox(height: 3),
        Text(value, style: Theme.of(context).textTheme.bodyLarge),
      ],
    ),
  );

  Widget _locationCard(RecommendedMatch match) {
    final location = _readableLocation(match);
    final uri = _mapUri(match);
    return _card('Location', [
      Text(
        location ?? 'Location details unavailable',
        key: const Key('match-full-address'),
        style: Theme.of(context).textTheme.bodyLarge?.copyWith(height: 1.45),
      ),
      if (uri != null) ...[
        const SizedBox(height: 8),
        TextButton.icon(
          key: const Key('view-location-on-google-maps'),
          onPressed: () => _openMap(match),
          icon: const Icon(Icons.location_on_outlined),
          label: const Text('View location on Google Maps'),
          style: TextButton.styleFrom(foregroundColor: const Color(0xFFD96712)),
        ),
      ],
    ]);
  }

  Widget _warning(RecommendedMatch match) => Card(
    key: const Key('match-warning'),
    color: Theme.of(context).colorScheme.errorContainer,
    margin: const EdgeInsets.only(bottom: 12),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: DefaultTextStyle.merge(
        style: TextStyle(color: Theme.of(context).colorScheme.onErrorContainer),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              Icons.warning_amber_rounded,
              color: Theme.of(context).colorScheme.error,
            ),
            Text(
              match.status == 'ROUTE_FAILED'
                  ? 'Delivery route failed'
                  : 'Why this match was rejected',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              match.rejectionReason != null
                  ? routingText(context, match.rejectionReason!)
                  : match.status == 'ROUTE_FAILED'
                  ? 'Delivery route could not be calculated. Please try again later.'
                  : readableRejectionReason(null),
            ),
            const SizedBox(height: 8),
            Text(routingText(context, 'cannotSelect')),
            if (match.status == 'ROUTE_FAILED') ...[
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: _routing ? null : _retryRoute,
                icon: _routing
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.refresh),
                label: Text(routingText(context, 'retryRoute')),
              ),
            ],
          ],
        ),
      ),
    ),
  );

  String _mediaUrl(String value) {
    final uri = Uri.tryParse(value);
    if (uri != null && uri.hasScheme && (uri.scheme == 'http' || uri.scheme == 'https')) {
      return value;
    }
    return AppConfig.apiBaseUri.resolve(value).toString();
  }

  Widget _specifications(String source) {
    try {
      final data = jsonDecode(source);
      if (data is Map) {
        return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        const Text('Specifications', style: TextStyle(fontWeight: FontWeight.w600)),
        ...data.entries.where((e) => e.value != null && e.value.toString().trim().isNotEmpty).map((e) => _detail(_label(e.key.toString()), e.value.toString())),
        ]);
      }
    } catch (_) {}
    return const SizedBox.shrink();
  }

  String _label(String key) => key.replaceAllMapped(RegExp(r'([a-z])([A-Z])'), (m) => '${m[1]} ${m[2]}').replaceFirst(key.isEmpty ? '' : key[0], key.isEmpty ? '' : key[0].toUpperCase());

  Widget _preferenceWarning(PreferenceCompatibility preference) => Container(
    margin: const EdgeInsets.only(top: 12), padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(color: const Color(0xfffff4e5), borderRadius: BorderRadius.circular(10), border: Border.all(color: const Color(0xfff59e0b))),
    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      const Text('Preference mismatch', style: TextStyle(fontWeight: FontWeight.bold)),
      const SizedBox(height: 4),
      ...preference.mismatches.map((m) => Text('${m.label}: requested ${m.requestedValue}; seller ${m.sellerValue?.isNotEmpty == true ? 'has ${m.sellerValue}' : 'did not provide this information'}')),
    ]),
  );

  void _showPhotoViewer(List<String> photos, String selected) {
    final initial = photos.indexOf(selected);
    showDialog<void>(context: context, builder: (context) => Dialog.fullscreen(child: Scaffold(
      appBar: AppBar(title: const Text('Listing photos')),
      body: PageView.builder(controller: PageController(initialPage: initial < 0 ? 0 : initial), itemCount: photos.length,
        itemBuilder: (_, index) => InteractiveViewer(child: Center(child: Image.network(_mediaUrl(photos[index]), fit: BoxFit.contain,
          errorBuilder: (_, _, _) => const Icon(Icons.broken_image_outlined, size: 48),
        )))),
    )));
  }

  Widget _card(String title, List<Widget> children) => Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          ...children,
        ],
      ),
    ),
  );

  Widget _buildHistory() {
    if (_historyLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_historyError != null) {
      return MatchErrorBox(
        _historyError!,
        onRetry: () => _loadHistory(_historyPage),
      );
    }
    final history = _history;
    if (history == null) return const SizedBox.shrink();
    return Column(
      children: [
        if (history.items.isEmpty) const Text('No match history yet.'),
        for (final entry in history.items)
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(
              readableHistoryAction(entry.action, outcome: entry.outcome),
            ),
            subtitle: Text(
              [
                formatMatchDateTime(entry.createdAt),
                ?historyActionExplanation(entry.action, outcome: entry.outcome),
              ].join('\n'),
            ),
          ),
        MatchPagination(
          page: history.page,
          totalPages: history.totalPages,
          busy: _historyLoading,
          onPage: _loadHistory,
        ),
      ],
    );
  }
}

class _SummaryCard extends StatefulWidget {
  const _SummaryCard({required this.match});
  final RecommendedMatch match;
  @override
  State<_SummaryCard> createState() => _SummaryCardState();
}

class _SummaryCardState extends State<_SummaryCard>
    with SingleTickerProviderStateMixin {
  late final AnimationController _glow = AnimationController(
    vsync: this,
    duration: const Duration(seconds: 3),
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _updateAnimation();
  }

  @override
  void didUpdateWidget(_SummaryCard oldWidget) {
    super.didUpdateWidget(oldWidget);
    _updateAnimation();
  }

  void _updateAnimation() {
    if (widget.match.aiRecommended &&
        !MediaQuery.disableAnimationsOf(context)) {
      if (!_glow.isAnimating) _glow.repeat(reverse: true);
    } else {
      _glow.stop();
      _glow.value = 0;
    }
  }

  @override
  void dispose() {
    _glow.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final match = widget.match;
    final scheme = Theme.of(context).colorScheme;
    final failed = match.isRejected || match.status == 'ROUTE_FAILED';
    final status = match.status == 'ROUTE_FAILED'
        ? 'Route failed'
        : match.isRejected
        ? 'Rejected'
        : match.isSelectable
        ? 'Valid'
        : readableMatchStatus(match.status);
    final statusColor = failed
        ? scheme.error
        : match.isSelectable
        ? const Color(0xFF15803D)
        : scheme.onSurfaceVariant;
    return AnimatedBuilder(
      animation: _glow,
      builder: (context, child) => Container(
        key: const Key('match-summary'),
        margin: const EdgeInsets.only(bottom: 16),
        decoration: BoxDecoration(
          color: scheme.surface,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: match.aiRecommended
                ? scheme.secondary.withValues(
                    alpha: .4 + .25 * Curves.easeInOut.transform(_glow.value),
                  )
                : scheme.outlineVariant,
          ),
          boxShadow: match.aiRecommended
              ? [
                  BoxShadow(
                    color: scheme.secondary.withValues(
                      alpha:
                          .06 + .06 * Curves.easeInOut.transform(_glow.value),
                    ),
                    blurRadius: 8 + 6 * Curves.easeInOut.transform(_glow.value),
                    spreadRadius: 1,
                  ),
                ]
              : [],
        ),
        child: child,
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                Chip(
                  avatar: Icon(
                    failed
                        ? Icons.error_outline
                        : match.isSelectable
                        ? Icons.check_circle_outline
                        : Icons.hourglass_empty,
                    color: statusColor,
                    size: 18,
                  ),
                  label: Text(
                    status,
                    style: TextStyle(
                      color: statusColor,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  backgroundColor: statusColor.withValues(alpha: .08),
                  side: BorderSide.none,
                ),
                if (match.aiRecommended)
                  Chip(
                    key: const Key('ai-recommended-badge'),
                    avatar: Icon(
                      Icons.auto_awesome,
                      size: 16,
                      color: scheme.secondary,
                    ),
                    label: const Text('AI Recommended'),
                    backgroundColor: scheme.secondary.withValues(alpha: .08),
                    side: BorderSide.none,
                  ),
              ],
            ),
            const SizedBox(height: 12),
            Text(
              match.materialTitle ?? 'Material recommendation',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            Text(
              match.sellerDisplayName ??
                  match.sellerBusinessName ??
                  match.sellerName ??
                  (match.sellerId == null ? 'Seller not recorded' : 'Verified seller'),
              style: Theme.of(context).textTheme.titleMedium,
            ),
            if (match.sellerBusinessName != null && match.sellerName != null)
              Text(match.sellerName!),
            const SizedBox(height: 16),
            Text(
              'Score ${(match.score * 100).toStringAsFixed(1)}%',
              style: Theme.of(context).textTheme.titleLarge,
            ),
          ],
        ),
      ),
    );
  }
}

import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/marketplace/marketplace_offer_view.dart';
import 'package:flutter/material.dart';
import 'package:mobile/core/api_exception.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/offers/offer_models.dart';
import 'package:mobile/widgets/role_navigation.dart';

class MyOffersScreen extends StatefulWidget {
  const MyOffersScreen({
    required this.gateway,
    required this.user,
    this.mode,
    super.key,
  });
  final OfferGateway gateway;
  final AppUser user;
  final MarketplaceMode? mode;
  @override
  State<MyOffersScreen> createState() => _MyOffersScreenState();
}

class _MyOffersScreenState extends State<MyOffersScreen> {
  late final OfferGateway view = MarketplaceOfferView.forUser(
    widget.gateway,
    widget.user,
    widget.mode ?? availableMode(widget.user),
  );
  OfferPage? offers;
  TransactionPage? transactions;
  String? error;
  String? status;
  String? transactionStatus;
  int offerPage = 1, transactionPage = 1;
  String sort = 'createdAt';
  bool loading = true;

  bool get allowed =>
      widget.user.hasRole(AppRole.seller) || widget.user.hasRole(AppRole.buyer);
  @override
  void initState() {
    super.initState();
    if (allowed) load();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final query = OfferQuery(status: status, sortBy: sort, page: offerPage);
      final results = await Future.wait([
        view.offers(query),
        view.transactions(
          OfferQuery(
            status: transactionStatus,
            sortBy: sort,
            page: transactionPage,
          ),
        ),
      ]);
      if (mounted) {
        setState(() {
          offers = results[0] as OfferPage;
          transactions = results[1] as TransactionPage;
        });
      }
    } on Object catch (failure) {
      if (mounted) {
        setState(() {
          offers = null;
          transactions = null;
          error = offerError(
            failure,
            'Unable to load offers. Check your connection and retry.',
          );
        });
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!allowed) {
      return const Scaffold(
        body: Center(
          child: Text('Access denied. Marketplace account required.'),
        ),
      );
    }
    final offerRows = offers?.items ?? const <Offer>[];
    final transactionRows = transactions?.items ?? const <Transaction>[];
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Offers'),
        actions: [
          IconButton(
            onPressed: loading ? null : load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          DropdownButtonFormField<String>(
            initialValue: status ?? '',
            decoration: const InputDecoration(labelText: 'Filter by status'),
            items: [
              const DropdownMenuItem(value: '', child: Text('All statuses')),
              ...offerStatuses.map(
                (value) => DropdownMenuItem(
                  value: value,
                  child: Text(offerStatusLabel(value)),
                ),
              ),
            ],
            onChanged: (value) {
              setState(() {
                status = value == '' ? null : value;
                offerPage = 1;
              });
              load();
            },
          ),
          DropdownButtonFormField<String>(
            initialValue: sort,
            decoration: const InputDecoration(labelText: 'Sort by'),
            items: const [
              DropdownMenuItem(value: 'createdAt', child: Text('Created date')),
              DropdownMenuItem(value: 'value', child: Text('Value')),
              DropdownMenuItem(value: 'status', child: Text('Status')),
            ],
            onChanged: (value) {
              setState(() {
                sort = value!;
                offerPage = 1;
                transactionPage = 1;
              });
              load();
            },
          ),
          if (error != null) TextButton(onPressed: load, child: Text(error!)),
          if (loading) const Center(child: CircularProgressIndicator()),
          if (!loading && error == null && offerRows.isEmpty)
            Padding(
              padding: const EdgeInsets.all(20),
              child: Text(
                status == null
                    ? 'No offers yet.'
                    : 'No offers match these filters.',
              ),
            ),
          ...offerRows.map(
            (offer) => Card(
              child: ListTile(
                title: Text('${offerStatusLabel(offer.status)} offer'),
                subtitle: Text(
                  '${offer.buyerId == widget.user.id ? 'Buyer' : 'Seller'} participation\nValue LKR ${offer.totalValue.toStringAsFixed(2)}',
                ),
                isThreeLine: true,
                trailing: Text('${offer.quantity}'),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => OfferDetailsScreen(
                      offer: offer,
                      user: widget.user,
                      gateway: widget.gateway,
                    ),
                  ),
                ),
              ),
            ),
          ),
          if (offers != null)
            _pages(offerPage, offers!.totalPages, (page) {
              offerPage = page;
              load();
            }),
          DropdownButtonFormField<String>(
            initialValue: transactionStatus ?? '',
            decoration: const InputDecoration(labelText: 'Transaction status'),
            items: [
              const DropdownMenuItem(
                value: '',
                child: Text('All transaction statuses'),
              ),
              ...transactionStatuses.map(
                (value) => DropdownMenuItem(
                  value: value,
                  child: Text(offerStatusLabel(value)),
                ),
              ),
            ],
            onChanged: (value) {
              setState(() {
                transactionStatus = value == '' ? null : value;
                transactionPage = 1;
              });
              load();
            },
          ),
          if (!loading && error == null && transactionRows.isEmpty)
            const Text('No transactions match these filters.'),
          if (transactionRows.isNotEmpty)
            const Padding(
              padding: EdgeInsets.only(top: 20),
              child: Text(
                'Transaction status',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
          ...transactionRows.map(
            (transaction) => Card(
              child: ListTile(
                title: Text(offerStatusLabel(transaction.status)),
                subtitle: Text(
                  'Reserved ${transaction.reservedQuantity} · Updated ${transaction.updatedAt.toLocal()}',
                ),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => TransactionDetailsScreen(
                      user: widget.user,
                      gateway: widget.gateway,
                      transactionId: transaction.id,
                    ),
                  ),
                ),
              ),
            ),
          ),
          if (transactions != null)
            _pages(transactionPage, transactions!.totalPages, (page) {
              transactionPage = page;
              load();
            }),
        ],
      ),
      bottomNavigationBar: RoleNavigation(
        user: widget.user,
        mode: widget.mode,
        current: '/offers',
      ),
    );
  }

  Widget _pages(int page, int total, void Function(int) change) => Row(
    children: [
      TextButton(
        onPressed: loading || page <= 1 ? null : () => change(page - 1),
        child: const Text('Previous'),
      ),
      Expanded(
        child: Text(
          'Page $page of ${total == 0 ? 1 : total}',
          textAlign: TextAlign.center,
        ),
      ),
      TextButton(
        onPressed: loading || page >= total ? null : () => change(page + 1),
        child: const Text('Next'),
      ),
    ],
  );
}

class OfferDetailsScreen extends StatelessWidget {
  const OfferDetailsScreen({
    required this.offer,
    required this.gateway,
    required this.user,
    super.key,
  });
  final OfferGateway gateway;
  final Offer offer;
  final AppUser user;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Offer Details')),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(
          offerStatusLabel(offer.status),
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        Text(
          'Your participation: ${offer.buyerId == user.id ? 'Buyer' : 'Seller'}',
        ),
        Text('Quantity: ${offer.quantity}'),
        Text('Material value: LKR ${offer.totalValue.toStringAsFixed(2)}'),
        Text('Created: ${offer.createdAt.toLocal()}'),
        OfferTransactionDetails(
          gateway: gateway,
          offerId: offer.id,
          user: user,
        ),
      ],
    ),
  );
}

class TransactionDetailsScreen extends StatelessWidget {
  const TransactionDetailsScreen({
    required this.gateway,
    required this.transactionId,
    required this.user,
    super.key,
  });
  final OfferGateway gateway;
  final String transactionId;
  final AppUser user;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Transaction Details')),
    body: SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: OfferTransactionDetails(
        gateway: gateway,
        transactionId: transactionId,
        user: user,
      ),
    ),
  );
}

class OfferTransactionDetails extends StatefulWidget {
  const OfferTransactionDetails({
    required this.gateway,
    required this.user,
    this.offerId,
    this.transactionId,
    super.key,
  });
  final OfferGateway gateway;
  final AppUser user;
  final String? offerId, transactionId;
  @override
  State<OfferTransactionDetails> createState() =>
      _OfferTransactionDetailsState();
}

class _OfferTransactionDetailsState extends State<OfferTransactionDetails> {
  Transaction? transaction;
  bool busy = true;
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      busy = true;
      error = null;
      transaction = null;
    });
    try {
      var id = widget.transactionId;
      if (id == null) {
        final page = await widget.gateway.transactions(
          OfferQuery(offerId: widget.offerId),
        );
        if (page.items.isNotEmpty) id = page.items.first.id;
      }
      final result = id == null ? null : await widget.gateway.transaction(id);
      if (mounted) setState(() => transaction = result);
    } on Object catch (failure) {
      if (mounted) {
        setState(
          () => error = offerError(failure, 'Unable to load transaction.'),
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> act(bool handover) async {
    final id = transaction!.id;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      if (handover) {
        await widget.gateway.handover(id);
      } else {
        await widget.gateway.confirmReceipt(id);
      }
      await load();
    } on Object catch (failure) {
      if (mounted) {
        setState(() {
          transaction = null;
          error = offerError(
            failure,
            'Unable to update transaction. Refresh and retry.',
          );
        });
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final row = transaction;
    final contact = row?.buyerId == widget.user.id
        ? row?.sellerContact
        : row?.sellerId == widget.user.id
        ? row?.buyerContact
        : null;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextButton(
          onPressed: busy ? null : load,
          child: const Text('Refresh transaction'),
        ),
        if (busy) const CircularProgressIndicator(),
        if (error != null) Text(error!),
        if (!busy && error == null && row == null)
          const Text(
            'No transaction yet. Contact details are hidden until approval.',
          ),
        if (row != null) ...[
          _TransactionTimeline(status: row.status),
          _DetailCard(
            title: 'Transaction summary',
            children: [
              _DetailLine(label: 'Status', value: offerStatusLabel(row.status)),
              _DetailLine(label: 'Reserved quantity', value: row.reservedQuantity.toStringAsFixed(2)),
              _DetailLine(label: 'Total value', value: 'LKR ${row.totalValue.toStringAsFixed(2)}'),
            ],
          ),
          if (!row.contactsVisible)
            const Text('Contact details are hidden until approval.'),
          if (row.contactsVisible && contact != null) ...[
            const Text('Counterparty contact'),
            if (contact.fullName != null) Text(contact.fullName!),
            Text(contact.email),
            if (contact.phoneNumber case final phone? when phone.trim().isNotEmpty)
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(phone),
                  IconButton(
                    tooltip: 'Call seller',
                    icon: const Icon(Icons.phone),
                    onPressed: () => launchUrl(Uri(scheme: 'tel', path: phone)),
                  ),
                ],
              ),
          ],
          if (row.canHandover(widget.user))
            FilledButton(
              onPressed: busy ? null : () => act(true),
              child: const Text('Material Handed Over'),
            ),
          if (row.canConfirmReceipt(widget.user)) ...[
            const Text('Have you received the materials?'),
            FilledButton(
              onPressed: busy ? null : () => act(false),
              child: const Text('Yes, Received'),
            ),
          ],
          if (row.status == 'APPROVED' && row.buyerId == widget.user.id)
            const Text('Waiting for the seller to hand over the materials.'),
          if (row.status == 'HANDED_OVER' && row.sellerId == widget.user.id)
            const Text('Waiting for the buyer to confirm receipt.'),
          TextButton(
            onPressed: () => Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => TransactionHistoryScreen(
                  gateway: widget.gateway,
                  transactionId: row.id,
                ),
              ),
            ),
            child: const Text('Transaction history'),
          ),
        ],
      ],
    );
  }
}

class _TransactionTimeline extends StatelessWidget {
  const _TransactionTimeline({required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    const stages = [('APPROVED', 'Approved', Icons.verified_outlined), ('HANDED_OVER', 'Handed Over', Icons.local_shipping_outlined), ('COMPLETED', 'Completed', Icons.check_circle_outline)];
    final active = stages.indexWhere((stage) => stage.$1 == status);
    return Card(
      color: const Color(0xFFF8FAFC),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const Text('Progress', style: TextStyle(fontWeight: FontWeight.w800)),
          const SizedBox(height: 16),
          for (var index = 0; index < stages.length; index++) ...[
            Row(children: [
              Icon(stages[index].$3, color: index <= active ? const Color(0xFFF47B20) : const Color(0xFFCBD5E1)),
              const SizedBox(width: 12),
              Text(stages[index].$2, style: TextStyle(fontWeight: index == active ? FontWeight.w800 : FontWeight.w500)),
              if (index == active) ...[const SizedBox(width: 8), const Chip(label: Text('Current'))],
            ]),
            if (index < stages.length - 1) const Padding(padding: EdgeInsets.only(left: 11), child: SizedBox(height: 20, child: VerticalDivider(width: 1))),
          ],
        ]),
      ),
    );
  }
}

class _DetailCard extends StatelessWidget {
  const _DetailCard({required this.title, required this.children});
  final String title;
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(title, style: const TextStyle(fontWeight: FontWeight.w800)), const SizedBox(height: 12), ...children])));
}

class _DetailLine extends StatelessWidget {
  const _DetailLine({required this.label, required this.value});
  final String label, value;
  @override
  Widget build(BuildContext context) => Padding(padding: const EdgeInsets.only(bottom: 8), child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [Text(label), Text(value, style: const TextStyle(fontWeight: FontWeight.w700))]));
}

class TransactionHistoryScreen extends StatefulWidget {
  const TransactionHistoryScreen({
    required this.gateway,
    required this.transactionId,
    super.key,
  });
  final OfferGateway gateway;
  final String transactionId;
  @override
  State<TransactionHistoryScreen> createState() =>
      _TransactionHistoryScreenState();
}

class _TransactionHistoryScreenState extends State<TransactionHistoryScreen> {
  TransactionHistoryPage? data;
  String? error;
  int page = 1;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final result = await widget.gateway.history(
        widget.transactionId,
        page: page,
      );
      if (mounted) setState(() => data = result);
    } on Object catch (failure) {
      if (mounted) {
        setState(
          () => error = offerError(
            failure,
            'Unable to load transaction history.',
          ),
        );
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Transaction History'),
      actions: [
        IconButton(
          onPressed: loading ? null : load,
          tooltip: 'Refresh history',
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (loading) const Center(child: CircularProgressIndicator()),
        if (error != null) ...[
          Text(error!),
          TextButton(
            onPressed: loading ? null : load,
            child: const Text('Retry'),
          ),
        ],
        if (!loading && error == null && data?.items.isEmpty == true)
          const Text('No history events yet.'),
        if (data != null && !loading && error == null) ...[
          ...data!.items.map(
            (entry) => ListTile(
              title: Text(entry.action.replaceAll('_', ' ')),
              subtitle: Text(entry.createdAt.toLocal().toString()),
            ),
          ),
          Row(
            children: [
              TextButton(
                onPressed: page <= 1
                    ? null
                    : () {
                        page--;
                        load();
                      },
                child: const Text('Previous'),
              ),
              Expanded(
                child: Text(
                  'Page $page of ${data!.totalPages == 0 ? 1 : data!.totalPages}',
                  textAlign: TextAlign.center,
                ),
              ),
              TextButton(
                onPressed: page >= data!.totalPages
                    ? null
                    : () {
                        page++;
                        load();
                      },
                child: const Text('Next'),
              ),
            ],
          ),
        ],
      ],
    ),
  );
}

String offerError(Object error, String fallback) => switch (error) {
  ApiException(statusCode: 401) =>
    'Your session has expired. Please sign in again.',
  ApiException(statusCode: 403) =>
    'Access denied. You cannot view these records.',
  ApiException(statusCode: final int code) =>
    'Unable to load records (API $code). Please retry.',
  _ => fallback,
};

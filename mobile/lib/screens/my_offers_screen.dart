import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/offers/offer_models.dart';

class MyOffersScreen extends StatefulWidget {
  const MyOffersScreen({required this.gateway, required this.user, super.key});
  final OfferGateway gateway;
  final AppUser user;
  @override State<MyOffersScreen> createState() => _MyOffersScreenState();
}

class _MyOffersScreenState extends State<MyOffersScreen> {
  OfferPage? offers;
  TransactionPage? transactions;
  String? error;
  String? status;
  String? transactionStatus;
  int offerPage = 1, transactionPage = 1;
  String sort = 'createdAt';
  bool loading = true;

  @override void initState() { super.initState(); load(); }
  Future<void> load() async {
    setState(() { loading = true; error = null; });
    try {
      final query = OfferQuery(status: status, sortBy: sort, page: offerPage);
      final results = await Future.wait([widget.gateway.offers(query), widget.gateway.transactions(
        OfferQuery(status: transactionStatus, sortBy: sort, page: transactionPage))]);
      if (mounted) setState(() { offers = results[0] as OfferPage; transactions = results[1] as TransactionPage; });
    } on Object { if (mounted) setState(() => error = 'Unable to load offers. Check your connection and retry.'); }
    finally { if (mounted) setState(() => loading = false); }
  }

  @override Widget build(BuildContext context) {
    final offerRows = offers?.items ?? const <Offer>[];
    final transactionRows = transactions?.items ?? const <Transaction>[];
    return Scaffold(
      appBar: AppBar(title: const Text('My Offers'), actions: [IconButton(onPressed: loading ? null : load, icon: const Icon(Icons.refresh))]),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        DropdownButtonFormField<String>(
          initialValue: status ?? '', decoration: const InputDecoration(labelText: 'Filter by status'),
          items: [const DropdownMenuItem(value: '', child: Text('All statuses')), ...offerStatuses.map((value) => DropdownMenuItem(value: value, child: Text(offerStatusLabel(value))))],
          onChanged: (value) { setState(() { status = value == '' ? null : value; offerPage = 1; }); load(); },
        ),
        DropdownButtonFormField<String>(
          initialValue: sort, decoration: const InputDecoration(labelText: 'Sort by'),
          items: const [DropdownMenuItem(value: 'createdAt', child: Text('Created date')), DropdownMenuItem(value: 'value', child: Text('Value')), DropdownMenuItem(value: 'status', child: Text('Status'))],
          onChanged: (value) { setState(() { sort = value!; offerPage = 1; transactionPage = 1; }); load(); },
        ),
        if (error != null) TextButton(onPressed: load, child: Text(error!)),
        if (loading) const Center(child: CircularProgressIndicator()),
        if (!loading && offerRows.isEmpty) const Padding(padding: EdgeInsets.all(20), child: Text('No offers match these filters.')),
        ...offerRows.map((offer) => Card(child: ListTile(
          title: Text('Offer ${offer.id.substring(0, 8)}'),
          subtitle: Text('${offer.buyerId == widget.user.id ? 'Buyer' : 'Seller'} participation · ${offerStatusLabel(offer.status)}\nValue ${offer.totalValue}'),
          isThreeLine: true, trailing: Text('${offer.quantity}'),
          onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => OfferDetailsScreen(offer: offer, user: widget.user))),
        ))),
        if (offers != null) _pages(offerPage, offers!.totalPages, (page) { offerPage = page; load(); }),
        DropdownButtonFormField<String>(
          initialValue: transactionStatus ?? '', decoration: const InputDecoration(labelText: 'Transaction status'),
          items: [const DropdownMenuItem(value: '', child: Text('All transaction statuses')), ...transactionStatuses.map((value) => DropdownMenuItem(value: value, child: Text(offerStatusLabel(value))))],
          onChanged: (value) { setState(() { transactionStatus = value == '' ? null : value; transactionPage = 1; }); load(); },
        ),
        if (!loading && transactionRows.isEmpty) const Text('No transactions match these filters.'),
        if (transactionRows.isNotEmpty) const Padding(padding: EdgeInsets.only(top: 20), child: Text('Transaction status', style: TextStyle(fontWeight: FontWeight.bold))),
        ...transactionRows.map((transaction) => Card(child: ListTile(
          title: Text(offerStatusLabel(transaction.status)),
          subtitle: Text('Reserved ${transaction.reservedQuantity} · Updated ${transaction.updatedAt.toLocal()}'),
          onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => TransactionHistoryScreen(gateway: widget.gateway, transactionId: transaction.id)),),
        ))),
        if (transactions != null) _pages(transactionPage, transactions!.totalPages, (page) { transactionPage = page; load(); }),
      ]),
    );
  }
  Widget _pages(int page, int total, void Function(int) change) => Row(children: [
    TextButton(onPressed: loading || page <= 1 ? null : () => change(page - 1), child: const Text('Previous')),
    Expanded(child: Text('Page $page of ${total == 0 ? 1 : total}', textAlign: TextAlign.center)),
    TextButton(onPressed: loading || page >= total ? null : () => change(page + 1), child: const Text('Next')),
  ]);
}

class OfferDetailsScreen extends StatelessWidget {
  const OfferDetailsScreen({required this.offer, required this.user, super.key});
  final Offer offer;
  final AppUser user;
  @override Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Offer Details')),
    body: ListView(padding: const EdgeInsets.all(20), children: [
      Text(offerStatusLabel(offer.status), style: Theme.of(context).textTheme.headlineSmall),
      Text('Your participation: ${offer.buyerId == user.id ? 'Buyer' : 'Seller'}'),
      Text('Quantity: ${offer.quantity}'), Text('Material value: LKR ${offer.totalValue.toStringAsFixed(2)}'),
      Text('Created: ${offer.createdAt.toLocal()}'),
      const Text('Return to My Offers and refresh for the latest decision, reservation status and transaction history.'),
    ]),
  );
}

class TransactionHistoryScreen extends StatefulWidget {
  const TransactionHistoryScreen({required this.gateway, required this.transactionId, super.key});
  final OfferGateway gateway;
  final String transactionId;
  @override State<TransactionHistoryScreen> createState() => _TransactionHistoryScreenState();
}
class _TransactionHistoryScreenState extends State<TransactionHistoryScreen> {
  TransactionHistoryPage? data;
  String? error;
  @override void initState() { super.initState(); widget.gateway.history(widget.transactionId).then((value) { if (mounted) setState(() => data = value); }).catchError((_) { if (mounted) setState(() => error = 'Unable to load transaction history.'); }); }
  @override Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Transaction History')),
    body: error != null ? Center(child: Text(error!)) : data == null ? const Center(child: CircularProgressIndicator()) : data!.items.isEmpty ? const Center(child: Text('No history events yet.')) : ListView(children: data!.items.map((entry) => ListTile(title: Text(entry.action.replaceAll('_', ' ')), subtitle: Text(entry.createdAt.toLocal().toString()))).toList()),
  );
}

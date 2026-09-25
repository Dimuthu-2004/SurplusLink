import 'package:flutter/foundation.dart';
import 'package:mobile/auth/auth_models.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/materials/material_models.dart';
import 'package:mobile/offers/offer_gateway.dart';
import 'package:mobile/offers/offer_models.dart';
import 'package:mobile/requirements/requirement_gateway.dart';
import 'package:mobile/requirements/requirement_models.dart';

/// Read-only composition of existing mobile list APIs for the home dashboard.
final class HomeDashboardController extends ChangeNotifier {
  HomeDashboardController({
    required this.user,
    this.requirements,
    this.materials,
    this.offers,
  });
  final AppUser user;
  final RequirementGateway? requirements;
  final MaterialInventoryGateway? materials;
  final OfferGateway? offers;
  bool isLoading = true;
  String? error;
  HomeDashboardData data = const HomeDashboardData();

  Future<void> load() async {
    isLoading = true;
    error = null;
    notifyListeners();
    final results = await Future.wait([_buyer(), _seller(), _transactions()]);
    final buyer = results[0] as BuyerDashboardData?;
    final seller = results[1] as SellerDashboardData?;
    final activities = <DashboardActivity>[
      ...buyer?.activities ?? [],
      ...seller?.activities ?? [],
      ...results[2] as List<DashboardActivity>,
    ]..sort((a, b) => b.when.compareTo(a.when));
    data = HomeDashboardData(
      buyer: buyer,
      seller: seller,
      activities: activities,
    );
    isLoading = false;
    notifyListeners();
  }

  Future<BuyerDashboardData?> _buyer() async {
    if (!user.hasRole(AppRole.buyer) || requirements == null) return null;
    try {
      final r = await Future.wait([
        requirements!.my(const RequirementQuery(pageSize: 10)),
        for (final status in const [
          'DRAFT',
          'OPEN',
          'MATCHING',
          'MATCH_FOUND',
          'PENDING_APPROVAL',
        ])
          requirements!.my(RequirementQuery(status: status, pageSize: 1)),
      ]);
      final totals = r.skip(1).map((x) => x.total).toList();
      return BuyerDashboardData(
        active: totals.fold(0, (a, b) => a + b),
        matches: totals[3],
        pending: totals[4],
        activities: r.first.items.map(_requirementActivity).nonNulls.toList(),
      );
    } on Object {
      error ??= 'Some dashboard information could not be refreshed.';
      return null;
    }
  }

  Future<SellerDashboardData?> _seller() async {
    if (!user.hasRole(AppRole.seller) || materials == null) return null;
    try {
      final r = await Future.wait([
        materials!.search(
          const MaterialListingQuery(mineOnly: true, pageSize: 10),
        ),
        materials!.search(
          const MaterialListingQuery(
            mineOnly: true,
            status: 'ACTIVE',
            pageSize: 1,
          ),
        ),
        materials!.search(
          const MaterialListingQuery(
            mineOnly: true,
            status: 'RESERVED',
            pageSize: 1,
          ),
        ),
      ]);
      return SellerDashboardData(
        active: r[1].totalCount,
        reserved: r[2].totalCount,
        activities: r.first.items.map(_listingActivity).nonNulls.toList(),
      );
    } on Object {
      error ??= 'Some dashboard information could not be refreshed.';
      return null;
    }
  }

  Future<List<DashboardActivity>> _transactions() async {
    if (offers == null ||
        (!user.hasRole(AppRole.buyer) && !user.hasRole(AppRole.seller))) {
      return const [];
    }
    try {
      return (await offers!.transactions(const OfferQuery(pageSize: 10))).items
          .map(_transactionActivity)
          .nonNulls
          .toList();
    } on Object {
      error ??= 'Some dashboard information could not be refreshed.';
      return const [];
    }
  }

  DashboardActivity? _requirementActivity(BuyerRequirement r) {
    final text = switch (r.status) {
      'PENDING_APPROVAL' => 'Requirement awaiting manager approval',
      'MATCH_FOUND' => 'Recommended matches are ready',
      'MATCHING' => 'Matching is in progress',
      'OPEN' => 'Requirement is open for matching',
      'APPROVED' => 'Requirement approved',
      _ => null,
    };
    return text == null
        ? null
        : DashboardActivity(text, r.updatedAt, 'requirement');
  }

  DashboardActivity? _listingActivity(MaterialListing r) {
    final text = switch (r.status) {
      'PENDING_VERIFICATION' => 'Listing awaiting verification',
      'ACTIVE' => 'Listing is active',
      'RESERVED' => 'Listing has reserved material',
      _ => null,
    };
    return text == null
        ? null
        : DashboardActivity(text, r.updatedAtUtc, 'listing');
  }

  DashboardActivity? _transactionActivity(Transaction r) {
    final text = switch (r.status) {
      'PENDING_APPROVAL' => 'Transaction awaiting manager approval',
      'APPROVED' when r.sellerId == user.id => 'Handover is ready',
      'APPROVED' => 'Transaction approved',
      'HANDED_OVER' when r.buyerId == user.id =>
        'Receipt confirmation is ready',
      'HANDED_OVER' => 'Materials handed over',
      'COMPLETED' => 'Transaction completed',
      _ => null,
    };
    return text == null
        ? null
        : DashboardActivity(text, r.updatedAt, 'transaction');
  }
}

final class HomeDashboardData {
  const HomeDashboardData({
    this.buyer,
    this.seller,
    this.activities = const [],
  });
  final BuyerDashboardData? buyer;
  final SellerDashboardData? seller;
  final List<DashboardActivity> activities;
}

final class BuyerDashboardData {
  const BuyerDashboardData({
    required this.active,
    required this.matches,
    required this.pending,
    required this.activities,
  });
  final int active, matches, pending;
  final List<DashboardActivity> activities;
}

final class SellerDashboardData {
  const SellerDashboardData({
    required this.active,
    required this.reserved,
    required this.activities,
  });
  final int active, reserved;
  final List<DashboardActivity> activities;
}

final class DashboardActivity {
  const DashboardActivity(this.label, this.when, this.kind);
  final String label, kind;
  final DateTime when;
}

import 'package:mobile/materials/material_models.dart';
import 'package:mobile/categories/material_category.dart';

abstract interface class MaterialInventoryGateway {
  Future<List<MaterialCategory>> categories();
  Future<MaterialListingPage> search(MaterialListingQuery query);
  Future<MaterialListing> getById(String listingId);
  Future<MaterialListing> create(MaterialListingDraft draft);
  Future<MaterialListing> update(String listingId, MaterialListingDraft draft);
  Future<void> delete(String listingId);
  Future<MaterialListing> publish(String listingId);
  Future<List<MaterialListingHistoryEntry>> history(String listingId);
}

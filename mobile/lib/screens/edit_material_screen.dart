import 'package:flutter/widgets.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/screens/material_listing_form_screen.dart';

class EditMaterialScreen extends StatelessWidget {
  const EditMaterialScreen({required this.gateway, required this.listingId, this.locationLookup, super.key});

  final MaterialInventoryGateway gateway;
  final String listingId;
  final AddressLookup? locationLookup;

  @override
  Widget build(BuildContext context) =>
      MaterialListingFormScreen(
        gateway: gateway,
        listingId: listingId,
        locationLookup: locationLookup,
      );
}
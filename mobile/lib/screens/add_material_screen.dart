import 'package:flutter/widgets.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/location/location_lookup.dart';
import 'package:mobile/screens/material_listing_form_screen.dart';

class AddMaterialScreen extends StatelessWidget {
  const AddMaterialScreen({
    required this.gateway,
    this.locationLookup,
    this.addressSearch,
    super.key,
  });

  final MaterialInventoryGateway gateway;
  final AddressLookup? locationLookup;
  final AddressSearch? addressSearch;

  @override
  Widget build(BuildContext context) => MaterialListingFormScreen(
    gateway: gateway,
    locationLookup: locationLookup,
    addressSearch: addressSearch,
  );
}

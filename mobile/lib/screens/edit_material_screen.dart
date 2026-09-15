import 'package:flutter/widgets.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/screens/material_listing_form_screen.dart';

class EditMaterialScreen extends StatelessWidget {
  const EditMaterialScreen({required this.gateway, required this.listingId, super.key});

  final MaterialInventoryGateway gateway;
  final String listingId;

  @override
  Widget build(BuildContext context) =>
      MaterialListingFormScreen(gateway: gateway, listingId: listingId);
}
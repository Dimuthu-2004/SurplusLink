import 'package:flutter/widgets.dart';
import 'package:mobile/materials/material_inventory_gateway.dart';
import 'package:mobile/screens/material_listing_form_screen.dart';

class AddMaterialScreen extends StatelessWidget {
  const AddMaterialScreen({required this.gateway, super.key});

  final MaterialInventoryGateway gateway;

  @override
  Widget build(BuildContext context) => MaterialListingFormScreen(gateway: gateway);
}
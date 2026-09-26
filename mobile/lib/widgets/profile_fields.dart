import 'package:flutter/material.dart';
import 'package:mobile/auth/auth_models.dart';

class ProfileFieldsController {
  ProfileFieldsController([AppUser? user])
    : name = TextEditingController(text: user?.fullName),
      phone = TextEditingController(text: user?.phoneNumber),
      nic = TextEditingController(),
      business = TextEditingController(text: user?.businessName),
      address = TextEditingController(text: user?.address);
  final TextEditingController name, phone, nic, business, address;
  UserProfile get profile => UserProfile(
    fullName: name.text,
    phoneNumber: phone.text,
    nic: nic.text,
    businessName: business.text,
    address: address.text,
  );
  void dispose() {
    for (final field in [name, phone, nic, business, address]) {
      field.dispose();
    }
  }
}

class ProfileFields extends StatelessWidget {
  const ProfileFields({
    required this.controller,
    this.enabled = true,
    super.key,
  });
  final ProfileFieldsController controller;
  final bool enabled;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      _field(
        'Full name',
        'profile-name',
        controller.name,
        120,
        hint: AutofillHints.name,
      ),
      _field(
        'Phone number',
        'profile-phone',
        controller.phone,
        26,
        type: TextInputType.phone,
        hint: AutofillHints.telephoneNumber,
        validate: (value) =>
            RegExp(r'^(?:0?94|\+94|0)7\d{8}$').hasMatch((value?.trim() ?? '').replaceAll(RegExp(r'[ -]'), ''))
            ? null
            : 'Enter a valid Sri Lankan phone number.',
      ),
      _field('Sri Lankan NIC', 'profile-nic', controller.nic, 12, validate: (value) => RegExp(r'^\d{9}[VvXx]$|^\d{12}$').hasMatch((value?.trim() ?? '').replaceAll(' ', '')) ? null : 'Enter a valid Sri Lankan NIC number.'),
      _field(
        'Business / organization (optional)',
        'profile-business',
        controller.business,
        160,
        optional: true,
      ),
      _field(
        'Address',
        'profile-address',
        controller.address,
        400,
        hint: AutofillHints.fullStreetAddress,
      ),
      const Padding(
        padding: EdgeInsets.only(bottom: 16),
        child: Text(
          'Your name, business and contact details appear on your material listings. Your account address stays private.',
          style: TextStyle(fontSize: 12),
        ),
      ),
    ],
  );
  Widget _field(
    String label,
    String keyName,
    TextEditingController field,
    int max, {
    bool optional = false,
    TextInputType? type,
    String? hint,
    FormFieldValidator<String>? validate,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: 16),
    child: TextFormField(
      key: Key(keyName),
      controller: field,
      enabled: enabled,
      keyboardType: type,
      autofillHints: hint == null ? null : [hint],
      decoration: InputDecoration(labelText: label),
      validator:
          validate ??
          (value) => !optional && (value?.trim().isEmpty ?? true)
              ? '$label is required.'
              : (value?.trim().length ?? 0) > max
              ? 'Use at most $max characters.'
              : null,
    ),
  );
}

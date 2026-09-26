import 'package:flutter/material.dart';

Future<bool> confirmLogout(BuildContext context) async => await showDialog<bool>(context: context, builder: (context) => AlertDialog(title: const Text('Log out of SurplusLink?'), content: const Text('Are you sure you want to end this session?'), actions: [TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')), FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Log Out'))])) ?? false;

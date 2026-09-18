import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class DashboardBackButton extends StatelessWidget {
  const DashboardBackButton({this.fallback = '/home', super.key});
  final String fallback;
  @override
  Widget build(BuildContext context) => BackButton(
    onPressed: () {
      if (context.canPop()) {
        context.pop();
      } else {
        context.go(fallback);
      }
    },
  );
}

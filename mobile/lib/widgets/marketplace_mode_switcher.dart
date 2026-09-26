import 'package:flutter/material.dart';
import 'package:mobile/marketplace/marketplace_mode.dart';
import 'package:mobile/marketplace/marketplace_mode_controller.dart';
import 'package:mobile/theme/surplus_link_theme.dart';

class MarketplaceModeSwitcher extends StatelessWidget {
  const MarketplaceModeSwitcher({required this.controller, super.key});
  final MarketplaceModeController controller;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: controller,
    builder: (context, _) {
      if (!controller.isDualRole) return const SizedBox.shrink();
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            key: const Key('marketplace-mode-switcher'),
            padding: const EdgeInsets.all(4),
            decoration: BoxDecoration(
              color: SurplusLinkTheme.surfaceSoft,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                for (final mode in MarketplaceMode.values)
                  Flexible(
                    child: Semantics(
                      selected: controller.activeMode == mode,
                      child: TextButton(
                        key: Key('mode-${mode.name}'),
                        style: TextButton.styleFrom(
                          padding: const EdgeInsets.symmetric(horizontal: 10),
                          foregroundColor: controller.activeMode == mode
                              ? SurplusLinkTheme.amberDark
                              : SurplusLinkTheme.slate600,
                          backgroundColor: controller.activeMode == mode
                              ? SurplusLinkTheme.amberSoft
                              : Colors.transparent,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(7),
                          ),
                        ),
                        onPressed: controller.canSwitch
                            ? () => controller.select(mode)
                            : null,
                        child: FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            mode == MarketplaceMode.buyer
                                ? 'Buyer Mode'
                                : 'Seller Mode',
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          if (controller.mutations.isBusy)
            const Text(
              'Finish saving before switching modes.',
              style: TextStyle(color: Colors.white70),
            ),
          if (controller.persistenceError != null)
            Text(
              controller.persistenceError!,
              style: const TextStyle(color: Colors.white70),
            ),
        ],
      );
    },
  );
}

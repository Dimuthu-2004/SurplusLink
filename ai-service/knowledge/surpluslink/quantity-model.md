<!--
title: SurplusLink Quantity & Packaging Model
source: SurplusLink Core Documentation
source_url: https://surpluslink.lk/docs/quantity-model
topic: Quantity Model
country: Sri Lanka
language: English
last_reviewed: 2026-10-01
source_type: SURPLUSLINK_INTERNAL
-->

# Quantity & Packaging Model

## Universal Quantity Concept
SurplusLink handles construction materials using a universal quantity model that distinguishes base measurements, physical packages, and package counts.

## Key Distinctions
- **Base Measurement**: Normalized base unit quantity (e.g. Litres, Kilograms, Square Meters, Cubic Meters, Pieces).
- **Physical Package Container**: Specific physical container (e.g., Can, Bag, Box, Drum, Cartridge).
- **Package Count**: Whole integer count of physical package containers.
- **Package Size**: Volume or weight per container (e.g. 4L per can, 50kg per bag).

## Examples
1. **Paint**:
   - 3 Cans of 4L paint = 3 Package Count × 4L Package Size = 12L Base Quantity.
   - Pricing is specified per package (LKR per can) or per base unit (LKR per L).
2. **Cement**:
   - 7 Bags of 50kg cement = 7 Package Count × 50kg Package Size = 350kg Base Quantity.
3. **Floor Tiles**:
   - 10 Boxes of 1.44 sqm tiles = 10 Package Count × 1.44 sqm = 14.4 sqm Base Quantity.
4. **Piece / Unit (Tools, Equipment)**:
   - 2 Generators = 2 Pieces / Units.

## Overage & Minimum Package Rules
- Sellers list full physical packages. Partial physical packages (e.g. half a can of opened paint) are not sold unless listed as a custom continuous measurement.
- If a buyer requires 6L of paint and seller sells 4L cans, the buyer requires 2 full cans (8L), producing 2L of unavoidable overage.

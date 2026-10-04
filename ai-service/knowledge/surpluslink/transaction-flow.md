<!--
title: SurplusLink Transaction Lifecycle
source: SurplusLink Core Documentation
source_url: https://surpluslink.lk/docs/transaction-flow
topic: Transaction Lifecycle
country: Sri Lanka
language: English
last_reviewed: 2026-10-01
source_type: SURPLUSLINK_INTERNAL
-->

# Transaction Lifecycle

## Lifecycle States
1. **Match Recommendation**: AI engine creates match group for buyer requirement.
2. **Manager Approval**: Manager approves match group; offers are issued to seller(s).
3. **Offer Acceptance & Stock Reservation**: Seller accepts offer; inventory allocation is reserved (`RESERVED` status).
4. **Seller Handover (`Waiting for Seller` -> `Handed Over`)**: Seller prepares materials and marks handover in app.
5. **Buyer Receipt (`Waiting for Buyer` -> `Completed`)**: Buyer inspects delivered materials and confirms receipt in app.
6. **Manager Review (If applicable)**: If handover occurs without buyer receipt or timeline issues arise, transaction enters Manager Review.

## Handover & Confirmation Security
- Transactions enforce secure verification to confirm physical exchange.
- Inventory is deducted permanently only upon successful completion.

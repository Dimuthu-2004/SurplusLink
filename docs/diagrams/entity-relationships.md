# Implemented entity relationships

Names below refer to current EF entities. `Listing` maps to Listings, `BuyerRequest` to MaterialRequests, `MaterialMatch` to Matches, and `Category` to Categories.

```mermaid
erDiagram
  User ||--o{ UserRoleAssignment : has
  User ||--o{ Listing : sells
  User ||--o{ BuyerRequest : requests
  Category ||--o{ Listing : categorizes
  Category ||--o{ BuyerRequest : categorizes
  Listing ||--o{ ListingPhoto : has
  Listing ||--o{ MaterialMatch : candidate
  BuyerRequest ||--o{ MaterialMatch : evaluates
  MaterialMatch ||--o{ Offer : proposes
  Offer ||--o{ Transaction : records
  Listing ||--o{ Reservation : reserves
  BuyerRequest ||--o{ Reservation : receives
  BuyerRequest ||--o{ AgentWorkflow : attempts
  MaterialMatch o|--o{ AgentWorkflow : recommends
  AgentWorkflow ||--o{ AgentStep : records
  AgentStep ||--o{ AgentToolCall : observes
  AgentWorkflow ||--o{ Approval : reviewed
  User ||--o{ Approval : decides
  User o|--o{ AuditLog : acts
```

Role assignments have a composite (UserId, Role) key; email/category names and request/listing match pairs have unique indexes. Offer and Transaction each reference buyer and seller separately and enforce different counterparties. Reservations are correlated to transactions through the offer's match and its listing/request; there is no direct Transaction.ReservationId column. AuditLog uses entity type and ID rather than a foreign key to each domain table. The older Workflow entity/table remains for migration compatibility; new orchestration uses AgentWorkflow only. Constraints and indexes are defined in MarketplaceModelConfiguration and the committed migrations.

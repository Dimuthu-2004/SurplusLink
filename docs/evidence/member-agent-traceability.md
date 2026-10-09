# Member Agent Traceability

| Member | Component | Agent | Input / tools | Output |
|---|---|---|---|---|
| 1 | Material Inventory and Listings | `MaterialMatchingAgent` | Requirement criteria; active-material/detail tools | Candidate contributions or safe no-candidate response |
| 2 | Buyer Requirements | `RequirementPlannerAgent` | Stored requirement and optional objective; no tools | Normalized criteria and fixed workflow plan |
| 3 | Matching and Logistics | `LogisticsAgent` | Candidate IDs, location, deadline; route/location/cost tools | Route metrics, feasibility, warnings |
| 4 | Offers, Reservations, Transactions and Approval | `ValidationAgent` | Candidate, budget, logistics; six deterministic checks | Validation result requiring manager approval |

The persisted workflow order is Planner -> Matching -> Logistics -> Validation -> Manager approval. React and Flutter access the ASP.NET Core API; the Python agent service remains internal.

Partial multi-seller fulfilment is intentional: multiple smaller positive listings can be valid candidates, buyers select allocations, and reservation remains blocked until manager approval.

# Deterministic match scoring

The backend and AI service apply the same final score after successful routing:

```
T = unitPrice * requiredQuantity + estimatedTransportCost
C = EXCELLENT: 1, GOOD: 0.75, FAIR: 0.5, POOR: 0.25
score = roundToEven(0.50*C + 0.30*max(0, 1-T/maximumBudget)
                    + 0.20/(1+distanceKm/100), 4)
```

The existing NEW condition is treated as EXCELLENT; unknown conditions earn zero condition points.
The score is stored on a 0?1 scale. Missing/failed logistics earn no final score (zero), with route measurements remaining null when unavailable. Zero score does not confer eligibility.

Category, unit, available quantity, active status, expiry, material and total budget, and delivery deadline remain hard checks. Candidates failing checks cannot be recommended. Excess stock no longer earns points: fulfilling the requested quantity is a hard constraint.

The matching agent's preliminary `basicFitScore` is `round(50*C + 30*materialBudgetHeadroom, 2)` on a 0?100 scale. It orders the initial search only, and never determines the recommendation. All matching candidates receive logistics/validation evaluation before selection.

Final ranking: descending rounded score; descending condition; ascending total estimated cost; ascending distance; ascending canonical listing ID. The backend independently verifies the winning ID and score against the trusted snapshot. Model text/objectives do not influence this policy.

The existing VALIDATION step output stores `scoreBreakdown` (score, condition/cost/distance points, total estimated cost), `recommendationReason`, and candidate eligibility evaluations. Only the winner receives the top-level `recommendedMatchId`. No schema migration or Flutter contract change is required. The comparison badge uses the latest workflow, preventing historical recommendations from marking multiple candidates.

Regression coverage includes POOR versus EXCELLENT at equal former base scores, each condition level, route failures/missing measurements, rounded-score ties resolved by condition/cost/distance/ID, reversed input order, and a single recommended candidate.

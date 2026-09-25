"""Deterministic policy shared with backend Matching/MatchScoring.cs."""
from decimal import Decimal, ROUND_HALF_EVEN


def condition_rank(condition):
    return {"NEW": 4, "EXCELLENT": 4, "GOOD": 3, "FAIR": 2, "POOR": 1}.get(condition.upper(), 0)


def score_breakdown(condition, material_cost, budget, distance=None, transport=None):
    quality = Decimal(condition_rank(condition)) / 4
    complete = distance is not None and transport is not None
    total = material_cost + transport if complete else None
    cost_points = Decimal("0.3") * max(Decimal(0), 1 - total / budget) if complete else Decimal(0)
    distance_points = Decimal("0.2") / (1 + distance / 100) if complete else Decimal(0)
    condition_points = Decimal("0.5") * quality
    score = (condition_points + cost_points + distance_points).quantize(Decimal("0.0001"), rounding=ROUND_HALF_EVEN) if complete else Decimal(0)
    return dict(score=score, conditionPoints=condition_points, costPoints=cost_points,
                distancePoints=distance_points, totalEstimatedCost=total)

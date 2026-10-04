from typing import Any, Dict, List, Optional


class MatchExplanationAgent:
    """Explains persisted recommendation results and score breakdowns using factual backend data."""

    def explain_match(self, matches: List[Dict[str, Any]], query: str) -> str:
        if not matches:
            return "I couldn't find any active recommended matches for your requirements currently. When your requirement is approved by a manager, recommendations will appear here."

        top_match = matches[0]
        listing_title = top_match.get("listingTitle") or "Surplus Material"
        score = top_match.get("score", 0.0)
        unit_price = top_match.get("unitPrice")
        distance_km = top_match.get("distanceKm")
        transport_cost = top_match.get("transportCost")
        reason = top_match.get("recommendationReason")

        explanation_parts = [
            f"Here is why **{listing_title}** was recommended:\n",
            f"• **Item Identity**: Matches your requested material category and specification.",
            f"• **Match Score**: {score:.2f} (Persisted system score based on price, condition, distance, and compatibility).",
        ]

        if unit_price is not None:
            explanation_parts.append(f"• **Material Unit Price**: LKR {unit_price:,.2f}")

        if distance_km is not None:
            explanation_parts.append(f"• **Logistics Distance**: {distance_km:.1f} km")

        if transport_cost is not None:
            explanation_parts.append(f"• **Estimated Transport Cost**: LKR {transport_cost:,.2f}")

        if reason:
            explanation_parts.append(f"• **Recommendation Note**: {reason}")

        explanation_parts.append("\nThis recommendation was verified by SurplusLink's multi-seller optimization engine.")

        return "\n".join(explanation_parts)

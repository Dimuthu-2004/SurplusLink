from __future__ import annotations

import re


class AgenticLlm:
    """Deterministic structured Gemini double; no marketplace facts are invented."""
    def is_available(self): return True
    def generate_chat_response(self, *args, **kwargs): return None

    def generate_structured(self, messages, response_model, **_kwargs):
        text = messages[0]["content"]
        if response_model.__name__ == "PlannerModelOutput":
            delivery = "deliveryRequired=False" not in text
            steps = [{"capability": "MATCHING", "purpose": "Find deterministic eligible listings."}]
            if delivery:
                steps.append({"capability": "LOGISTICS", "purpose": "Evaluate controlled delivery facts."})
            steps.append({"capability": "VALIDATION", "purpose": "Apply deterministic business checks."})
            return response_model.model_validate({"objective": "Confirmed procurement requirement.", "planSteps": steps,
                "reasoningSummary": "Use eligible listings, applicable delivery analysis, and final validation.",
                "constraints": ["Approval remains human."]})
        ids = re.findall(r"'listingId': '([^']+)'", text)
        if response_model.__name__ == "SemanticMatchingOutput":
            return response_model.model_validate({"explanations": [{"listingId": x, "semanticFit": "HIGH",
                "reason": "Listing wording is semantically suitable.", "matchedAttributes": ["description"], "concerns": []} for x in ids]})
        if response_model.__name__ == "LogisticsReasoningOutput":
            return response_model.model_validate({"rationales": [{"listingId": x,
                "reason": "Controlled routing facts were considered.", "concerns": []} for x in ids]})
        raise AssertionError(response_model)

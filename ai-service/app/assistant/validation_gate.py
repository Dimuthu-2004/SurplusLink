from __future__ import annotations

import logging
import re
from typing import Any, Dict, List, Optional
from pydantic import BaseModel, ConfigDict

from app.assistant.canonical_request import CanonicalUserRequest
from app.assistant.semantic_schemas import Intent, RequirementDraft

logger = logging.getLogger(__name__)


class ValidationReport(BaseModel):
    model_config = ConfigDict(extra="ignore")

    is_valid: bool
    violations: List[str] = []
    sanitized_location: Optional[str] = None
    sanitized_draft: Optional[RequirementDraft] = None


class ValidationGate:
    """
    Authoritative single validation gate that verifies all planned assistant outputs,
    data models, calculations, and marketplace responses before presentation to the user.
    """

    INVALID_LOCATION_TOKENS = {
        "site",
        "site eka",
        "site ekata",
        "job site",
        "kohomada",
        "keeyada",
        "puluwanda",
        "ekata",
        "ganna",
        "thiyenawada",
        "ooneda",
        "monada",
    }

    @classmethod
    def validate_location(cls, location_text: Optional[str]) -> Optional[str]:
        """
        Validates location tokens.
        Rejects noise words like 'site', 'kohomada', 'puluwanda' as delivery locations.
        """
        if not location_text:
            return None

        cleaned = location_text.strip().lower()

        # Rejects noise words
        if cleaned in cls.INVALID_LOCATION_TOKENS or any(token in cleaned for token in ["kohomada", "keeyada", "puluwanda"]):
            logger.info("ValidationGate rejected invalid location token: '%s'", location_text)
            return None

        return location_text.strip()

    @classmethod
    def validate_area_calculation(cls, user_area_m2: float, calculated_area_m2: float) -> bool:
        """
        Verifies that area calculations preserve input sanity.
        Rejects ridiculous discrepancies (e.g. 12m2 user input turning into 33,445m2).
        """
        if user_area_m2 <= 0:
            return True

        ratio = calculated_area_m2 / user_area_m2
        if ratio > 5.0 or ratio < 0.2:
            logger.error("ValidationGate AREA SANITY FAIL: user_area=%.2f, calculated=%.2f, ratio=%.2f", user_area_m2, calculated_area_m2, ratio)
            return False

        return True

    @classmethod
    def validate_package_preservation(cls, initial_pkg_count: Optional[int], initial_pkg_size: Optional[float], final_draft: RequirementDraft) -> bool:
        """
        Verifies package count does not disappear when package size is supplied.
        """
        if initial_pkg_count and initial_pkg_size:
            if final_draft.input_mode == "PACKAGE" or final_draft.input_mode == "PACKAGE_COUNT":
                if final_draft.package_count is None:
                    logger.error("ValidationGate PACKAGE PRESERVATION FAIL: package count was lost in final draft.")
                    return False
        return True

    @classmethod
    def validate_marketplace_provenance(cls, intent: Intent, tool_results: Optional[List[Dict[str, Any]]], response_text: str) -> bool:
        """
        Verifies that marketplace answers are produced only from non-empty live tool results.
        Prevents RAG or LLM hallucination of marketplace prices or listings.
        """
        if intent in {Intent.LIVE_MARKETPLACE_QUERY, Intent.PRICE_INFORMATION}:
            # If answer contains exact price claims like LKR XXX, check tool_results
            if "LKR" in response_text or "rs." in response_text.lower():
                if not tool_results:
                    logger.error("ValidationGate MARKETPLACE PROVENANCE FAIL: Price claimed without live tool results.")
                    return False
        return True

    @classmethod
    def validate_canonical_request(cls, req: CanonicalUserRequest) -> ValidationReport:
        """Runs validation checks on incoming canonical request."""
        violations = []
        sanitized_loc = cls.validate_location(req.location)

        # Check dimensions vs area invariant
        if req.dimensions and req.coverage_area:
            # Check that dimensions were not naively multiplied into area if they are item dimensions
            if req.dimensions.unit == "mm" and req.coverage_area > 1000:
                violations.append("Item dimensions in mm must not be directly treated as coverage area in m2.")

        return ValidationReport(
            is_valid=len(violations) == 0,
            violations=violations,
            sanitized_location=sanitized_loc,
        )

    @classmethod
    def validate_requirement_draft(cls, initial_req: CanonicalUserRequest, draft: RequirementDraft) -> ValidationReport:
        """Runs validation checks on requirement draft before sending response to user."""
        violations = []

        # 1. Location validation
        sanitized_loc = cls.validate_location(draft.location_text)

        # 2. Package preservation
        if not cls.validate_package_preservation(initial_req.package_count, initial_req.package_size, draft):
            violations.append("Package count disappeared during draft construction.")

        # 3. Quantity sanity
        if draft.normalized_quantity is not None and draft.normalized_quantity < 0:
            violations.append("Normalized quantity cannot be negative.")

        return ValidationReport(
            is_valid=len(violations) == 0,
            violations=violations,
            sanitized_location=sanitized_loc,
            sanitized_draft=draft,
        )

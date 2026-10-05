"""Strict, non-authoritative Gemini contracts for the four-stage workflow.

These schemas deliberately contain no prices, quantities, routes, status changes,
or tool names.  Model output can explain a decision, but it cannot manufacture or
alter the facts that ASP.NET and the deterministic agents own.
"""
from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class AgenticModel(BaseModel):
    model_config = ConfigDict(extra="forbid", frozen=True, str_strip_whitespace=True)


class PlannerModelStep(AgenticModel):
    capability: Literal["MATCHING", "LOGISTICS", "VALIDATION"]
    purpose: str = Field(min_length=1, max_length=280)


class PlannerModelOutput(AgenticModel):
    """The model may choose only from the fixed workflow capability allow-list."""

    objective: str = Field(min_length=1, max_length=400)
    planSteps: tuple[PlannerModelStep, ...] = Field(min_length=2, max_length=3)
    reasoningSummary: str = Field(min_length=1, max_length=500)
    constraints: tuple[str, ...] = Field(default=(), max_length=12)


class SemanticCandidateExplanation(AgenticModel):
    listingId: str = Field(min_length=1, max_length=80)
    semanticFit: Literal["HIGH", "MEDIUM", "LOW"]
    reason: str = Field(min_length=1, max_length=400)
    matchedAttributes: tuple[str, ...] = Field(default=(), max_length=10)
    concerns: tuple[str, ...] = Field(default=(), max_length=10)


class SemanticMatchingOutput(AgenticModel):
    explanations: tuple[SemanticCandidateExplanation, ...] = Field(min_length=1, max_length=20)


class LogisticsCandidateRationale(AgenticModel):
    listingId: str = Field(min_length=1, max_length=80)
    reason: str = Field(min_length=1, max_length=400)
    concerns: tuple[str, ...] = Field(default=(), max_length=10)


class LogisticsReasoningOutput(AgenticModel):
    rationales: tuple[LogisticsCandidateRationale, ...] = Field(min_length=1, max_length=20)

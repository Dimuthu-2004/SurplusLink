from __future__ import annotations

from datetime import datetime, timezone
from decimal import Decimal
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator


class MatchingRequest(BaseModel):
    """Validated input accepted by the Material Matching Agent."""

    model_config = ConfigDict(extra="forbid", str_strip_whitespace=True)

    categoryId: str | None = Field(default=None, min_length=1, max_length=120)
    category: str | None = Field(default=None, min_length=1, max_length=120)
    requiredQuantity: Decimal = Field(gt=0)
    unit: str = Field(min_length=1, max_length=32)
    maximumBudget: Decimal = Field(gt=0)
    deadline: datetime

    @model_validator(mode="after")
    def has_category_and_future_deadline(self) -> "MatchingRequest":
        if not self.categoryId and not self.category:
            raise ValueError("Either categoryId or category is required.")
        if self.deadline.tzinfo is None:
            raise ValueError("deadline must include a timezone.")
        if self.deadline <= datetime.now(timezone.utc):
            raise ValueError("deadline must be in the future.")
        return self


class Candidate(BaseModel):
    """Public candidate schema. It intentionally contains no mutation fields."""

    model_config = ConfigDict(extra="forbid")

    listingId: str
    availableQuantity: Decimal = Field(ge=0)
    unitPrice: Decimal = Field(gt=0)
    condition: str = Field(min_length=1)
    basicFitScore: float = Field(ge=0, le=100)
    reason: str = Field(min_length=1, max_length=500)


class MatchingFailure(BaseModel):
    model_config = ConfigDict(extra="forbid")

    code: Literal["INVALID_INPUT", "SEARCH_UNAVAILABLE", "NO_CANDIDATE"]
    message: str = Field(min_length=1, max_length=500)


class MatchingResponse(BaseModel):
    """Safe result envelope used for successful and empty matching requests."""

    model_config = ConfigDict(extra="forbid")

    status: Literal["ok", "invalid_input", "search_unavailable", "no_candidate"]
    candidates: list[Candidate] = Field(default_factory=list)
    failure: MatchingFailure | None = None
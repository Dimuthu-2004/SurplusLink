from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from decimal import Decimal
from typing import Protocol, Sequence
from uuid import UUID


@dataclass(frozen=True)
class ActiveMaterialCriteria:
    """Criteria passed across the approved backend/repository read boundary."""

    buyer_user_id: UUID
    category_id: str | None
    category: str | None
    required_quantity: Decimal
    unit: str
    maximum_budget: Decimal
    deadline: datetime
    requested_at: datetime


@dataclass(frozen=True)
class MaterialListingRecord:
    """Read model supplied by the approved boundary, never an EF/database entity."""

    seller_id: UUID
    listing_id: str
    category_id: str | None
    category: str | None
    available_quantity: Decimal
    unit: str
    unit_price: Decimal
    condition: str
    status: str
    is_verified: bool
    available_until: datetime


class ActiveMaterialsReadBoundary(Protocol):
    """Approved integration point for active-material reads.

    Implementations may call an internal backend endpoint or a repository. They
    must not expose mutation operations to this agent.
    """

    def search_active_materials(
        self, criteria: ActiveMaterialCriteria
    ) -> Sequence[MaterialListingRecord]:
        ...

    def get_material_detail(self, listing_id: str) -> MaterialListingRecord | None:
        ...


class MaterialSearchTools:
    """The Matching Agent's complete, read-only tool surface."""

    def __init__(self, boundary: ActiveMaterialsReadBoundary) -> None:
        self._boundary = boundary

    def search_active_materials(
        self, criteria: ActiveMaterialCriteria
    ) -> list[MaterialListingRecord]:
        return [
            listing
            for listing in self._boundary.search_active_materials(criteria)
            if self._is_safe_active(listing, criteria.requested_at)
        ]

    def get_material_detail(
        self, listing_id: str, *, requested_at: datetime
    ) -> MaterialListingRecord | None:
        detail = self._boundary.get_material_detail(listing_id)
        if detail is None or not self._is_safe_active(detail, requested_at):
            return None
        return detail

    @staticmethod
    def _is_safe_active(listing: MaterialListingRecord, requested_at: datetime) -> bool:
        return (
            listing.status.upper() == "ACTIVE"
            and listing.is_verified
            and listing.available_until > requested_at
        )
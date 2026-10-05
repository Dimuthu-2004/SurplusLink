import logging
import os
from typing import Any, Dict, List, Optional
import httpx

logger = logging.getLogger(__name__)


class BackendToolsClient:
    def __init__(self, base_url: Optional[str] = None, shared_token: Optional[str] = None):
        self.base_url = base_url or os.getenv("ASP_NET_BASE_URL", "http://localhost:5170")
        self.shared_token = shared_token or os.getenv("AI_SERVICE_SHARED_TOKEN", "development-shared-token-32-chars-long")

    def _headers(self, user_context: Dict[str, Any]) -> Dict[str, str]:
        user_id = user_context.get("user_id", "")
        roles = user_context.get("roles", [])
        roles_str = ",".join(roles) if isinstance(roles, list) else str(roles)

        return {
            "x-internal-token": self.shared_token,
            "x-user-id": str(user_id),
            "x-user-roles": roles_str,
            "Accept": "application/json",
        }

    async def get_current_user_summary(self, user_context: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/user-summary"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_current_user_summary failed: {e}")
        return None

    async def get_my_active_listings(self, user_context: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-active-listings"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_active_listings failed: {e}")
        return []

    async def get_my_listing(self, user_context: Dict[str, Any], listing_id: str) -> Optional[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-listings/{listing_id}"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_listing failed: {e}")
        return None

    async def get_my_requirements(self, user_context: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-requirements"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_requirements failed: {e}")
        return []

    async def get_my_requirement(self, user_context: Dict[str, Any], requirement_id: str) -> Optional[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-requirements/{requirement_id}"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_requirement failed: {e}")
        return None

    async def get_my_matches(self, user_context: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-matches"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_matches failed: {e}")
        return []

    async def get_my_offers(self, user_context: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-offers"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_offers failed: {e}")
        return []

    async def get_my_transactions(self, user_context: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-transactions"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_transactions failed: {e}")
        return []

    async def get_my_transaction(self, user_context: Dict[str, Any], reference: str) -> Optional[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/my-transactions/{reference}"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_my_transaction failed: {e}")
        return None

    async def get_catalog_item(self, user_context: Dict[str, Any], query: Optional[str] = None, template_id: Optional[str] = None) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/catalog-item"
        params = {}
        if query:
            params["query"] = query
        if template_id:
            params["templateId"] = template_id

        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context), params=params)
                if res.status_code == 200:
                    val = res.json()
                    return [val] if isinstance(val, dict) else val
        except Exception as e:
            logger.warning(f"Tool get_catalog_item failed: {e}")
        from app.assistant.item_resolver import BUILTIN_CATALOG_TEMPLATES
        return BUILTIN_CATALOG_TEMPLATES

    async def get_material_categories(self, user_context: Dict[str, Any]) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/material-categories"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_material_categories failed: {e}")
        return []

    async def search_active_listings(self, user_context: Dict[str, Any], query: Optional[str] = None, category_id: Optional[str] = None) -> List[Dict[str, Any]]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/active-listings-search"
        params = {}
        if query:
            params["query"] = query
        if category_id:
            params["categoryId"] = category_id

        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context), params=params)
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool search_active_listings failed: {e}")
        return []

    async def get_price_statistics(self, user_context: Dict[str, Any], query: Optional[str] = None, category_id: Optional[str] = None) -> Dict[str, Any]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/price-statistics"
        params = {}
        if query:
            params["query"] = query
        if category_id:
            params["categoryId"] = category_id

        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context), params=params)
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_price_statistics failed: {e}")
        return {"totalListings": 0, "minPrice": 0.0, "maxPrice": 0.0, "avgPrice": 0.0, "medianPrice": 0.0}

    async def get_seller_count(self, user_context: Dict[str, Any]) -> Dict[str, Any]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/seller-count"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_seller_count failed: {e}")
        return {}

    async def get_category_count(self, user_context: Dict[str, Any]) -> Dict[str, Any]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/category-count"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_category_count failed: {e}")
        return {}

    async def get_active_listing_count(self, user_context: Dict[str, Any]) -> Dict[str, Any]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/active-listing-count"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_active_listing_count failed: {e}")
        return {}

    async def get_marketplace_stats(self, user_context: Dict[str, Any]) -> Dict[str, Any]:
        url = f"{self.base_url.rstrip('/')}/api/ai-internal/tools/marketplace-stats"
        try:
            async with httpx.AsyncClient(timeout=8.0) as client:
                res = await client.get(url, headers=self._headers(user_context))
                if res.status_code == 200:
                    return res.json()
        except Exception as e:
            logger.warning(f"Tool get_marketplace_stats failed: {e}")
        return {}


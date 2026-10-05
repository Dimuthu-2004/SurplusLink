"""Provider boundary for SurplusLink natural-language features.

Only this module speaks to Gemini. Agents receive validated Pydantic values;
the model never receives a database mutation capability.
"""
import json
import logging
import os
import time
from typing import Dict, List, Optional, Protocol, TypeVar

from pydantic import BaseModel, ValidationError

T = TypeVar("T", bound=BaseModel)
logger = logging.getLogger(__name__)


class LLMProvider(Protocol):
    def is_available(self) -> bool: ...
    def generate_chat_response(self, messages: List[Dict[str, str]], system_prompt: Optional[str] = None,
                               temperature: float = 0.2, max_tokens: int = 800) -> Optional[str]: ...
    def generate_structured(self, messages: List[Dict[str, str]], response_model: type[T],
                            temperature: float = 0, max_tokens: int = 1000) -> Optional[T]: ...


class GeminiLLMProvider:
    """Official Google Gen AI SDK adapter with bounded validation retries."""
    def __init__(self, api_key: Optional[str] = None, model: Optional[str] = None, timeout: float = 15.0):
        self.api_key = api_key or os.getenv("GEMINI_API_KEY", "")
        self.model = model or os.getenv("GEMINI_MODEL", os.getenv("AI_MODEL", "gemini-3.8-flash"))
        self.timeout = float(os.getenv("GEMINI_TIMEOUT", str(timeout)))
        self._client = None
        self.last_error: Optional[str] = None
        if not self.api_key:
            self.last_error = "GEMINI_API_KEY is not configured."
            logger.error("Gemini is not configured: GEMINI_API_KEY is required for AI conversations.")
            return
        try:
            from google import genai
            self._types = __import__("google.genai.types", fromlist=["GenerateContentConfig"])
            self._client = genai.Client(
                api_key=self.api_key,
                http_options=self._types.HttpOptions(timeout=int(self.timeout * 1000)),
            )
        except Exception as exc:
            self.last_error = f"{type(exc).__name__}: {exc}"
            logger.error("Gemini initialization failed (%s). Install google-genai and check server configuration.", type(exc).__name__)

    def is_available(self) -> bool:
        return self._client is not None

    @staticmethod
    def _contents(messages: List[Dict[str, str]]) -> str:
        return "\n".join(f"{item.get('role', 'user').upper()}: {item.get('content', '')}" for item in messages)

    @staticmethod
    def _gemini_schema(schema: dict) -> dict:
        """Remove JSON Schema metadata unsupported by the Gemini Developer API."""
        unsupported_keys = {
            "additionalProperties",
            "exclusiveMaximum",
            "exclusiveMinimum",
            "maximum",
            "minimum",
        }
        if isinstance(schema, dict):
            schema = {
                key: GeminiLLMProvider._gemini_schema(value)
                for key, value in schema.items()
                if key not in unsupported_keys
            }
        elif isinstance(schema, list):
            return [GeminiLLMProvider._gemini_schema(item) for item in schema]
        return schema

    def _generate(self, messages: List[Dict[str, str]], system_prompt: Optional[str], temperature: float,
                  max_tokens: int, response_schema=None) -> Optional[str]:
        if not self.is_available():
            return None
        try:
            config = self._types.GenerateContentConfig(
                system_instruction=system_prompt or None,
                temperature=temperature,
                max_output_tokens=max_tokens,
                response_mime_type="application/json" if response_schema else None,
                response_schema=response_schema,
            )
        except (TypeError, ValueError) as exc:
            self.last_error = f"{type(exc).__name__}: {exc}"
            logger.error("Gemini request configuration is invalid: %s", exc)
            return None
        for attempt in range(2):
            try:
                result = self._client.models.generate_content(model=self.model, contents=self._contents(messages), config=config)
                if result and result.text:
                    self.last_error = None
                    return result.text
            except Exception as exc:
                self.last_error = f"{type(exc).__name__}: {exc}"
                logger.warning(
                    "Gemini request failed (attempt %s): %s: %s",
                    attempt + 1,
                    type(exc).__name__,
                    str(exc)[:300],
                )
                if attempt == 0:
                    time.sleep(0.5)
        return None

    def generate_chat_response(self, messages: List[Dict[str, str]], system_prompt: Optional[str] = None,
                               temperature: float = 0.2, max_tokens: int = 800) -> Optional[str]:
        return self._generate(messages, system_prompt, temperature, max_tokens)

    def generate_structured(self, messages: List[Dict[str, str]], response_model: type[T],
                            temperature: float = 0, max_tokens: int = 1000) -> Optional[T]:
        schema = self._gemini_schema(response_model.model_json_schema())
        raw = self._generate(messages, None, temperature, max_tokens, schema)
        if raw:
            try:
                result = response_model.model_validate_json(raw)
                self.last_error = None
                return result
            except (ValidationError, ValueError, json.JSONDecodeError) as exc:
                self.last_error = f"{type(exc).__name__}: {exc}"
                logger.warning("Gemini structured response failed Pydantic validation: %s", type(exc).__name__)
        return None

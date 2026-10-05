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
        if not self.api_key:
            logger.error("Gemini is not configured: GEMINI_API_KEY is required for AI conversations.")
            return
        try:
            from google import genai
            self._types = __import__("google.genai.types", fromlist=["GenerateContentConfig"])
            self._client = genai.Client(api_key=self.api_key)
        except Exception as exc:
            logger.error("Gemini initialization failed (%s). Install google-genai and check server configuration.", type(exc).__name__)

    def is_available(self) -> bool:
        return self._client is not None

    @staticmethod
    def _contents(messages: List[Dict[str, str]]) -> str:
        return "\n".join(f"{item.get('role', 'user').upper()}: {item.get('content', '')}" for item in messages)

    def _generate(self, messages: List[Dict[str, str]], system_prompt: Optional[str], temperature: float,
                  max_tokens: int, response_schema=None) -> Optional[str]:
        if not self.is_available():
            return None
        config = self._types.GenerateContentConfig(
            system_instruction=system_prompt or None,
            temperature=temperature,
            max_output_tokens=max_tokens,
            response_mime_type="application/json" if response_schema else None,
            response_schema=response_schema,
        )
        for attempt in range(3):
            try:
                result = self._client.models.generate_content(model=self.model, contents=self._contents(messages), config=config)
                if result and result.text:
                    return result.text
            except Exception as exc:
                logger.warning("Gemini request failed (attempt %s): %s", attempt + 1, type(exc).__name__)
                time.sleep(0.5 * (attempt + 1))
        return None

    def generate_chat_response(self, messages: List[Dict[str, str]], system_prompt: Optional[str] = None,
                               temperature: float = 0.2, max_tokens: int = 800) -> Optional[str]:
        return self._generate(messages, system_prompt, temperature, max_tokens)

    def generate_structured(self, messages: List[Dict[str, str]], response_model: type[T],
                            temperature: float = 0, max_tokens: int = 1000) -> Optional[T]:
        schema = response_model.model_json_schema()
        for attempt in range(3):
            raw = self._generate(messages, None, temperature, max_tokens, schema)
            if not raw:
                continue
            try:
                return response_model.model_validate_json(raw)
            except (ValidationError, ValueError, json.JSONDecodeError) as exc:
                logger.warning("Gemini structured response failed Pydantic validation (attempt %s): %s", attempt + 1, type(exc).__name__)
        return None

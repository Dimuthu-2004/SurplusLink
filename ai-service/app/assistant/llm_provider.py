import logging
import os
import time
from typing import Any, Dict, List, Optional, TypeVar

from pydantic import BaseModel, ValidationError

T = TypeVar("T", bound=BaseModel)

logger = logging.getLogger(__name__)


class GroqLLMProvider:
    def __init__(self, api_key: Optional[str] = None, model: Optional[str] = None, timeout: float = 15.0):
        self.api_key = api_key or os.getenv("GROQ_API_KEY", "")
        self.model = model or os.getenv("GROQ_MODEL", "qwen/qwen3.8-27b")
        self.timeout = float(os.getenv("GROQ_TIMEOUT", str(timeout)))
        self._client = None

        if self.api_key:
            try:
                from groq import Groq
                base_url = os.getenv("GROQ_BASE_URL", "").rstrip("/")
                if base_url.endswith("/openai/v1") or "groq.com" in base_url:
                    os.environ.pop("GROQ_BASE_URL", None)
                    base_url = ""
                options = {"api_key": self.api_key, "timeout": self.timeout}
                if base_url:
                    options["base_url"] = base_url
                self._client = Groq(**options)
            except Exception as e:
                logger.warning(f"Failed to initialize Groq client: {e}")

    def is_available(self) -> bool:
        return self._client is not None and bool(self.api_key)

    def generate_chat_response(
        self,
        messages: List[Dict[str, str]],
        system_prompt: Optional[str] = None,
        temperature: float = 0.2,
        max_tokens: int = 800,
    ) -> Optional[str]:
        if not self.is_available():
            logger.warning("GroqLLMProvider not available: client=%s, api_key_configured=%s", bool(self._client), bool(self.api_key))
            return None

        formatted_messages = []
        if system_prompt:
            formatted_messages.append({"role": "system", "content": system_prompt})
        formatted_messages.extend(messages)

        logger.info("Calling Groq model: %s for chat response", self.model)
        for attempt in range(3):
            try:
                completion = self._client.chat.completions.create(
                    model=self.model,
                    messages=formatted_messages,
                    temperature=temperature,
                    max_tokens=max_tokens,
                )
                if completion and completion.choices:
                    return completion.choices[0].message.content
            except Exception as e:
                logger.warning(f"Groq API call failed (attempt {attempt + 1}): {type(e).__name__} - {e}")
                time.sleep(0.5 * (attempt + 1))

        return None

    def generate_structured(
        self,
        messages: List[Dict[str, str]],
        response_model: type[T],
        temperature: float = 0,
        max_tokens: int = 1000,
    ) -> Optional[T]:
        if not self.is_available():
            logger.warning("GroqLLMProvider not available for structured call: client=%s, api_key_configured=%s", bool(self._client), bool(self.api_key))
            return None

        logger.info("Calling Groq model: %s for structured routing", self.model)
        for attempt in range(3):
            try:
                completion = self._client.chat.completions.create(
                    model=self.model,
                    messages=messages,
                    temperature=temperature,
                    max_tokens=max_tokens,
                    response_format={"type": "json_object"},
                )
                if completion and completion.choices:
                    content = completion.choices[0].message.content
                    logger.debug("Raw Groq structured response received: %s", content[:150] if content else None)
                    return response_model.model_validate_json(content)
            except ValidationError as val_err:
                logger.warning("Groq structured Pydantic validation failed (attempt %s): %s", attempt + 1, val_err)
                time.sleep(0.5 * (attempt + 1))
            except Exception as exc:
                logger.warning("Groq structured API call failed (attempt %s): %s - %s", attempt + 1, type(exc).__name__, exc)
                time.sleep(0.5 * (attempt + 1))
        return None

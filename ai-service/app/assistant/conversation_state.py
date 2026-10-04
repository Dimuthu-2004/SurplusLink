from __future__ import annotations

import time
from collections import OrderedDict
from threading import RLock

from app.assistant.semantic_schemas import ConversationState


class ConversationStateStore:
    """Small process-local state store; replaceable by Redis without changing the engine."""

    def __init__(self, max_conversations: int = 2000, ttl_seconds: int = 86400):
        self.max_conversations = max_conversations
        self.ttl_seconds = ttl_seconds
        self._items: OrderedDict[str, tuple[float, ConversationState]] = OrderedDict()
        self._lock = RLock()

    def get(self, key: str) -> ConversationState:
        now = time.monotonic()
        with self._lock:
            item = self._items.pop(key, None)
            if item and now - item[0] <= self.ttl_seconds:
                self._items[key] = (now, item[1])
                return item[1]
            state = ConversationState()
            self._items[key] = (now, state)
            self._trim()
            return state

    def put(self, key: str, state: ConversationState) -> None:
        with self._lock:
            self._items.pop(key, None)
            self._items[key] = (time.monotonic(), state)
            self._trim()

    def _trim(self) -> None:
        while len(self._items) > self.max_conversations:
            self._items.popitem(last=False)

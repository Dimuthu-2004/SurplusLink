from __future__ import annotations

import json
import math
import os
import re
from collections import Counter
from pathlib import Path
from typing import Any, Iterable

MODEL_NAME = os.getenv("RAG_EMBEDDING_MODEL", "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")


def _tokens(text: str) -> list[str]:
    return re.findall(r"[^\W_]+", text.casefold(), flags=re.UNICODE)


def _normalize(vector) -> list[float]:
    values = [float(value) for value in vector]
    norm = math.sqrt(sum(value * value for value in values)) or 1.0
    return [value / norm for value in values]


class LocalVectorStore:
    def __init__(self, index_path: Path):
        self.index_path = Path(index_path)
        self.chunks: list[dict[str, Any]] = []
        self.embeddings: list[list[float]] = []
        self.embedding_model_name = MODEL_NAME
        self._model = None

    def _embedding_model(self):
        if self._model is None:
            from fastembed import TextEmbedding
            self._model = TextEmbedding(model_name=self.embedding_model_name)
        return self._model

    def add_chunks(self, chunks: Iterable[Any]) -> None:
        self.chunks = [dict(vars(chunk)) for chunk in chunks]
        texts = [f"{c['title']} {c['section']} {c['topic']} {c['content']}" for c in self.chunks]
        try:
            values = list(self._embedding_model().embed(texts))
            self.embeddings = [_normalize(row) for row in values]
        except Exception:
            self.embeddings = []

    def save(self) -> None:
        self.index_path.parent.mkdir(parents=True, exist_ok=True)
        self.index_path.write_text(json.dumps({"version": 2, "embedding_model": self.embedding_model_name, "chunks": self.chunks, "embeddings": self.embeddings}, ensure_ascii=False), encoding="utf-8")

    def load(self) -> bool:
        if not self.index_path.exists() or self.index_path.stat().st_size == 0:
            return False
        try:
            data = json.loads(self.index_path.read_text(encoding="utf-8"))
            self.chunks = data.get("chunks", [])
            self.embeddings = data.get("embeddings", [])
            self.embedding_model_name = data.get("embedding_model", MODEL_NAME)
            return bool(self.chunks)
        except (OSError, ValueError, TypeError):
            return False

    def _filtered(self, metadata_filter: dict[str, str] | None):
        for index, chunk in enumerate(self.chunks):
            if metadata_filter and any(str(chunk.get(k, chunk.get("metadata", {}).get(k, ""))).casefold() != str(v).casefold() for k, v in metadata_filter.items()):
                continue
            yield index, chunk

    def compute_bm25_similarity(self, query: str, top_k: int = 8, metadata_filter: dict[str, str] | None = None):
        candidates = list(self._filtered(metadata_filter))
        if not candidates:
            return []
        docs = [_tokens(f"{c['title']} {c['section']} {c['topic']} {c['content']}") for _, c in candidates]
        query_tokens = _tokens(query)
        avgdl = sum(map(len, docs)) / max(len(docs), 1)
        dfs = Counter(token for doc in docs for token in set(doc))
        scored = []
        for (original_index, chunk), doc in zip(candidates, docs):
            frequencies = Counter(doc)
            score = 0.0
            for token in query_tokens:
                df = dfs[token]
                if not df:
                    continue
                idf = math.log(1 + (len(docs) - df + 0.5) / (df + 0.5))
                tf = frequencies[token]
                score += idf * (tf * 2.2) / (tf + 1.2 * (1 - 0.75 + 0.75 * len(doc) / max(avgdl, 1)))
            scored.append((chunk, score, original_index))
        scored.sort(key=lambda row: row[1], reverse=True)
        maximum = scored[0][1] if scored else 0
        return [(c, s / maximum if maximum else 0.0, i) for c, s, i in scored[:top_k]]

    def compute_embedding_similarity(self, query: str, top_k: int = 8, metadata_filter: dict[str, str] | None = None):
        if not self.embeddings or len(self.embeddings) != len(self.chunks):
            return []
        try:
            vector = _normalize(next(iter(self._embedding_model().query_embed(query))))
        except Exception:
            return []
        scored = []
        for index, chunk in self._filtered(metadata_filter):
            score = sum(float(a) * float(b) for a, b in zip(vector, self.embeddings[index]))
            scored.append((chunk, max(0.0, score), index))
        scored.sort(key=lambda row: row[1], reverse=True)
        return scored[:top_k]

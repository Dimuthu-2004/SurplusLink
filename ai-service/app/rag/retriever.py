from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

from app.rag.vector_store import LocalVectorStore


@dataclass
class Citation:
    title: str
    source: str
    url: str
    section: str


@dataclass
class RetrievalResult:
    query: str
    has_sufficient_evidence: bool
    context_text: str
    citations: List[Citation]
    raw_results: List[Dict[str, Any]]


class KnowledgeRetriever:
    def __init__(self, index_path: Optional[Path] = None, similarity_threshold: float = 0.12, top_k: int = 4):
        if index_path is None:
            index_path = Path(__file__).parent / "data" / "vector_index.json"
        self.vector_store = LocalVectorStore(index_path=index_path)
        self.similarity_threshold = similarity_threshold
        self.top_k = top_k
        self.loaded = self.vector_store.load()

    def retrieve(self, query: str, top_k: Optional[int] = None, threshold: Optional[float] = None, metadata_filter: Optional[Dict[str, str]] = None) -> RetrievalResult:
        if not self.loaded:
            self.loaded = self.vector_store.load()

        k = top_k if top_k is not None else self.top_k
        thresh = threshold if threshold is not None else self.similarity_threshold

        lexical = self.vector_store.compute_bm25_similarity(query, top_k=max(k * 3, 10), metadata_filter=metadata_filter)
        semantic = self.vector_store.compute_embedding_similarity(query, top_k=max(k * 3, 10), metadata_filter=metadata_filter)
        fused: Dict[str, Dict[str, Any]] = {}
        for channel_name, channel, weight in (("lexical", lexical, 1.0), ("semantic", semantic, 1.25)):
            for rank, (chunk, score, _) in enumerate(channel):
                row = fused.setdefault(chunk["chunk_id"], {"chunk": chunk, "score": 0.0, "lexical": 0.0, "semantic": 0.0})
                row["score"] += weight / (61 + rank)
                row[channel_name] = score
        terms = set(query.casefold().split())
        for row in fused.values():
            heading = f"{row['chunk']['title']} {row['chunk']['section']} {row['chunk']['topic']}".casefold()
            overlap = sum(term in heading for term in terms) / max(len(terms), 1)
            row["score"] = .35 * row["lexical"] + .55 * row["semantic"] + .10 * overlap
        raw_matches = sorted(fused.values(), key=lambda row: row["score"], reverse=True)[:k]

        if not raw_matches or raw_matches[0]["score"] < thresh:
            return RetrievalResult(
                query=query,
                has_sufficient_evidence=False,
                context_text="",
                citations=[],
                raw_results=[],
            )

        citations_seen = set()
        citations: List[Citation] = []
        context_parts: List[str] = []
        filtered_results: List[Dict[str, Any]] = []

        for row in raw_matches:
            chunk, score = row["chunk"], row["score"]
            if score < thresh:
                continue

            filtered_results.append({"chunk": chunk, "score": score, "lexical_score": row["lexical"], "semantic_score": row["semantic"]})

            cit_key = (chunk["title"], chunk["source"], chunk["source_url"], chunk["section"])
            if cit_key not in citations_seen:
                citations_seen.add(cit_key)
                citations.append(
                    Citation(
                        title=chunk["title"],
                        source=chunk["source"],
                        url=chunk["source_url"],
                        section=chunk["section"],
                    )
                )

            context_parts.append(
                f"--- Document: {chunk['title']} ({chunk['section']}) ---\n{chunk['content']}"
            )

        context_text = "\n\n".join(context_parts)

        return RetrievalResult(
            query=query,
            has_sufficient_evidence=len(context_parts) > 0,
            context_text=context_text,
            citations=citations,
            raw_results=filtered_results,
        )

    def _normalize_query(self, query: str) -> str:
        q = query.lower().strip()
        # Sinhala / Romanized Sinhala term maps for better construction retrieval
        synonyms = {
            "සිමෙන්ති": "cement",
            "පේන්ට්": "paint",
            "තීන්ත": "paint",
            "ටයිල්": "tiles",
            "යකඩ": "steel rebar",
            "ලී": "timber wood",
            "ලීටර්": "litre litres",
            "බෑග්": "bags",
            "ගඩොල්": "brick masonry",
            "වැලි": "sand aggregates",
            "කැට": "aggregates metal",
            "mata": "i need",
            "one": "need want",
            "hoyanna": "find search",
            "thiyenawa": "available stock list",
            "kiyada": "price cost rate",
            "mokada": "status details",
        }
        for k, v in synonyms.items():
            if k in q:
                q += f" {v}"
        return q

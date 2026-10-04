import os
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional


@dataclass
class DocumentChunk:
    chunk_id: str
    doc_id: str
    title: str
    section: str
    content: str
    topic: str
    source: str
    source_url: str
    language: str
    country: str
    last_reviewed: str
    source_type: str  # SURPLUSLINK_INTERNAL or REFERENCE
    metadata: Dict[str, Any] = field(default_factory=dict)


def parse_metadata_header(text: str) -> Dict[str, str]:
    meta: Dict[str, str] = {}
    comment_match = re.search(r"<!--\s*(.*?)\s*-->", text, re.DOTALL)
    if comment_match:
        comment_content = comment_match.group(1)
        for line in comment_content.splitlines():
            if ":" in line:
                key, value = line.split(":", 1)
                meta[key.strip().lower()] = value.strip()
    return meta


def chunk_markdown_document(file_path: Path) -> List[DocumentChunk]:
    with open(file_path, "r", encoding="utf-8") as f:
        text = f.read()

    meta = parse_metadata_header(text)
    doc_id = file_path.stem
    title = meta.get("title", doc_id.replace("-", " ").title())
    topic = meta.get("topic", doc_id.title())
    source = meta.get("source", "SurplusLink Reference")
    source_url = meta.get("source_url", "https://surpluslink.lk")
    language = meta.get("language", "English")
    country = meta.get("country", "Sri Lanka")
    last_reviewed = meta.get("last_reviewed", "2026-10-01")
    source_type = meta.get("source_type", "REFERENCE")

    # Clean off metadata comment header
    content_text = re.sub(r"<!--\s*.*?\s*-->", "", text, flags=re.DOTALL).strip()

    # Split by headers (#, ##, ###)
    sections = re.split(r"\n(?=#{1,3}\s+)", content_text)

    chunks: List[DocumentChunk] = []
    chunk_idx = 0

    for section in sections:
        section_text = section.strip()
        if not section_text:
            continue

        # Extract section heading
        lines = section_text.splitlines()
        first_line = lines[0] if lines else ""
        if first_line.startswith("#"):
            section_title = first_line.lstrip("#").strip()
        else:
            section_title = "General"

        # Ignore tiny empty sections
        body = "\n".join(lines[1:]).strip() if len(lines) > 1 else section_text
        if len(body) < 15 and section_title == title:
            continue

        chunk_id = f"{doc_id}_chunk_{chunk_idx}"
        chunk_idx += 1

        chunks.append(
            DocumentChunk(
                chunk_id=chunk_id,
                doc_id=doc_id,
                title=title,
                section=section_title,
                content=f"{section_title}\n\n{body}" if section_title not in body else body,
                topic=topic,
                source=source,
                source_url=source_url,
                language=language,
                country=country,
                last_reviewed=last_reviewed,
                source_type=source_type,
                metadata={
                    "doc_id": doc_id,
                    "title": title,
                    "section": section_title,
                    "topic": topic,
                    "source": source,
                    "source_url": source_url,
                    "language": language,
                    "country": country,
                    "last_reviewed": last_reviewed,
                    "source_type": source_type,
                },
            )
        )

    return chunks


def load_all_knowledge_documents(knowledge_dir: Path) -> List[DocumentChunk]:
    chunks: List[DocumentChunk] = []
    if not knowledge_dir.exists():
        return chunks

    for root, _, files in os.walk(knowledge_dir):
        for file in files:
            if file.endswith(".md"):
                file_path = Path(root) / file
                doc_chunks = chunk_markdown_document(file_path)
                chunks.extend(doc_chunks)

    return chunks

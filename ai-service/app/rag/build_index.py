import sys
from pathlib import Path

# Add parent directory to sys.path when executed directly
service_root = Path(__file__).resolve().parent.parent.parent
if str(service_root) not in sys.path:
    sys.path.insert(0, str(service_root))

from app.rag.document_loader import load_all_knowledge_documents
from app.rag.vector_store import LocalVectorStore


def build_index() -> dict:
    knowledge_dir = service_root / "knowledge"
    index_path = service_root / "app" / "rag" / "data" / "vector_index.json"

    print(f"Loading knowledge documents from: {knowledge_dir}")
    chunks = load_all_knowledge_documents(knowledge_dir)

    unique_docs = set(c.doc_id for c in chunks)

    vector_store = LocalVectorStore(index_path=index_path)
    vector_store.add_chunks(chunks)
    vector_store.save()

    summary = {
        "documents_indexed": len(unique_docs),
        "chunks_created": len(chunks),
        "errors": 0,
        "index_path": str(index_path),
    }

    print("\n--- Indexing Summary ---")
    print(f"Documents indexed: {summary['documents_indexed']}")
    print(f"Chunks created: {summary['chunks_created']}")
    print(f"Errors: {summary['errors']}")
    print(f"Index persisted to: {summary['index_path']}")

    return summary


if __name__ == "__main__":
    build_index()

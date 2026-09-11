from fastapi import FastAPI

app = FastAPI(title="SurplusLink AI Service", docs_url=None, redoc_url=None)


@app.get("/internal/health", tags=["operations"])
async def health() -> dict[str, str]:
    """Internal readiness probe; LangGraph workflows are added in future slices."""
    return {"status": "ok"}

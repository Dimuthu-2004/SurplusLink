import hmac
import logging
import os
from pathlib import Path

from dotenv import load_dotenv

logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s %(message)s")
logger = logging.getLogger(__name__)

# ---------------------------------------------------------
# Load environment variables from the project root .env.local
#
# Structure:
#
# SurplusLink/
# ├── .env.local
# ├── ai-service/
# │   └── app/
# │       └── main.py
# ├── backend/
# ├── mobile/
# └── web/
# ---------------------------------------------------------

AI_SERVICE_DIR = Path(__file__).resolve().parent.parent
PROJECT_ROOT = AI_SERVICE_DIR.parent
ENV_FILE = PROJECT_ROOT / ".env.local"

if ENV_FILE.exists():
    load_dotenv(dotenv_path=ENV_FILE, override=True)

from fastapi import Depends, FastAPI, Header, HTTPException
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel

from app.workflows.orchestration import (
    Limits,
    WorkflowOrchestrator,
    WorkflowRequest,
    WorkflowResponse,
)

app = FastAPI(
    title="SurplusLink AI Service",
    docs_url=None,
    redoc_url=None,
)

logger.info("ai_workflow_trace_logging_enabled module=app.workflows.orchestration")

@app.get("/internal/health", tags=["operations"])
async def health() -> dict[str, str]:
    """Internal readiness probe; never exposes workflow data or credentials."""
    return {"status": "ok"}


async def require_internal_token(
    x_internal_token: str = Header(default=""),
):
    expected = os.getenv("AI_SERVICE_SHARED_TOKEN", "")

    if len(expected) < 32:
        raise HTTPException(
            status_code=503,
            detail="Internal authentication is not configured.",
        )

    if not hmac.compare_digest(
        expected.encode(),
        x_internal_token.encode(),
    ):
        raise HTTPException(
            status_code=401,
            detail="Invalid internal credentials.",
        )


@app.exception_handler(RequestValidationError)
async def invalid_request(request, error):
    return JSONResponse(
        status_code=422,
        content={"detail": "Invalid request parameters."},
    )


@app.post(
    "/internal/workflows/run",
    response_model=WorkflowResponse,
    dependencies=[Depends(require_internal_token)],
)
async def run_workflow(request: WorkflowRequest):
    try:
        limits = Limits.from_env()
    except ValueError:
        raise HTTPException(
            status_code=503,
            detail="Workflow execution limits are invalid.",
        ) from None

    return await WorkflowOrchestrator(limits).run(request)

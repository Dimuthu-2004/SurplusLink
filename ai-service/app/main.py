from fastapi import FastAPI

import hmac
import os

from fastapi import Depends, Header, HTTPException
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

from app.workflows.orchestration import Limits, WorkflowOrchestrator, WorkflowRequest, WorkflowResponse

app = FastAPI(title="SurplusLink AI Service", docs_url=None, redoc_url=None)


@app.get("/internal/health", tags=["operations"])
async def health() -> dict[str, str]:
    """Internal readiness probe; never exposes workflow data or credentials."""
    return {"status": "ok"}


async def require_internal_token(x_internal_token: str = Header(default="")):
    expected = os.getenv("AI_SERVICE_SHARED_TOKEN", "")
    if len(expected) < 32:
        raise HTTPException(503, "Internal authentication is not configured.")
    if not hmac.compare_digest(expected.encode(), x_internal_token.encode()):
        raise HTTPException(401, "Invalid internal credentials.")


@app.exception_handler(RequestValidationError)
async def invalid_request(request, error):
    return JSONResponse(status_code=422, content={"detail": "Invalid workflow request."})


@app.post("/internal/workflows/run", response_model=WorkflowResponse,
          dependencies=[Depends(require_internal_token)])
async def run_workflow(request: WorkflowRequest):
    try:
        limits = Limits.from_env()
    except ValueError:
        raise HTTPException(503, "Workflow execution limits are invalid.") from None
    return await WorkflowOrchestrator(limits).run(request)

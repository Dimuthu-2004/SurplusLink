param([int]$AiPort = 8000)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$python = Join-Path $projectRoot 'ai-service/.venv/Scripts/python.exe'
if (!(Test-Path $python)) { throw 'Create ai-service/.venv and install requirements.txt first.' }
if (!$env:AI_SERVICE_SHARED_TOKEN) {
    $bytes = New-Object byte[] 48
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $env:AI_SERVICE_SHARED_TOKEN = [Convert]::ToBase64String($bytes)
}
$env:AI_SERVICE_BASE_URL = "http://127.0.0.1:$AiPort"
$env:AgentWorkflow__Enabled = 'true'
$routingRequired = 'Routing__Endpoint', 'Routing__ApiKey', 'Routing__BaseFee', 'Routing__CostPerKm', 'Routing__CostPerMinute'
$missingRouting = $routingRequired | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) }
if ($missingRouting) { Write-Warning ("Routing provider configuration is missing: " + ($missingRouting -join ', ')) }
$logs = Join-Path $projectRoot '.runtime'
New-Item -ItemType Directory -Force $logs | Out-Null
if (Get-NetTCPConnection -LocalPort $AiPort -State Listen -ErrorAction SilentlyContinue) {
    throw "Port $AiPort is already in use. Stop that service or choose another AiPort."
}
$ai = Start-Process -FilePath $python -ArgumentList '-m', 'uvicorn', 'app.main:app', '--host', '127.0.0.1', '--port', $AiPort -WorkingDirectory (Join-Path $projectRoot 'ai-service') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'ai.log') -RedirectStandardError (Join-Path $logs 'ai-error.log')
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($ai.HasExited) { throw 'AI service exited. Check .runtime/ai-error.log.' }
        try { Invoke-RestMethod "$env:AI_SERVICE_BASE_URL/internal/health" -TimeoutSec 2 | Out-Null; $ready = $true; break } catch { Start-Sleep -Seconds 1 }
    }
    if (!$ready) { throw 'AI service did not become ready.' }
    Write-Host 'AI ready; starting API with the same internal token and workflow worker enabled.'
    Write-Host 'Routing__* environment settings are still required for valid transport recommendations.'
    dotnet run --project (Join-Path $projectRoot 'backend/SurplusLink.Api') --launch-profile http
} finally {
    if (!$ai.HasExited) { Stop-Process -Id $ai.Id }
}

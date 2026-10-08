$ErrorActionPreference = 'Stop'
Write-Host '====================================='
Write-Host 'MEMBER 3 - MATCHING & LOGISTICS'
Write-Host '====================================='
Push-Location (Join-Path $PSScriptRoot '..\..\ai-service')
try { Write-Host '[1] Agent tests'; py -3 -m pytest member_tests/member3_tests.py -v; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; Write-Host 'FINAL RESULT: PASS' } finally { Pop-Location }

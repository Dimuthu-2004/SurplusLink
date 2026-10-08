$ErrorActionPreference = 'Stop'
Write-Host '====================================='
Write-Host 'MEMBER 4 - OFFERS, RESERVATIONS, TRANSACTIONS & APPROVAL'
Write-Host '====================================='
Push-Location (Join-Path $PSScriptRoot '..\..\ai-service')
try { Write-Host '[1] Agent tests'; py -3 -m pytest member_tests/member4_tests.py -v; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; Write-Host 'FINAL RESULT: PASS' } finally { Pop-Location }

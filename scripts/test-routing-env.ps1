$ErrorActionPreference = 'Stop'
$loader = Join-Path $PSScriptRoot 'import-routing-env.ps1'
$testFile = Join-Path ([IO.Path]::GetTempPath()) ('routing-env-' + [guid]::NewGuid() + '.txt')
$beforeKey = $env:Routing__ApiKey
$beforeFee = $env:Routing__BaseFee
try {
    @('Routing__ApiKey="local-test-only-key"', 'Routing__BaseFee=100', 'NOT_ROUTING=not-imported') | Set-Content -LiteralPath $testFile
    $env:Routing__ApiKey = ''
    $env:Routing__BaseFee = '222'
    $output = . $loader -Path $testFile
    if ($env:Routing__ApiKey -ne 'local-test-only-key') { throw 'Quoted routing value was not imported.' }
    if ($env:Routing__BaseFee -ne '222') { throw 'Existing environment value was overwritten.' }
    if ($output) { throw 'Loader should not log configuration values.' }
    if ($env:NOT_ROUTING) { throw 'Non-routing configuration was imported.' }
    Write-Host 'Routing environment import: PASS'
} finally {
    $env:Routing__ApiKey = $beforeKey
    $env:Routing__BaseFee = $beforeFee
    Remove-Item -LiteralPath $testFile
}
